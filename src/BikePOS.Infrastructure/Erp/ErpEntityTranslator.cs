using BikePOS.Models;

namespace BikePOS.Infrastructure.Erp;

/// <summary>
/// Anti-corruption layer: translates between BikePOS domain models and ERP-specific payloads.
/// Replaces the reflection-based field mapping with explicit, type-safe translations.
/// Falls back to SyncFieldMapping configuration when available.
/// </summary>
public static class ErpEntityTranslator
{
    /// <summary>
    /// Translate a Customer entity to a dictionary for ERP push.
    /// Uses field mappings if configured, otherwise a sensible default mapping.
    /// </summary>
    public static Dictionary<string, object?> TranslateCustomer(Customer customer, IReadOnlyList<SyncFieldMapping>? mappings = null)
    {
        if (mappings != null && mappings.Count > 0)
            return ApplyMappings(customer, mappings);

        return new Dictionary<string, object?>
        {
            ["first_name"] = customer.FirstName,
            ["last_name"] = customer.LastName,
            ["full_name"] = customer.FullName,
            ["email"] = customer.Email,
            ["phone"] = customer.Phone,
            ["street"] = customer.Street,
            ["city"] = customer.City,
            ["state"] = customer.State,
            ["zip_code"] = customer.ZipCode,
            ["country"] = customer.Country
        };
    }

    /// <summary>
    /// Translate a Product entity to a dictionary for ERP push.
    /// </summary>
    public static Dictionary<string, object?> TranslateProduct(Product product, IReadOnlyList<SyncFieldMapping>? mappings = null)
    {
        if (mappings != null && mappings.Count > 0)
            return ApplyMappings(product, mappings);

        return new Dictionary<string, object?>
        {
            ["name"] = product.Name,
            ["sku"] = product.Sku,
            ["price"] = product.Price,
            ["quantity_in_stock"] = product.QuantityInStock,
            ["category"] = product.Category
        };
    }

    /// <summary>
    /// Translate a Component entity to a dictionary for ERP push.
    /// </summary>
    public static Dictionary<string, object?> TranslateComponent(Component component, IReadOnlyList<SyncFieldMapping>? mappings = null)
    {
        if (mappings != null && mappings.Count > 0)
            return ApplyMappings(component, mappings);

        return new Dictionary<string, object?>
        {
            ["name"] = component.Name,
            ["sku"] = component.Sku,
            ["brand"] = component.Brand,
            ["color"] = component.Color,
            ["component_type"] = component.ComponentType,
            ["price"] = component.Price
        };
    }

    /// <summary>
    /// Translate a ServiceTicket entity to a dictionary for ERP push.
    /// </summary>
    public static Dictionary<string, object?> TranslateServiceTicket(ServiceTicket ticket, IReadOnlyList<SyncFieldMapping>? mappings = null)
    {
        if (mappings != null && mappings.Count > 0)
            return ApplyMappings(ticket, mappings);

        return new Dictionary<string, object?>
        {
            ["ticket_number"] = ticket.TicketDisplay,
            ["status"] = ticket.Status.ToString(),
            ["description"] = ticket.Description,
            ["price"] = ticket.Price,
            ["discount_percent"] = ticket.DiscountPercent,
            ["component_id"] = ticket.ComponentId,
            ["customer_id"] = ticket.CustomerId,
            ["mechanic_id"] = ticket.MechanicId,
            ["created_at"] = ticket.CreatedAt,
            ["updated_at"] = ticket.UpdatedAt
        };
    }

    /// <summary>
    /// Translate a Charge entity to a dictionary for ERP push.
    /// </summary>
    public static Dictionary<string, object?> TranslateCharge(Charge charge, IReadOnlyList<SyncFieldMapping>? mappings = null)
    {
        if (mappings != null && mappings.Count > 0)
            return ApplyMappings(charge, mappings);

        return new Dictionary<string, object?>
        {
            ["amount"] = charge.Amount,
            ["payment_method"] = charge.PaymentMethod.ToString(),
            ["payment_status"] = charge.PaymentStatus.ToString(),
            ["cashier_name"] = charge.CashierName,
            ["charged_at"] = charge.ChargedAt,
            ["ticket_id"] = charge.ServiceTicketId,
            ["notes"] = charge.Notes
        };
    }

    /// <summary>
    /// Apply configured SyncFieldMappings using reflection.
    /// </summary>
    private static Dictionary<string, object?> ApplyMappings(object entity, IReadOnlyList<SyncFieldMapping> mappings)
    {
        var fields = new Dictionary<string, object?>();
        var props = entity.GetType().GetProperties(
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        foreach (var mapping in mappings.OrderBy(m => m.SortOrder))
        {
            var prop = props.FirstOrDefault(p =>
                string.Equals(p.Name, mapping.LocalField, StringComparison.OrdinalIgnoreCase));
            if (prop == null) continue;

            var value = prop.GetValue(entity);
            value = ApplyTransform(value, mapping.TransformExpression);
            fields[mapping.RemoteField] = value;
        }

        return fields;
    }

    private static object? ApplyTransform(object? value, string? transform)
    {
        if (string.IsNullOrEmpty(transform) || value == null) return value;

        return transform.ToLower() switch
        {
            "toupper" => value.ToString()?.ToUpperInvariant(),
            "tolower" => value.ToString()?.ToLowerInvariant(),
            "tostring" => value.ToString(),
            _ => value
        };
    }
}
