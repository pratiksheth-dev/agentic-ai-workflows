using AgenticWorkflows.Agents;
using AgenticWorkflows.Rag;

namespace AgenticWorkflows.Workflow;

/// <summary>Everything the workflow produced, step by step.</summary>
public record WorkflowResult(
    string Topic,
    string Plan,
    IReadOnlyList<SearchResult> RetrievedContext,
    string Draft,
    string Review);

/// <summary>
/// Sequential multi-agent orchestration:
///
///   Planner ──► (RAG retrieval) ──► Researcher ──► Reviewer
///
/// The planner decomposes the topic, the researcher grounds a draft in
/// passages retrieved from the knowledge base, and the reviewer acts as a
/// quality gate. Each step's output is the next step's input — the classic
/// "chain of agents" pattern, kept deliberately explicit (no framework magic)
/// so the flow is easy to follow and extend.
/// </summary>
public class ResearchWorkflow
{
    private readonly PlannerAgent _planner;
    private readonly ResearchAgent _researcher;
    private readonly ReviewerAgent _reviewer;
    private readonly InMemoryVectorStore _knowledgeBase;
    private readonly Action<string> _log;

    public ResearchWorkflow(
        PlannerAgent planner,
        ResearchAgent researcher,
        ReviewerAgent reviewer,
        InMemoryVectorStore knowledgeBase,
        Action<string>? log = null)
    {
        _planner = planner;
        _researcher = researcher;
        _reviewer = reviewer;
        _knowledgeBase = knowledgeBase;
        _log = log ?? (_ => { });
    }

    public async Task<WorkflowResult> RunAsync(
        string topic,
        CancellationToken cancellationToken = default)
    {
        // Step 1 — plan the work.
        _log($"[1/3] {_planner.Name}: decomposing the topic...");
        var plan = await _planner.PlanAsync(topic, cancellationToken);

        // Step 2 — retrieve grounding knowledge, then draft from it (RAG).
        _log("[RAG] Retrieving relevant passages from the knowledge base...");
        var context = _knowledgeBase.Search(topic, topK: 3);

        _log($"[2/3] {_researcher.Name}: drafting from plan + retrieved context...");
        var draft = await _researcher.ResearchAsync(topic, plan, context, cancellationToken);

        // Step 3 — quality gate.
        _log($"[3/3] {_reviewer.Name}: reviewing the draft...");
        var review = await _reviewer.ReviewAsync(topic, draft, cancellationToken);

        return new WorkflowResult(topic, plan, context, draft, review);
    }
}
