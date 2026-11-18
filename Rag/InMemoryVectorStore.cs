namespace AgenticWorkflows.Rag;

/// <summary>A document stored in the demo knowledge base.</summary>
public record KnowledgeDocument(string Id, string Title, string Content);

/// <summary>A retrieved document together with its similarity score (0..1).</summary>
public record SearchResult(KnowledgeDocument Document, double Score);

/// <summary>
/// DEMO STUB — an in-memory stand-in for a real vector database.
///
/// Instead of neural embeddings it builds term-frequency (TF) vectors over
/// the corpus vocabulary and ranks documents by cosine similarity. That is
/// enough to demonstrate the retrieval half of RAG (chunk → embed → search →
/// ground the prompt) with zero infrastructure and zero API keys.
///
/// Production replacement: generate embeddings with Azure OpenAI
/// (text-embedding-3-small) and store/search them in Azure AI Search or
/// another vector store. The workflow only depends on this class's Search
/// signature, so the swap is localized.
/// </summary>
public class InMemoryVectorStore
{
    private readonly List<KnowledgeDocument> _documents = new();

    public void Add(KnowledgeDocument document) => _documents.Add(document);

    public void AddRange(IEnumerable<KnowledgeDocument> documents) =>
        _documents.AddRange(documents);

    public int Count => _documents.Count;

    /// <summary>
    /// Returns the top <paramref name="topK"/> documents most similar to the
    /// query, best first. Documents with zero similarity are omitted.
    /// </summary>
    public IReadOnlyList<SearchResult> Search(string query, int topK = 3)
    {
        if (_documents.Count == 0)
            return Array.Empty<SearchResult>();

        // Vocabulary = every distinct term in the corpus plus the query.
        var vocabulary = _documents
            .SelectMany(d => Tokenize(d.Title + " " + d.Content))
            .Concat(Tokenize(query))
            .Distinct()
            .ToList();

        var queryVector = TermFrequencyVector(query, vocabulary);

        return _documents
            .Select(doc => new SearchResult(
                doc,
                CosineSimilarity(
                    queryVector,
                    TermFrequencyVector(doc.Title + " " + doc.Content, vocabulary))))
            .Where(r => r.Score > 0)
            .OrderByDescending(r => r.Score)
            .Take(topK)
            .ToList();
    }

    /// <summary>Lowercases and splits text into alphanumeric word tokens.</summary>
    private static IEnumerable<string> Tokenize(string text)
    {
        return text
            .ToLowerInvariant()
            .Split(
                new[] { ' ', '\t', '\n', '\r', '.', ',', ';', ':', '(', ')', '-', '/', '"' },
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(token => token.Length > 2);
    }

    /// <summary>Builds a term-frequency vector aligned to the vocabulary.</summary>
    private static double[] TermFrequencyVector(string text, IReadOnlyList<string> vocabulary)
    {
        var counts = Tokenize(text)
            .GroupBy(token => token)
            .ToDictionary(g => g.Key, g => (double)g.Count());

        var vector = new double[vocabulary.Count];
        for (var i = 0; i < vocabulary.Count; i++)
        {
            if (counts.TryGetValue(vocabulary[i], out var count))
                vector[i] = count;
        }

        return vector;
    }

    private static double CosineSimilarity(double[] a, double[] b)
    {
        double dot = 0, magnitudeA = 0, magnitudeB = 0;

        for (var i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            magnitudeA += a[i] * a[i];
            magnitudeB += b[i] * b[i];
        }

        if (magnitudeA == 0 || magnitudeB == 0)
            return 0;

        return dot / (Math.Sqrt(magnitudeA) * Math.Sqrt(magnitudeB));
    }
}
