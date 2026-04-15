namespace BikePOS.Api.Endpoints;

public static class AssistantEndpoints
{
    public record ChatMessageDto(string Role, string Content);
    public record ChatRequestDto(List<ChatMessageDto> Messages);
    public record ChatResponseDto(string Content);

    public static void MapAssistantEndpoints(this WebApplication app)
    {
        var g = app.MapGroup("/api/assistant");

        g.MapPost("/chat", async (ChatRequestDto body, CancellationToken ct) =>
        {
            await Task.Delay(600, ct);
            return Results.Ok(new ChatResponseDto(
                "I'm not wired up to an agent yet. Once connected, I'll be able to answer questions about your shop."));
        });
    }
}
