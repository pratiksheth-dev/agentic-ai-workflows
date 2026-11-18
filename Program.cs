// Built by Pratik Sheth — sample portfolio project.
// Multi-agent workflow on Microsoft Semantic Kernel: a Planner, a Researcher
// (grounded by an in-memory RAG demo) and a Reviewer, chained sequentially.
// Runs fully offline — no API keys required (see AI/MockChatCompletionService).

using AgenticWorkflows.Agents;
using AgenticWorkflows.AI;
using AgenticWorkflows.Rag;
using AgenticWorkflows.Workflow;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

// ---------------------------------------------------------------------------
// Kernel setup.
//
// The kernel is registered with a deterministic mock chat service so the
// sample runs anywhere with no secrets. To use a real model instead, replace
// the AddSingleton line with the Azure OpenAI registration shown in the
// README — no other code changes:
//
//     builder.AddAzureOpenAIChatCompletion(
//         deploymentName: "gpt-4o",
//         endpoint: Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")!,
//         apiKey: Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY")!);
// ---------------------------------------------------------------------------
var builder = Kernel.CreateBuilder();
builder.Services.AddSingleton<IChatCompletionService>(new MockChatCompletionService());
var kernel = builder.Build();

// Knowledge base for the RAG demo.
var knowledgeBase = new InMemoryVectorStore();
knowledgeBase.AddRange(SampleKnowledgeBase.Documents);

// Compose the workflow from its agents.
var workflow = new ResearchWorkflow(
    planner: new PlannerAgent(kernel),
    researcher: new ResearchAgent(kernel),
    reviewer: new ReviewerAgent(kernel),
    knowledgeBase: knowledgeBase,
    log: message => Console.WriteLine(message));

// Topic: pass one on the command line, or fall back to the default sample.
var topic = args.Length > 0
    ? string.Join(' ', args)
    : "Migrating a monolithic .NET application to Azure Container Apps";

Console.WriteLine("=== Agentic Workflow Demo (Semantic Kernel, offline mock model) ===");
Console.WriteLine($"Topic: {topic}");
Console.WriteLine($"Knowledge base: {knowledgeBase.Count} documents indexed");
Console.WriteLine();

var result = await workflow.RunAsync(topic);

PrintSection("PLAN (Planner)", result.Plan);

Console.WriteLine("--- RETRIEVED CONTEXT (RAG) ---");
foreach (var hit in result.RetrievedContext)
{
    Console.WriteLine($"  * {hit.Document.Title} (score {hit.Score:F2})");
}
Console.WriteLine();

PrintSection("DRAFT (Researcher)", result.Draft);
PrintSection("REVIEW (Reviewer)", result.Review);

static void PrintSection(string title, string body)
{
    Console.WriteLine($"--- {title} ---");
    Console.WriteLine(body);
    Console.WriteLine();
}
