namespace StackOverflowRAG.Core.Models;

/// <summary>
/// One test question with its known right answer.
/// </summary>
public class EvalCase
{
    public int Id { get; set; }

    /// <summary>
    /// User-style question, rewritten by an LLM from the original post
    /// </summary>
    public string Question { get; set; } = string.Empty;

    /// <summary>
    /// "specific" (own words), "keyword" (exact technical terms) or "vague" (doesn't know the right terms)
    /// </summary>
    public string Style { get; set; } = "specific";

    /// <summary>
    /// The post the question was written from; retrieval should find it
    /// </summary>
    public int ExpectedPostId { get; set; }

    public string OriginalTitle { get; set; } = string.Empty;

    /// <summary>
    /// The post's top-scoring answer, used by the judge to score correctness
    /// </summary>
    public string ReferenceAnswer { get; set; } = string.Empty;
}

/// <summary>
/// The saved test set and its ID.
/// </summary>
public class EvalDataset
{
    /// <summary>
    /// Fingerprint of the test set file; changes whenever the test set is regenerated
    /// </summary>
    public string Id { get; set; } = string.Empty;

    public List<EvalCase> Cases { get; set; } = new();
}

/// <summary>
/// Settings for one evaluation run.
/// </summary>
public class EvalRunRequest
{
    /// <summary>
    /// Number of test questions to use (the first N of the dataset)
    /// </summary>
    public int Size { get; set; } = 50;

    public int TopK { get; set; } = 5;

    public bool UseHybrid { get; set; } = false;

    public bool UseMultiQuery { get; set; } = false;
}

/// <summary>
/// Result for one test question.
/// </summary>
public class EvalCaseResult
{
    public int CaseId { get; set; }
    public string Question { get; set; } = string.Empty;
    public string Style { get; set; } = string.Empty;
    public int ExpectedPostId { get; set; }

    /// <summary>
    /// Extra phrasings searched when multi-query was on
    /// </summary>
    public List<string>? SearchedQueries { get; set; }

    /// <summary>
    /// Distinct post IDs in retrieval order
    /// </summary>
    public List<int> RetrievedPostIds { get; set; } = new();

    /// <summary>
    /// 1-based position of the expected post, or null when it was not retrieved
    /// </summary>
    public int? Rank { get; set; }

    public string Answer { get; set; } = string.Empty;

    // Judge scores, 1-5
    public int Faithfulness { get; set; }
    public int Relevance { get; set; }
    public int Correctness { get; set; }
    public string JudgeReason { get; set; } = string.Empty;

    public long LatencyMs { get; set; }
    public int Tokens { get; set; }
    public double Cost { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Scores for the questions of one style within a run.
/// </summary>
public class EvalStyleScores
{
    public int Count { get; set; }
    public double HitRate { get; set; }
    public double Mrr { get; set; }
    public double AvgFaithfulness { get; set; }
    public double AvgCorrectness { get; set; }
}

/// <summary>
/// A finished run: its settings, averages and per-question results.
/// </summary>
public class EvalRun
{
    public string Id { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public EvalRunRequest Settings { get; set; } = new();

    /// <summary>
    /// Test set this run used; only runs with the same ID are comparable
    /// </summary>
    public string? DatasetId { get; set; }

    /// <summary>
    /// Temperature the answers were generated with
    /// </summary>
    public double AnswerTemperature { get; set; }

    public int CaseCount { get; set; }

    /// <summary>
    /// Share of questions whose expected post was in the top K
    /// </summary>
    public double HitRate { get; set; }

    /// <summary>
    /// Mean reciprocal rank of the expected post (0 when missing)
    /// </summary>
    public double Mrr { get; set; }

    public double AvgFaithfulness { get; set; }
    public double AvgRelevance { get; set; }
    public double AvgCorrectness { get; set; }
    public double AvgLatencyMs { get; set; }
    public double TotalCost { get; set; }
    public int Errors { get; set; }

    /// <summary>
    /// The same scores per question style, to show where a setting helps or hurts
    /// </summary>
    public Dictionary<string, EvalStyleScores> ByStyle { get; set; } = new();

    /// <summary>
    /// Per-question details; left out of run listings
    /// </summary>
    public List<EvalCaseResult>? Results { get; set; }
}
