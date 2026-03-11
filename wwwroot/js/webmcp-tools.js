// WebMCP Tool Registration
// Uses jasonjmcghee/WebMCP to expose bike inventory tools to AI agents via Claude Desktop.
// https://github.com/jasonjmcghee/WebMCP

const mcp = new WebMCP({
    color: "#007bff",
    position: "bottom-right",
    size: "30px",
    padding: "20px",
});

// Tool: List all bikes
mcp.registerTool(
    "list-bikes",
    "List all bikes in the inventory. Returns an array of bike objects with id, name, sku, color, brand, and price.",
    { type: "object", properties: {} },
    async function () {
        const res = await fetch("/api/bikes");
        return { content: [{ type: "text", text: JSON.stringify(await res.json(), null, 2) }] };
    }
);

// Tool: Get bike by ID
mcp.registerTool(
    "get-bike",
    "Get details of a specific bike by its ID.",
    {
        type: "object",
        properties: {
            id: { type: "number", description: "The bike ID" },
        },
        required: ["id"],
    },
    async function (args) {
        const res = await fetch(`/api/bikes/${args.id}`);
        if (!res.ok) return { content: [{ type: "text", text: "Bike not found" }] };
        return { content: [{ type: "text", text: JSON.stringify(await res.json(), null, 2) }] };
    }
);

// Tool: Search bikes
mcp.registerTool(
    "search-bikes",
    "Search bikes by name, brand, color, or SKU. Returns matching bikes.",
    {
        type: "object",
        properties: {
            query: { type: "string", description: "Search term to match against bike name, brand, color, or SKU" },
        },
        required: ["query"],
    },
    async function (args) {
        const res = await fetch(`/api/bikes/search?query=${encodeURIComponent(args.query)}`);
        return { content: [{ type: "text", text: JSON.stringify(await res.json(), null, 2) }] };
    }
);

// Tool: Navigate to bike page
mcp.registerTool(
    "navigate-to-bike",
    "Navigate the user to a specific bike page (list, create, details, edit, or delete).",
    {
        type: "object",
        properties: {
            page: {
                type: "string",
                description: "The page to navigate to: 'list', 'create', 'details', 'edit', or 'delete'",
            },
            id: {
                type: "number",
                description: "The bike ID (required for details, edit, and delete pages)",
            },
        },
        required: ["page"],
    },
    function (args) {
        const routes = {
            list: "/bikes",
            create: "/bikes/create",
            details: `/bikes/details?id=${args.id}`,
            edit: `/bikes/edit?id=${args.id}`,
            delete: `/bikes/delete?id=${args.id}`,
        };
        const url = routes[args.page];
        if (!url) return { content: [{ type: "text", text: "Invalid page" }] };
        window.location.href = url;
        return { content: [{ type: "text", text: `Navigated to ${url}` }] };
    }
);

// Tool: Get current page content
mcp.registerTool(
    "get-page-content",
    "Get a summary of the current page content visible to the user.",
    { type: "object", properties: {} },
    function () {
        const title = document.querySelector("h1")?.textContent || document.title;
        const mainText = document.querySelector("main")?.innerText?.substring(0, 2000) || "";
        return {
            content: [{
                type: "text",
                text: JSON.stringify({ url: window.location.href, title, content: mainText }, null, 2),
            }],
        };
    }
);

// Resource: Current page content
mcp.registerResource(
    "page-content",
    "The current page HTML content",
    { uri: "page://current", mimeType: "text/html" },
    function (uri) {
        return {
            contents: [{
                uri: uri,
                mimeType: "text/html",
                text: document.querySelector("main")?.innerHTML || document.body.innerHTML,
            }],
        };
    }
);

// Prompt: Bike inventory summary
mcp.registerPrompt(
    "bike-summary",
    "Generate a summary of the bike inventory",
    [],
    async function () {
        const res = await fetch("/api/bikes");
        const bikes = await res.json();
        return {
            messages: [{
                role: "user",
                content: {
                    type: "text",
                    text: `Summarize this bike inventory:\n\n${JSON.stringify(bikes, null, 2)}`,
                },
            }],
        };
    }
);

console.log("[WebMCP] Bike inventory tools, resources, and prompts registered.");
