using Microsoft.SemanticKernel;

namespace AgenticWorkflows.Agents;

/// <summary>
/// Step 3 — Reviewer. Quality gate for the workflow: checks the researcher's
/// draft for completeness, risk coverage, and actionability, then returns a
/// verdict with any suggested fixes.
/// </summary>
public class ReviewerAgent : AgentBase
{
    private const string SystemPrompt = """
        [ROLE:REVIEWER]
        You are a reviewing agent. Evaluate the draft against the original
        topic and plan. Check completeness, whether risks are addressed, and
        whether the recommendations are actionable. Return a clear verdict.
        """;

    public ReviewerAgent(Kernel kernel) : base(kernel)
    {
    }

    public override string Name => "Reviewer";

    public Task<string> ReviewAsync(
        string topic,
        string draft,
        CancellationToken cancellationToken = default)
    {
        var userMessage = $"""
            Topic: {topic}

            Draft:
            {draft}
            """;

        return AskAsync(SystemPrompt, userMessage, cancellationToken);
    }
}
