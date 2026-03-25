// WebMCP Tool Registration
// Uses jasonjmcghee/WebMCP to expose component inventory tools to AI agents via Claude Desktop.
// https://github.com/jasonjmcghee/WebMCP

const mcp = new WebMCP({
    color: "#007bff",
    position: "bottom-right",
    size: "30px",
    padding: "20px",
});

// Tool: List all components
mcp.registerTool(
    "list-components",
    "List all components (bikes, rims, pedals, etc.) in the inventory. Returns an array of component objects with id, name, sku, color, brand, componentType, and price.",
    { type: "object", properties: {} },
    async function () {
        const res = await fetch("/api/components");
        return { content: [{ type: "text", text: JSON.stringify(await res.json(), null, 2) }] };
    }
);

// Tool: Get component by ID
mcp.registerTool(
    "get-component",
    "Get details of a specific component by its ID.",
    {
        type: "object",
        properties: {
            id: { type: "number", description: "The component ID" },
        },
        required: ["id"],
    },
    async function (args) {
        const res = await fetch(`/api/components/${args.id}`);
        if (!res.ok) return { content: [{ type: "text", text: "Component not found" }] };
        return { content: [{ type: "text", text: JSON.stringify(await res.json(), null, 2) }] };
    }
);

// Tool: Search components
mcp.registerTool(
    "search-components",
    "Search components by name, brand, color, or SKU. Returns matching components.",
    {
        type: "object",
        properties: {
            query: { type: "string", description: "Search term to match against component name, brand, color, or SKU" },
        },
        required: ["query"],
    },
    async function (args) {
        const res = await fetch(`/api/components/search?query=${encodeURIComponent(args.query)}`);
        return { content: [{ type: "text", text: JSON.stringify(await res.json(), null, 2) }] };
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

// Prompt: Component inventory summary
mcp.registerPrompt(
    "component-summary",
    "Generate a summary of the component inventory",
    [],
    async function () {
        const res = await fetch("/api/components");
        const components = await res.json();
        return {
            messages: [{
                role: "user",
                content: {
                    type: "text",
                    text: `Summarize this component inventory:\n\n${JSON.stringify(components, null, 2)}`,
                },
            }],
        };
    }
);

console.log("[BikePOS] WebMCP tools, resources, and prompts registered.");
