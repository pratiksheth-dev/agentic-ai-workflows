using Microsoft.SemanticKernel;

namespace AgenticWorkflows.Agents;

/// <summary>
/// Step 1 — Planner. Breaks the incoming topic into an ordered, actionable
/// plan that the rest of the workflow follows.
/// </summary>
public class PlannerAgent : AgentBase
{
    private const string SystemPrompt = """
        [ROLE:PLANNER]
        You are a planning agent. Given a topic, produce a short numbered plan
        (4-6 steps) that a research team can execute. Be concrete and ordered.
        """;

    public PlannerAgent(Kernel kernel) : base(kernel)
    {
    }

    public override string Name => "Planner";

    public Task<string> PlanAsync(string topic, CancellationToken cancellationToken = default)
    {
        return AskAsync(SystemPrompt, $"Topic: {topic}", cancellationToken);
    }
}
