using System.Text.Json;

namespace BikePOS.Api.Endpoints;

public static class AssistantEndpoints
{
    public record ChatMessageDto(string Role, string Content);
    public record ChatRequestDto(List<ChatMessageDto> Messages);
    public record SuggestDescriptionDto(string Bike, string Service);
    public record SuggestDescriptionResponse(string Suggestion);
    public record SummarizeReportDto(string Report, string From, string To);

    public static void MapAssistantEndpoints(this WebApplication app)
    {
        var g = app.MapGroup("/api/assistant");

        g.MapPost("/suggest-ticket-description", (SuggestDescriptionDto body) =>
        {
            var bike = string.IsNullOrWhiteSpace(body.Bike) ? "the bike" : body.Bike;
            var service = string.IsNullOrWhiteSpace(body.Service) ? "Service" : body.Service;
            var suggestion = $"{service} on {bike}: inspect drivetrain, true wheels, adjust derailleurs, " +
                             $"check brake pads and cable tension, lubricate chain, confirm tire pressure. " +
                             $"Flag any worn components for customer approval before replacing.";
            return Results.Ok(new SuggestDescriptionResponse(suggestion));
        });

        g.MapPost("/summarize-report", async (SummarizeReportDto body, HttpContext http, CancellationToken ct) =>
        {
            http.Response.Headers.ContentType = "text/event-stream";
            http.Response.Headers.CacheControl = "no-cache";
            http.Response.Headers["X-Accel-Buffering"] = "no";

            var (opener, tool, narrative) = body.Report switch
            {
                "services" => (
                    $"Looking at service revenue from {body.From} to {body.To}. ",
                    "get_service_revenue",
                    "Tune-ups lead the mix, followed by wheel truing and brake jobs. Consider promoting tune-ups — they drive the most revenue per labor hour."),
                "mechanics" => (
                    $"Reviewing mechanic productivity from {body.From} to {body.To}. ",
                    "get_mechanic_productivity",
                    "Throughput is steady across the team. Average completion time sits around 24 hours — any ticket stuck past 72h is worth a nudge."),
                _ => (
                    $"Summarizing daily sales from {body.From} to {body.To}. ",
                    "get_daily_sales",
                    "Revenue trends are healthy with a card-heavy payment mix. Weekends pull ahead — good candidates for product-heavy service bundles."),
            };

            foreach (var chunk in Chunk(opener))
            {
                await WriteEvent(http, "text_delta", new { text = chunk });
                await Task.Delay(25, ct);
            }
            await WriteEvent(http, "tool_call_start",
                new { id = "call_1", name = tool, args = new { from = body.From, to = body.To } });
            await Task.Delay(500, ct);
            await WriteEvent(http, "tool_call_end",
                new { id = "call_1", result = "Aggregated rows." });
            await Task.Delay(200, ct);
            foreach (var chunk in Chunk(narrative))
            {
                await WriteEvent(http, "text_delta", new { text = chunk });
                await Task.Delay(25, ct);
            }
            await WriteEvent(http, "done", new { });
        });

        g.MapPost("/chat", async (ChatRequestDto body, HttpContext http, CancellationToken ct) =>
        {
            http.Response.Headers.ContentType = "text/event-stream";
            http.Response.Headers.CacheControl = "no-cache";
            http.Response.Headers["X-Accel-Buffering"] = "no";

            var lastUser = body.Messages.LastOrDefault(m =>
                string.Equals(m.Role, "user", StringComparison.OrdinalIgnoreCase))?.Content ?? "";
            var script = BuildScript(lastUser);

            foreach (var ev in script)
            {
                if (ct.IsCancellationRequested) break;
                await WriteEvent(http, ev.Type, ev.Payload);
                await Task.Delay(ev.DelayMs, ct);
            }
            await WriteEvent(http, "done", new { });
        });

        g.MapPost("/reports-chat", async (ChatRequestDto body, HttpContext http, CancellationToken ct) =>
        {
            http.Response.Headers.ContentType = "text/event-stream";
            http.Response.Headers.CacheControl = "no-cache";
            http.Response.Headers["X-Accel-Buffering"] = "no";

            var lastUser = body.Messages.LastOrDefault(m =>
                string.Equals(m.Role, "user", StringComparison.OrdinalIgnoreCase))?.Content ?? "";

            foreach (var ev in BuildReportsScript(lastUser))
            {
                if (ct.IsCancellationRequested) break;
                await WriteEvent(http, ev.Type, ev.Payload);
                await Task.Delay(ev.DelayMs, ct);
            }
            await WriteEvent(http, "done", new { });
        });
    }

    private static IEnumerable<StreamEvent> BuildReportsScript(string userMessage)
    {
        var lower = userMessage.ToLowerInvariant();
        var today = DateTime.UtcNow.Date;
        string Fmt(DateTime d) => d.ToString("yyyy-MM-dd");

        // Date range intents
        (DateTime from, DateTime to, string label)? range = null;
        if (lower.Contains("last 7") || lower.Contains("last week") || lower.Contains("past week"))
            range = (today.AddDays(-6), today, "the last 7 days");
        else if (lower.Contains("last 30") || lower.Contains("last month") || lower.Contains("past month"))
            range = (today.AddDays(-29), today, "the last 30 days");
        else if (lower.Contains("this month"))
            range = (new DateTime(today.Year, today.Month, 1), today, "this month");
        else if (lower.Contains("this year") || lower.Contains("ytd") || lower.Contains("year to date"))
            range = (new DateTime(today.Year, 1, 1), today, "year-to-date");
        else if (lower.Contains("yesterday"))
            range = (today.AddDays(-1), today.AddDays(-1), "yesterday");
        else if (lower.Contains("today"))
            range = (today, today, "today");

        string? tab = null;
        string? tabLabel = null;
        if (lower.Contains("service") || lower.Contains("top service"))
        { tab = "services"; tabLabel = "service revenue"; }
        else if (lower.Contains("mechanic") || lower.Contains("throughput") || lower.Contains("productivity"))
        { tab = "mechanics"; tabLabel = "mechanic productivity"; }
        else if (lower.Contains("daily") || lower.Contains("sales") || lower.Contains("revenue"))
        { tab = "daily"; tabLabel = "daily sales"; }

        if (range is null && tab is null)
        {
            foreach (var chunk in Chunk("I can change the date range (try \"last 7 days\", \"this month\", \"YTD\") or focus a section (\"top services\", \"mechanic productivity\", \"daily sales\"). What would you like to see?"))
                yield return new StreamEvent("text_delta", new { text = chunk }, 20);
            yield break;
        }

        var opener = (range, tab) switch
        {
            ({ } r, not null) => $"Switching to {tabLabel} for {r.label}. ",
            ({ } r, null)    => $"Pulling {r.label}. ",
            (null, not null) => $"Focusing on {tabLabel}. ",
            _                => "",
        };
        foreach (var chunk in Chunk(opener))
            yield return new StreamEvent("text_delta", new { text = chunk }, 20);

        if (range is { } rr)
        {
            yield return new StreamEvent("ui_action",
                new { action = "set_date_range", from = Fmt(rr.from), to = Fmt(rr.to) }, 120);
        }
        if (tab is not null)
        {
            yield return new StreamEvent("ui_action",
                new { action = "select_tab", tab }, 120);
        }

        // Inline chart
        if (lower.Contains("chart") || lower.Contains("graph") || lower.Contains("plot"))
        {
            var chartKind = tab == "services" || tab == "mechanics" ? "bar" : "line";
            var (data, xKey, series) = tab switch
            {
                "services" => (
                    (object)new[] {
                        new { name = "Tune-up", value = 1240 },
                        new { name = "Wheel truing", value = 860 },
                        new { name = "Brake job", value = 720 },
                        new { name = "Chain swap", value = 520 },
                    },
                    "name",
                    new[] { new { key = "value", label = "Revenue", color = "var(--chart-1)" } }),
                "mechanics" => (
                    new[] {
                        new { name = "Ana", value = 14 },
                        new { name = "Marco", value = 11 },
                        new { name = "Leo", value = 9 },
                    },
                    "name",
                    new[] { new { key = "value", label = "Tickets", color = "var(--chart-2)" } }),
                _ => (
                    new[] {
                        new { name = "Mon", value = 320 },
                        new { name = "Tue", value = 410 },
                        new { name = "Wed", value = 280 },
                        new { name = "Thu", value = 520 },
                        new { name = "Fri", value = 610 },
                        new { name = "Sat", value = 740 },
                        new { name = "Sun", value = 390 },
                    },
                    "name",
                    new[] { new { key = "value", label = "Revenue", color = "var(--chart-1)" } }),
            };
            yield return new StreamEvent("chart",
                new { kind = chartKind, title = tabLabel ?? "Preview", data, xKey, series }, 200);
        }

        // CSV export
        if (lower.Contains("csv") || lower.Contains("export") || lower.Contains("download"))
        {
            var (filename, content) = tab switch
            {
                "services" => (
                    $"services-{Fmt(range?.from ?? today)}-{Fmt(range?.to ?? today)}.csv",
                    "Service,Revenue,Tickets\nTune-up,1240,12\nWheel truing,860,8\nBrake job,720,6\nChain swap,520,4\n"),
                "mechanics" => (
                    $"mechanics-{Fmt(range?.from ?? today)}-{Fmt(range?.to ?? today)}.csv",
                    "Mechanic,Tickets,AvgHours\nAna,14,22.5\nMarco,11,26.1\nLeo,9,30.4\n"),
                _ => (
                    $"daily-{Fmt(range?.from ?? today)}-{Fmt(range?.to ?? today)}.csv",
                    "Date,Revenue,Transactions\n2026-04-09,320,3\n2026-04-10,410,4\n2026-04-11,280,2\n2026-04-12,520,5\n2026-04-13,610,6\n2026-04-14,740,7\n2026-04-15,390,3\n"),
            };
            yield return new StreamEvent("download",
                new { filename, mime = "text/csv", content }, 150);
        }

        var closer = tab switch
        {
            "services"  => "Top services are ranked in the chart on the right. Say \"mechanics\" to flip views.",
            "mechanics" => "Mechanics are ordered by tickets charged. Ask me to zoom into a specific window if needed.",
            "daily"     => "Daily revenue is broken out by payment method. Cash/card/transfer stack in the area chart.",
            _           => "Updated the range — charts and KPIs are refreshing now.",
        };
        foreach (var chunk in Chunk(closer))
            yield return new StreamEvent("text_delta", new { text = chunk }, 25);
    }

    private record StreamEvent(string Type, object Payload, int DelayMs);

    private static IEnumerable<StreamEvent> BuildScript(string userMessage)
    {
        var lower = userMessage.ToLowerInvariant();

        if (lower.Contains("ticket"))
        {
            foreach (var chunk in Chunk("Let me pull up the ticket list for you. "))
                yield return new StreamEvent("text_delta", new { text = chunk }, 30);

            yield return new StreamEvent("tool_call_start",
                new { id = "call_1", name = "list_tickets", args = new { status = "Open" } }, 400);
            yield return new StreamEvent("tool_call_end",
                new { id = "call_1", result = "Found 3 open tickets." }, 200);

            foreach (var chunk in Chunk("You have 3 tickets in progress — TKT-042, TKT-043, TKT-044. Want details on any of them?"))
                yield return new StreamEvent("text_delta", new { text = chunk }, 25);
        }
        else if (lower.Contains("sales") || lower.Contains("revenue"))
        {
            foreach (var chunk in Chunk("Checking today's numbers. "))
                yield return new StreamEvent("text_delta", new { text = chunk }, 30);

            yield return new StreamEvent("tool_call_start",
                new { id = "call_1", name = "get_daily_sales", args = new { from = "2026-04-15", to = "2026-04-15" } }, 500);
            yield return new StreamEvent("tool_call_end",
                new { id = "call_1", result = "Revenue: $1,240 across 8 transactions." }, 200);

            foreach (var chunk in Chunk("Today so far: $1,240 in revenue across 8 transactions. Solid day."))
                yield return new StreamEvent("text_delta", new { text = chunk }, 25);
        }
        else
        {
            foreach (var chunk in Chunk("I'm Cadence — your shop assistant. I can check tickets, look up customers, track sales, and add products to work orders. What do you need?"))
                yield return new StreamEvent("text_delta", new { text = chunk }, 30);
        }
    }

    private static IEnumerable<string> Chunk(string s)
    {
        var words = s.Split(' ');
        for (int i = 0; i < words.Length; i++)
            yield return i == words.Length - 1 ? words[i] : words[i] + " ";
    }

    private static async Task WriteEvent(HttpContext http, string type, object payload)
    {
        var json = JsonSerializer.Serialize(new { type, data = payload });
        await http.Response.WriteAsync($"data: {json}\n\n");
        await http.Response.Body.FlushAsync();
    }
}
