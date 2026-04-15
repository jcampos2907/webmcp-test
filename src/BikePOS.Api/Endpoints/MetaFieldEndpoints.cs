using BikePOS.Application.Queries;
using BikePOS.Data;
using BikePOS.Models;
using Microsoft.EntityFrameworkCore;

namespace BikePOS.Api.Endpoints;

public static class MetaFieldEndpoints
{
    public record MetaFieldDto(
        string Id, string EntityType, string Key, string Label,
        string FieldType, bool IsRequired, int SortOrder, bool IsActive,
        string? Options, string? DefaultValue, string? RegexPattern);

    public record SaveMetaFieldDto(
        string EntityType, string Key, string Label,
        string FieldType, bool IsRequired, int SortOrder, bool IsActive,
        string? Options, string? DefaultValue, string? RegexPattern);

    public static void MapMetaFieldEndpoints(this WebApplication app)
    {
        var g = app.MapGroup("/api/meta-fields");

        g.MapGet("", async (ListMetaFieldsQueryHandler h, string? entityType, CancellationToken ct) =>
        {
            var fields = await h.HandleAsync(entityType ?? "Customer", null, ct);
            return Results.Ok(fields.Select(Map));
        });

        g.MapPost("", async (SaveMetaFieldDto body, IDbContextFactory<BikePosContext> f, CancellationToken ct) =>
        {
            using var db = f.CreateDbContext();
            var field = new MetaFieldDefinition
            {
                EntityType = body.EntityType,
                Key = body.Key,
                Label = body.Label,
                FieldType = body.FieldType,
                IsRequired = body.IsRequired,
                SortOrder = body.SortOrder,
                IsActive = body.IsActive,
                Options = body.Options,
                DefaultValue = body.DefaultValue,
                RegexPattern = body.RegexPattern,
            };
            db.MetaFieldDefinition.Add(field);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/meta-fields/{field.Id}", Map(field));
        });

        g.MapPut("/{id}", async (string id, SaveMetaFieldDto body, IDbContextFactory<BikePosContext> f, CancellationToken ct) =>
        {
            using var db = f.CreateDbContext();
            var field = await db.MetaFieldDefinition.FindAsync(new object[] { id }, ct);
            if (field is null) return Results.NotFound();
            field.EntityType = body.EntityType;
            field.Key = body.Key;
            field.Label = body.Label;
            field.FieldType = body.FieldType;
            field.IsRequired = body.IsRequired;
            field.SortOrder = body.SortOrder;
            field.IsActive = body.IsActive;
            field.Options = body.Options;
            field.DefaultValue = body.DefaultValue;
            field.RegexPattern = body.RegexPattern;
            await db.SaveChangesAsync(ct);
            return Results.Ok(Map(field));
        });

        g.MapDelete("/{id}", async (string id, IDbContextFactory<BikePosContext> f, CancellationToken ct) =>
        {
            using var db = f.CreateDbContext();
            var field = await db.MetaFieldDefinition.FindAsync(new object[] { id }, ct);
            if (field is null) return Results.NotFound();
            db.MetaFieldDefinition.Remove(field);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });
    }

    private static MetaFieldDto Map(MetaFieldDefinition f) => new(
        f.Id, f.EntityType, f.Key, f.Label, f.FieldType,
        f.IsRequired, f.SortOrder, f.IsActive,
        f.Options, f.DefaultValue, f.RegexPattern);
}
