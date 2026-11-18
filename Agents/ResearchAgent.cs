using AgenticWorkflows.Rag;
using Microsoft.SemanticKernel;

namespace AgenticWorkflows.Agents;

/// <summary>
/// Step 2 — Researcher/Summarizer. Takes the plan plus the passages retrieved
/// from the knowledge base (RAG) and produces a grounded draft summary.
/// </summary>
public class ResearchAgent : AgentBase
{
    private const string SystemPrompt = """
        [ROLE:RESEARCHER]
        You are a research agent. Using the plan and the retrieved context,
        write a concise, well-structured summary of the topic. Ground every
        claim in the retrieved context; do not invent facts.
        """;

    public ResearchAgent(Kernel kernel) : base(kernel)
    {
    }

    public override string Name => "Researcher";

    public Task<string> ResearchAsync(
        string topic,
        string plan,
        IReadOnlyList<SearchResult> context,
        CancellationToken cancellationToken = default)
    {
        var sourcesText = context.Count == 0
            ? "(none)"
            : string.Join("\n", context.Select(r =>
                $"- {r.Document.Title} (score {r.Score:F2})"));

        var contextText = context.Count == 0
            ? "(no documents retrieved)"
            : string.Join("\n", context.Select(r =>
                $"[{r.Document.Title}] {r.Document.Content}"));

        var userMessage = $"""
            Topic: {topic}

            Plan:
            {plan}

            Sources:
            {sourcesText}

            Context:
            {contextText}
            """;

        return AskAsync(SystemPrompt, userMessage, cancellationToken);
    }
}
