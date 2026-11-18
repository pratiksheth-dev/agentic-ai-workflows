namespace AgenticWorkflows.Rag;

/// <summary>
/// The tiny built-in knowledge base the RAG demo retrieves from. In a real
/// system these documents would be ingested from files, wikis, or databases,
/// chunked, embedded, and stored in a vector database.
/// </summary>
public static class SampleKnowledgeBase
{
    public static IReadOnlyList<KnowledgeDocument> Documents { get; } = new[]
    {
        new KnowledgeDocument(
            "doc-aca",
            "Azure Container Apps overview",
            "Azure Container Apps is a serverless platform for running containerized " +
            "microservices. It handles scaling, ingress, and revisions, supports Dapr " +
            "and KEDA-based autoscaling, and removes the need to manage Kubernetes " +
            "clusters directly. It is a common landing zone when migrating .NET " +
            "applications from a monolith to cloud-native microservices on Azure."),

        new KnowledgeDocument(
            "doc-migration",
            "Monolith to microservices migration strategy",
            "Migrating a monolith should be incremental. Start with an inventory of " +
            "components and dependencies, then extract one bounded context at a time " +
            "using the strangler fig pattern. Keep steps reversible, run old and new " +
            "systems side by side during transition, and migrate data with dual writes " +
            "or change data capture. The main risks are hidden dependencies, data " +
            "coupling, downtime during cutover, and team skill gaps."),

        new KnowledgeDocument(
            "doc-clean-arch",
            "Clean Architecture principles",
            "Clean Architecture organizes a system into concentric layers: domain " +
            "entities at the center, then application use cases, then infrastructure " +
            "and presentation at the edges. Dependencies point inward only, so the " +
            "domain and application layers can be tested without databases or web " +
            "frameworks. Interfaces owned by the inner layers are implemented by " +
            "outer layers, which keeps infrastructure replaceable."),

        new KnowledgeDocument(
            "doc-rag",
            "Retrieval-Augmented Generation (RAG)",
            "RAG grounds a language model in external knowledge. Documents are " +
            "chunked and converted to embeddings, stored in a vector database, and " +
            "retrieved by similarity to the user query at runtime. The retrieved " +
            "passages are injected into the prompt so answers cite real sources " +
            "instead of relying only on model training data. RAG reduces " +
            "hallucination and keeps answers current without retraining the model."),

        new KnowledgeDocument(
            "doc-agents",
            "Multi-agent workflow patterns",
            "Multi-agent systems split a complex task across specialized agents: a " +
            "planner decomposes the goal, worker agents execute the steps, and a " +
            "reviewer validates the output before it is returned. Orchestration can " +
            "be sequential, parallel, or a feedback loop where the reviewer sends " +
            "work back for revision. Clear role prompts and structured hand-offs " +
            "between agents keep the system predictable and debuggable."),

        new KnowledgeDocument(
            "doc-cicd",
            "CI/CD for containerized .NET services",
            "A CI/CD pipeline for containerized services builds the application, runs " +
            "unit tests, publishes a Docker image to a container registry, and " +
            "deploys it through environments with automated checks. Infrastructure " +
            "as code (Bicep or Terraform) keeps cloud resources reproducible. " +
            "Automated rollback and health probes limit the blast radius of a bad " +
            "deployment and reduce downtime risk.")
    };
}
