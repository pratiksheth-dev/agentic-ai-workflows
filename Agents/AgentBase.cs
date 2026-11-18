using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace AgenticWorkflows.Agents;

/// <summary>
/// Base class for the workflow's agents. Each agent is a single-purpose role
/// with its own system prompt; all of them talk to the chat service through
/// the Semantic Kernel, so the backing model (mock, Azure OpenAI, OpenAI...)
/// can be swapped without touching agent code.
/// </summary>
public abstract class AgentBase
{
    protected AgentBase(Kernel kernel)
    {
        Kernel = kernel;
    }

    protected Kernel Kernel { get; }

    /// <summary>The role name shown in logs and output (e.g. "Planner").</summary>
    public abstract string Name { get; }

    /// <summary>
    /// Sends a system + user message pair to the chat service and returns the
    /// assistant's reply text.
    /// </summary>
    protected async Task<string> AskAsync(
        string systemPrompt,
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        var chatService = Kernel.GetRequiredService<IChatCompletionService>();

        var history = new ChatHistory(systemPrompt);
        history.AddUserMessage(userMessage);

        var response = await chatService.GetChatMessageContentAsync(
            history, cancellationToken: cancellationToken);

        return response.Content ?? string.Empty;
    }
}
