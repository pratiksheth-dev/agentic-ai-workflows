# Agentic AI Workflows with Semantic Kernel

**Built by Pratik Sheth**

A C# console application that demonstrates a **multi-agent workflow** built on
[Microsoft Semantic Kernel](https://learn.microsoft.com/semantic-kernel/).
Three specialized agents — a **Planner**, a **Researcher**, and a **Reviewer** —
collaborate on a topic in sequence, with the Researcher grounded by a small
**Retrieval-Augmented Generation (RAG)** demo.

The sample runs **fully offline with no API keys**: the model behind the
kernel is a deterministic mock (see below), so anyone can clone and run it.

## Workflow design

```
                    ┌────────────────────┐
   Topic ─────────► │  1. Planner agent  │  decomposes the topic into a plan
                    └─────────┬──────────┘
                              │ plan
                    ┌─────────▼──────────┐
                    │   RAG retrieval    │  top-k passages from the
                    │ (in-memory store)  │  knowledge base, by similarity
                    └─────────┬──────────┘
                              │ plan + context
                    ┌─────────▼──────────┐
                    │ 2. Researcher agent│  drafts a grounded summary
                    └─────────┬──────────┘
                              │ draft
                    ┌─────────▼──────────┐
                    │  3. Reviewer agent │  quality gate: completeness,
                    │                    │  risks, actionability → verdict
                    └─────────┬──────────┘
                              ▼
                        Final output
```

This is the classic sequential "chain of agents" pattern. Each agent has one
job, its own system prompt, and a structured hand-off (labeled prompt
sections), which keeps the system predictable and easy to debug — and easy to
extend with a revision loop (Reviewer → Researcher) or parallel workers.

## Agent roles

| Agent      | Responsibility                                                                 |
| ---------- | ------------------------------------------------------------------------------ |
| Planner    | Breaks the topic into a short, ordered, executable plan.                        |
| Researcher | Combines the plan with RAG-retrieved passages and writes a grounded draft.      |
| Reviewer   | Checks the draft for completeness, risk coverage, and actionability; returns a verdict. |

All agents share an `AgentBase` that talks to the chat service **only**
through Semantic Kernel's `IChatCompletionService` — which is what makes the
backing model swappable.

## The RAG demo

`Rag/InMemoryVectorStore.cs` is a **clearly labeled demo stub** for a vector
database. Instead of neural embeddings it builds term-frequency vectors over
the corpus vocabulary and ranks documents by cosine similarity — enough to
show the retrieval half of RAG (index → search → ground the prompt) with zero
infrastructure. Six sample documents are built in (`Rag/SampleKnowledgeBase.cs`).

## Running it

Prerequisites: [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

```bash
dotnet run
```

Or with your own topic:

```bash
dotnet run -- "Designing a RAG pipeline for internal documentation"
```

## Going to production: Azure OpenAI

The only "fake" part is `AI/MockChatCompletionService.cs`, a deterministic
implementation of `IChatCompletionService`. Replace it with a real model and
nothing else changes. In `Program.cs`:

```csharp
var builder = Kernel.CreateBuilder();

builder.AddAzureOpenAIChatCompletion(
    deploymentName: "gpt-4o",
    endpoint: Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")!,
    apiKey: Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY")!);

var kernel = builder.Build();
```

(Requires the `Microsoft.SemanticKernel.Connectors.AzureOpenAI` package.
Store the key in Azure Key Vault / user secrets — never in source control.)

Similarly, swap the RAG stub for real embeddings and a vector store:

- Embed documents and queries with Azure OpenAI `text-embedding-3-small`.
- Store and search vectors in **Azure AI Search** (or Cosmos DB / PostgreSQL
  with vector support).
- Keep the `Search(query, topK)` shape so the workflow code is untouched.

## Project structure

```
AI/         MockChatCompletionService — offline IChatCompletionService stub
Agents/     AgentBase, PlannerAgent, ResearchAgent, ReviewerAgent
Rag/        InMemoryVectorStore (demo stub), SampleKnowledgeBase
Workflow/   ResearchWorkflow — the sequential orchestration
Program.cs  Kernel setup and console entry point
```

## Author

**Pratik Sheth** — Principal Software Engineer
LinkedIn: https://www.linkedin.com/in/pratik-s-a85a3317
Location: Buena Park, CA

## License

MIT — see [LICENSE](LICENSE).
