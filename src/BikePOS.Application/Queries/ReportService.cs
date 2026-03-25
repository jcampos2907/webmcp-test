using BikePOS.Data;
using BikePOS.Models;
using Microsoft.EntityFrameworkCore;

namespace BikePOS.Services;

// DTOs
public record DailySalesRow(
    DateOnly Date,
    decimal Revenue,
    int Transactions,
    decimal Cash,
    decimal Card,
    decimal Transfer);

public record ServiceRevenueRow(
    string ServiceName,
    decimal Revenue,
    int TicketCount);

public record MechanicProductivityRow(
    string MechanicName,
    int TicketCount,
    double AvgHoursToComplete);

public record ReceiptData(
    string StoreName,
    string? StoreAddress,
    string? StorePhone,
    string? StoreEmail,
    string? CompanyTaxId,
    string TicketDisplay,
    DateTime ChargeDate,
    string? CustomerName,
    string? ComponentName,
    string? ServiceName,
    List<ReceiptLineItem> Products,
    decimal Subtotal,
    decimal DiscountPercent,
    decimal Total,
    string PaymentMethod,
    decimal AmountPaid,
    string? CashierName);

public record ReceiptLineItem(
    string Name,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);

public class ReportService
{
    private readonly IDbContextFactory<BikePosContext> _dbFactory;
    private readonly TenantContext _tenant;

    public ReportService(IDbContextFactory<BikePosContext> dbFactory, TenantContext tenant)
    {
        _dbFactory = dbFactory;
        _tenant = tenant;
    }

    public async Task<List<DailySalesRow>> GetDailySalesAsync(DateOnly from, DateOnly to)
    {
        using var db = _dbFactory.CreateDbContext();
        var fromDt = from.ToDateTime(TimeOnly.MinValue);
        var toDt = to.ToDateTime(TimeOnly.MaxValue);

        var charges = await db.Charge
            .Where(c => c.PaymentStatus == PaymentStatus.Completed
                        && c.ChargedAt >= fromDt && c.ChargedAt <= toDt)
            .Select(c => new
            {
                Date = c.ChargedAt.Date,
                c.Amount,
                c.PaymentMethod
            })
            .ToListAsync();

        return charges
            .GroupBy(c => DateOnly.FromDateTime(c.Date))
            .Select(g => new DailySalesRow(
                g.Key,
                g.Sum(c => c.Amount),
                g.Count(),
                g.Where(c => c.PaymentMethod == PaymentMethod.Cash).Sum(c => c.Amount),
                g.Where(c => c.PaymentMethod == PaymentMethod.Card).Sum(c => c.Amount),
                g.Where(c => c.PaymentMethod == PaymentMethod.Transfer).Sum(c => c.Amount)))
            .OrderBy(r => r.Date)
            .ToList();
    }

    public async Task<List<ServiceRevenueRow>> GetRevenueByServiceTypeAsync(DateOnly from, DateOnly to)
    {
        using var db = _dbFactory.CreateDbContext();
        var fromDt = from.ToDateTime(TimeOnly.MinValue);
        var toDt = to.ToDateTime(TimeOnly.MaxValue);

        var rows = await db.Charge
            .Where(c => c.PaymentStatus == PaymentStatus.Completed
                        && c.ChargedAt >= fromDt && c.ChargedAt <= toDt)
            .Include(c => c.ServiceTicket).ThenInclude(t => t.BaseService)
            .Select(c => new
            {
                ServiceName = c.ServiceTicket.BaseService != null ? c.ServiceTicket.BaseService.Name : "No Service",
                c.Amount,
                c.ServiceTicketId
            })
            .ToListAsync();

        return rows
            .GroupBy(r => r.ServiceName)
            .Select(g => new ServiceRevenueRow(
                g.Key,
                g.Sum(r => r.Amount),
                g.Select(r => r.ServiceTicketId).Distinct().Count()))
            .OrderByDescending(r => r.Revenue)
            .ToList();
    }

    public async Task<List<MechanicProductivityRow>> GetMechanicProductivityAsync(DateOnly from, DateOnly to)
    {
        using var db = _dbFactory.CreateDbContext();
        var fromDt = from.ToDateTime(TimeOnly.MinValue);
        var toDt = to.ToDateTime(TimeOnly.MaxValue);

        var tickets = await db.ServiceTicket
            .Where(t => t.Mechanic != null
                        && t.Status == TicketStatus.Charged
                        && t.UpdatedAt >= fromDt && t.UpdatedAt <= toDt)
            .Select(t => new
            {
                MechanicName = t.Mechanic!.Name,
                HoursToComplete = (t.UpdatedAt - t.CreatedAt).TotalHours
            })
            .ToListAsync();

        return tickets
            .GroupBy(t => t.MechanicName)
            .Select(g => new MechanicProductivityRow(
                g.Key,
                g.Count(),
                Math.Round(g.Average(t => t.HoursToComplete), 1)))
            .OrderByDescending(r => r.TicketCount)
            .ToList();
    }

    public async Task<ReceiptData?> GetReceiptDataAsync(string chargeId)
    {
        using var db = _dbFactory.CreateDbContext();

        var charge = await db.Charge
            .Include(c => c.ServiceTicket).ThenInclude(t => t.Component)
            .Include(c => c.ServiceTicket).ThenInclude(t => t.BaseService)
            .Include(c => c.ServiceTicket).ThenInclude(t => t.Customer)
            .Include(c => c.ServiceTicket).ThenInclude(t => t.TicketProducts).ThenInclude(tp => tp.Product)
            .FirstOrDefaultAsync(c => c.Id == chargeId);

        if (charge is null) return null;

        var ticket = charge.ServiceTicket;

        // Get store info
        var store = await db.Store
            .Include(s => s.Company)
            .FirstOrDefaultAsync(s => s.Id == charge.StoreId);

        // Get shop settings for store name override
        var shopSettings = await db.ShopSetting
            .Where(s => s.StoreId == charge.StoreId)
            .ToDictionaryAsync(s => s.Key, s => s.Value);

        var storeName = shopSettings.GetValueOrDefault("shop_name") ?? store?.Name ?? "";
        var storeAddress = shopSettings.GetValueOrDefault("shop_address") ?? store?.Address;
        var storePhone = shopSettings.GetValueOrDefault("shop_phone") ?? store?.Phone;
        var storeEmail = shopSettings.GetValueOrDefault("shop_email") ?? store?.Email;
        var companyTaxId = shopSettings.GetValueOrDefault("shop_tax_id") ?? store?.Company?.TaxId;

        var products = ticket.TicketProducts.Select(tp => new ReceiptLineItem(
            tp.Product.Name,
            tp.Quantity,
            tp.UnitPrice,
            tp.Quantity * tp.UnitPrice)).ToList();

        var customerName = ticket.Customer != null
            ? $"{ticket.Customer.FirstName} {ticket.Customer.LastName}"
            : null;

        return new ReceiptData(
            storeName,
            storeAddress,
            storePhone,
            storeEmail,
            companyTaxId,
            ticket.TicketDisplay,
            charge.ChargedAt,
            customerName,
            ticket.Component?.Name,
            ticket.BaseService?.Name,
            products,
            ticket.Price / (1 - ticket.DiscountPercent / 100m),
            ticket.DiscountPercent,
            ticket.Price,
            charge.PaymentMethod.ToString(),
            charge.Amount,
            charge.CashierName);
    }
}
