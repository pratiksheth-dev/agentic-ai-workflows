using System.Runtime.CompilerServices;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace AgenticWorkflows.AI;

/// <summary>
/// Deterministic, offline stand-in for a real LLM chat service.
///
/// It implements Semantic Kernel's <see cref="IChatCompletionService"/> so the
/// whole workflow runs end-to-end with NO API keys: responses are composed
/// from the caller's input using the role marker in the system prompt
/// ([ROLE:PLANNER], [ROLE:RESEARCHER], [ROLE:REVIEWER]).
///
/// In production this class is deleted and the kernel is registered with
/// Azure OpenAI instead — see the README ("Going to production") section.
/// Nothing else in the codebase changes, because agents only ever talk to
/// <see cref="IChatCompletionService"/> through the kernel.
/// </summary>
public class MockChatCompletionService : IChatCompletionService
{
    /// <inheritdoc />
    public IReadOnlyDictionary<string, object?> Attributes =>
        new Dictionary<string, object?> { ["Provider"] = "Mock (offline demo)" };

    /// <inheritdoc />
    public Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(
        ChatHistory chatHistory,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default)
    {
        var systemPrompt = chatHistory
            .FirstOrDefault(m => m.Role == AuthorRole.System)?.Content ?? string.Empty;
        var userMessage = chatHistory
            .LastOrDefault(m => m.Role == AuthorRole.User)?.Content ?? string.Empty;

        var text = systemPrompt switch
        {
            var s when s.Contains("[ROLE:PLANNER]") => ComposePlan(userMessage),
            var s when s.Contains("[ROLE:RESEARCHER]") => ComposeResearch(userMessage),
            var s when s.Contains("[ROLE:REVIEWER]") => ComposeReview(userMessage),
            _ => "Mock response: no role marker found in the system prompt."
        };

        IReadOnlyList<ChatMessageContent> result =
            new[] { new ChatMessageContent(AuthorRole.Assistant, text) };

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(
        ChatHistory chatHistory,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // The mock does not stream token-by-token; it yields the full reply once.
        var contents = await GetChatMessageContentsAsync(
            chatHistory, executionSettings, kernel, cancellationToken);

        foreach (var content in contents)
        {
            yield return new StreamingChatMessageContent(AuthorRole.Assistant, content.Content);
        }
    }

    // ----- Deterministic response composers --------------------------------
    // Each composer derives its output from the actual input text so the demo
    // behaves sensibly for any topic, while staying fully deterministic.

    private static string ComposePlan(string userMessage)
    {
        var topic = ExtractSection(userMessage, "Topic:");

        return $"""
            Plan for: {topic}

            1. Clarify scope and success criteria for "{topic}".
            2. Inventory the current state: components, dependencies, and constraints.
            3. Retrieve background knowledge relevant to the topic from the knowledge base.
            4. Draft the deliverable in sections, following the outline from step 1.
            5. Review the draft for completeness, accuracy, and risks; revise if needed.
            """;
    }

    private static string ComposeResearch(string userMessage)
    {
        var topic = ExtractSection(userMessage, "Topic:");
        var sourcesText = ExtractSection(userMessage, "Sources:");
        var sources = sourcesText
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(line => line.StartsWith("- "))
            .Select(line => line[2..])
            .ToList();

        var sourceSummary = sources.Count > 0
            ? string.Join("; ", sources)
            : "no matching documents were retrieved";

        return $"""
            Research summary for: {topic}

            Based on {sources.Count} retrieved source(s) ({sourceSummary}), the key points are:

            - Start from a clear inventory of the current state before changing anything;
              most failures in initiatives like this come from unknown dependencies.
            - Prefer incremental, reversible steps over a big-bang change, validating
              each step against the success criteria defined in the plan.
            - Keep the architecture loosely coupled so individual pieces can evolve
              independently, and automate verification (tests, CI/CD) from day one.
            - Track risks explicitly — cost, downtime, data migration, and team
              skill gaps — and assign each risk an owner and a mitigation.

            These findings are grounded in the retrieved context above; in production
            they would be generated by the LLM from the same retrieved passages.
            """;
    }

    private static string ComposeReview(string userMessage)
    {
        var draft = ExtractSection(userMessage, "Draft:");
        var checks = new List<string>();

        checks.Add(draft.Length >= 200
            ? "PASS — Draft is substantive (length and structure look adequate)."
            : "WARN — Draft is short; consider expanding the analysis.");

        checks.Add(draft.Contains("risk", StringComparison.OrdinalIgnoreCase)
            ? "PASS — Risks are addressed."
            : "WARN — Risks are not mentioned; add a risk section.");

        checks.Add(draft.Contains("step", StringComparison.OrdinalIgnoreCase) ||
                   draft.Contains("incremental", StringComparison.OrdinalIgnoreCase)
            ? "PASS — Approach is broken into actionable steps."
            : "WARN — Approach lacks a step-by-step breakdown.");

        var verdict = checks.Any(c => c.StartsWith("WARN")) ? "APPROVED WITH SUGGESTIONS" : "APPROVED";

        return $"""
            Review verdict: {verdict}

            {string.Join("\n", checks)}

            Overall: the draft follows the plan, uses the retrieved knowledge, and is
            safe to hand back to the requester. Address any WARN items in a revision
            pass if higher fidelity is required.
            """;
    }

    /// <summary>
    /// Extracts the text of one labeled section from an agent prompt. Sections
    /// run until the next known label ("Topic:", "Plan:", "Sources:",
    /// "Context:", "Draft:") or the end of the message. The agents format
    /// their prompts with these markers, which keeps the mock parser trivial
    /// and deterministic.
    /// </summary>
    private static string ExtractSection(string message, string label)
    {
        var sectionLabels = new[] { "Topic:", "Plan:", "Sources:", "Context:", "Draft:" };

        var index = message.IndexOf(label, StringComparison.Ordinal);
        if (index < 0)
            return string.Empty;

        var start = index + label.Length;
        var end = message.Length;

        foreach (var other in sectionLabels)
        {
            if (other == label)
                continue;

            var next = message.IndexOf(other, start, StringComparison.Ordinal);
            if (next >= 0 && next < end)
                end = next;
        }

        return message[start..end].Trim();
    }
}
