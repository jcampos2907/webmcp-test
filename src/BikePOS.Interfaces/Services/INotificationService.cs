namespace BikePOS.Interfaces.Services;

/// <summary>
/// Abstraction for sending notifications (email, WhatsApp, etc.).
/// Infrastructure implementations live in Infrastructure/Notifications/.
/// </summary>
public interface INotificationService
{
    Task SendEmailAsync(string to, string subject, string htmlBody, CancellationToken ct = default);
    Task SendWhatsAppAsync(string phone, string message, CancellationToken ct = default);
}
