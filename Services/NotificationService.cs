using System.Net;
using System.Net.Mail;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using BikePOS.Data;
using BikePOS.Models;

namespace BikePOS.Services;

public class NotificationService
{
    private readonly IDbContextFactory<BikePosContext> _dbFactory;
    private readonly TenantContext _tenant;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IDbContextFactory<BikePosContext> dbFactory,
        TenantContext tenant,
        ILogger<NotificationService> logger)
    {
        _dbFactory = dbFactory;
        _tenant = tenant;
        _logger = logger;
    }

    /// <summary>
    /// Notify the customer that their ticket is ready (status = Completed).
    /// Reads notification settings from ShopSetting, sends via enabled channels.
    /// </summary>
    public async Task NotifyTicketReadyAsync(string ticketId)
    {
        using var db = _dbFactory.CreateDbContext();

        var ticket = await db.ServiceTicket
            .Include(t => t.Customer)
            .Include(t => t.Component)
            .Include(t => t.BaseService)
            .FirstOrDefaultAsync(t => t.Id == ticketId);

        if (ticket?.Customer is null) return;

        var settings = await db.ShopSetting
            .Where(s => s.StoreId == _tenant.StoreId)
            .ToDictionaryAsync(s => s.Key, s => s.Value);

        var emailEnabled = settings.GetValueOrDefault("notify_email_enabled") == "true";
        var whatsappEnabled = settings.GetValueOrDefault("notify_whatsapp_enabled") == "true";

        if (!emailEnabled && !whatsappEnabled) return;

        var storeName = settings.GetValueOrDefault("shop_name") ?? _tenant.StoreName ?? "BikePOS";
        var template = settings.GetValueOrDefault("notify_message_template")
            ?? "{CustomerName}, your {ServiceName} for {ComponentName} ({TicketNumber}) is ready for pickup at {StoreName}.";

        var message = template
            .Replace("{CustomerName}", ticket.Customer.FirstName)
            .Replace("{ServiceName}", ticket.BaseService?.Name ?? "service")
            .Replace("{ComponentName}", ticket.Component?.Name ?? "component")
            .Replace("{TicketNumber}", ticket.TicketDisplay)
            .Replace("{StoreName}", storeName);

        if (emailEnabled && !string.IsNullOrEmpty(ticket.Customer.Email))
        {
            await SendEmailAsync(db, ticket, ticket.Customer.Email, message, settings);
        }

        if (whatsappEnabled && !string.IsNullOrEmpty(ticket.Customer.Phone))
        {
            await SendWhatsAppAsync(db, ticket, ticket.Customer.Phone, message, settings);
        }
    }

    private async Task SendEmailAsync(
        BikePosContext db, ServiceTicket ticket, string email, string message,
        Dictionary<string, string> settings)
    {
        var log = new NotificationLog
        {
            ServiceTicketId = ticket.Id,
            CustomerId = ticket.CustomerId,
            Channel = NotificationChannel.Email,
            Recipient = email,
            Message = message,
            StoreId = _tenant.StoreId
        };

        try
        {
            var smtpHost = settings.GetValueOrDefault("notify_smtp_host", "");
            var smtpPort = int.TryParse(settings.GetValueOrDefault("notify_smtp_port"), out var p) ? p : 587;
            var smtpUser = settings.GetValueOrDefault("notify_smtp_user", "");
            var smtpPass = settings.GetValueOrDefault("notify_smtp_password", "");
            var fromEmail = settings.GetValueOrDefault("notify_smtp_from", smtpUser);
            var subject = settings.GetValueOrDefault("notify_email_subject", "Your service is ready!");

            if (string.IsNullOrEmpty(smtpHost))
            {
                log.Status = NotificationStatus.Failed;
                log.ErrorMessage = "SMTP host not configured";
                db.Set<NotificationLog>().Add(log);
                await db.SaveChangesAsync();
                return;
            }

            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                Credentials = new NetworkCredential(smtpUser, smtpPass),
                EnableSsl = true
            };

            var mailMessage = new MailMessage(fromEmail, email, subject, message);
            await client.SendMailAsync(mailMessage);

            log.Status = NotificationStatus.Sent;
            log.SentAt = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            log.Status = NotificationStatus.Failed;
            log.ErrorMessage = ex.Message;
            _logger.LogWarning(ex, "Failed to send email notification for ticket {TicketId}", ticket.Id);
        }

        db.Set<NotificationLog>().Add(log);
        await db.SaveChangesAsync();
    }

    private async Task SendWhatsAppAsync(
        BikePosContext db, ServiceTicket ticket, string phone, string message,
        Dictionary<string, string> settings)
    {
        var log = new NotificationLog
        {
            ServiceTicketId = ticket.Id,
            CustomerId = ticket.CustomerId,
            Channel = NotificationChannel.WhatsApp,
            Recipient = phone,
            Message = message,
            StoreId = _tenant.StoreId
        };

        try
        {
            var webhookUrl = settings.GetValueOrDefault("notify_whatsapp_webhook_url", "");
            var apiToken = settings.GetValueOrDefault("notify_whatsapp_api_token", "");

            if (string.IsNullOrEmpty(webhookUrl))
            {
                log.Status = NotificationStatus.Failed;
                log.ErrorMessage = "WhatsApp webhook URL not configured";
                db.Set<NotificationLog>().Add(log);
                await db.SaveChangesAsync();
                return;
            }

            using var httpClient = new HttpClient();
            if (!string.IsNullOrEmpty(apiToken))
                httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiToken);

            var payload = JsonSerializer.Serialize(new
            {
                phone = phone,
                message = message,
                ticketId = ticket.Id,
                ticketNumber = ticket.TicketDisplay
            });

            var response = await httpClient.PostAsync(webhookUrl,
                new StringContent(payload, Encoding.UTF8, "application/json"));

            if (response.IsSuccessStatusCode)
            {
                log.Status = NotificationStatus.Sent;
                log.SentAt = DateTime.UtcNow;
            }
            else
            {
                log.Status = NotificationStatus.Failed;
                log.ErrorMessage = $"HTTP {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}";
            }
        }
        catch (Exception ex)
        {
            log.Status = NotificationStatus.Failed;
            log.ErrorMessage = ex.Message;
            _logger.LogWarning(ex, "Failed to send WhatsApp notification for ticket {TicketId}", ticket.Id);
        }

        db.Set<NotificationLog>().Add(log);
        await db.SaveChangesAsync();
    }

    /// <summary>Get recent notification logs for the current store.</summary>
    public async Task<List<NotificationLog>> GetRecentLogsAsync(int count = 20)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.Set<NotificationLog>()
            .Include(n => n.ServiceTicket)
            .Include(n => n.Customer)
            .Where(n => n.StoreId == _tenant.StoreId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(count)
            .ToListAsync();
    }
}
