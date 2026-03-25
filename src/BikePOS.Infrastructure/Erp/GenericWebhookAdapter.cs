using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using BikePOS.Interfaces.Services;
using BikePOS.Models;

namespace BikePOS.Infrastructure.Erp;

public class GenericWebhookAdapter : IErpAdapter
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GenericWebhookAdapter> _logger;

    public string ProviderName => "generic_webhook";

    public GenericWebhookAdapter(IHttpClientFactory httpClientFactory, ILogger<GenericWebhookAdapter> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<ErpSyncResult> PushEntityAsync(ErpConnection connection, ErpEntityPayload payload)
    {
        try
        {
            var client = CreateClient(connection);
            var url = $"{connection.BaseUrl?.TrimEnd('/')}/{payload.EntityType.ToLower()}";

            var json = JsonSerializer.Serialize(new
            {
                action = payload.ExternalId != null ? "update" : "create",
                entity_type = payload.EntityType,
                entity_id = payload.EntityId,
                external_id = payload.ExternalId,
                fields = payload.Fields
            });

            var response = await client.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return new ErpSyncResult
                {
                    Success = false,
                    ResponsePayload = responseBody,
                    ErrorMessage = $"HTTP {(int)response.StatusCode}: {responseBody}"
                };
            }

            // Try to extract external ID from response
            string? externalId = null;
            try
            {
                using var doc = JsonDocument.Parse(responseBody);
                if (doc.RootElement.TryGetProperty("id", out var idProp))
                    externalId = idProp.GetString();
                else if (doc.RootElement.TryGetProperty("external_id", out var extIdProp))
                    externalId = extIdProp.GetString();
            }
            catch { /* response may not be JSON */ }

            return new ErpSyncResult
            {
                Success = true,
                ExternalId = externalId,
                ResponsePayload = responseBody
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Webhook push failed for {EntityType} {EntityId}", payload.EntityType, payload.EntityId);
            return new ErpSyncResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<ErpSyncResult> PullEntityAsync(ErpConnection connection, string entityType, string externalId)
    {
        try
        {
            var client = CreateClient(connection);
            var url = $"{connection.BaseUrl?.TrimEnd('/')}/{entityType.ToLower()}/{externalId}";

            var response = await client.GetAsync(url);
            var responseBody = await response.Content.ReadAsStringAsync();

            return new ErpSyncResult
            {
                Success = response.IsSuccessStatusCode,
                ExternalId = externalId,
                ResponsePayload = responseBody,
                ErrorMessage = response.IsSuccessStatusCode ? null : $"HTTP {(int)response.StatusCode}: {responseBody}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Webhook pull failed for {EntityType} {ExternalId}", entityType, externalId);
            return new ErpSyncResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<bool> TestConnectionAsync(ErpConnection connection)
    {
        try
        {
            var client = CreateClient(connection);
            var url = $"{connection.BaseUrl?.TrimEnd('/')}/health";
            var response = await client.GetAsync(url);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public Task<List<string>> GetRemoteFieldsAsync(ErpConnection connection, string entityType)
    {
        return Task.FromResult(new List<string>());
    }

    private HttpClient CreateClient(ErpConnection connection)
    {
        var client = _httpClientFactory.CreateClient("ErpWebhook");
        client.Timeout = TimeSpan.FromSeconds(30);
        if (!string.IsNullOrEmpty(connection.ApiKey))
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", connection.ApiKey);
        }
        return client;
    }
}
