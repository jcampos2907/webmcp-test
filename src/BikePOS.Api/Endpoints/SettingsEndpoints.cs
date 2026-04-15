using BikePOS.Data;
using BikePOS.Models;
using Microsoft.EntityFrameworkCore;

namespace BikePOS.Api.Endpoints;

public static class SettingsEndpoints
{
    public record ShopSettingsDto(
        string? ShopName, string? ShopAddress, string? ShopPhone,
        string? ShopEmail, string? ShopTaxId, string? ReceiptFooter);

    private static readonly string[] Keys =
        ["shop_name", "shop_address", "shop_phone", "shop_email", "shop_tax_id", "receipt_footer"];

    public static void MapSettingsEndpoints(this WebApplication app)
    {
        var g = app.MapGroup("/api/settings");

        g.MapGet("/shop", async (IDbContextFactory<BikePosContext> f, CancellationToken ct) =>
        {
            using var db = f.CreateDbContext();
            var all = await db.ShopSetting.Where(s => s.StoreId == null).ToListAsync(ct);
            string? Get(string k) => all.FirstOrDefault(s => s.Key == k)?.Value;
            return Results.Ok(new ShopSettingsDto(
                Get("shop_name"), Get("shop_address"), Get("shop_phone"),
                Get("shop_email"), Get("shop_tax_id"), Get("receipt_footer")));
        });

        g.MapPut("/shop", async (ShopSettingsDto body, IDbContextFactory<BikePosContext> f, CancellationToken ct) =>
        {
            using var db = f.CreateDbContext();
            var existing = await db.ShopSetting.Where(s => s.StoreId == null && Keys.Contains(s.Key)).ToListAsync(ct);
            var map = new Dictionary<string, string?>
            {
                ["shop_name"] = body.ShopName,
                ["shop_address"] = body.ShopAddress,
                ["shop_phone"] = body.ShopPhone,
                ["shop_email"] = body.ShopEmail,
                ["shop_tax_id"] = body.ShopTaxId,
                ["receipt_footer"] = body.ReceiptFooter,
            };
            foreach (var (k, v) in map)
            {
                var row = existing.FirstOrDefault(s => s.Key == k);
                if (row is null)
                {
                    if (!string.IsNullOrWhiteSpace(v))
                        db.ShopSetting.Add(new ShopSetting { Key = k, Value = v });
                }
                else
                {
                    row.Value = v ?? "";
                }
            }
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });
    }
}
