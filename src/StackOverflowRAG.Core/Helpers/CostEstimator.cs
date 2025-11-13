namespace StackOverflowRAG.Core.Helpers;

/// <summary>
/// Helper for estimating API costs
/// </summary>
public static class CostEstimator
{
    // GPT-4o-mini pricing (as of 2025)
    private const double InputTokenCostPerMillion = 0.15;  // $0.15 per 1M input tokens
    private const double OutputTokenCostPerMillion = 0.60; // $0.60 per 1M output tokens

    /// <summary>
    /// Estimates cost for LLM API call
    /// </summary>
    /// <param name="inputTokens">Number of input tokens (prompt)</param>
    /// <param name="outputTokens">Number of output tokens (completion)</param>
    /// <returns>Estimated cost in USD</returns>
    public static double EstimateLlmCost(int inputTokens, int outputTokens)
    {
        var inputCost = (inputTokens / 1_000_000.0) * InputTokenCostPerMillion;
        var outputCost = (outputTokens / 1_000_000.0) * OutputTokenCostPerMillion;
        return inputCost + outputCost;
    }

    /// <summary>
    /// Estimates token count from text (rough approximation: 1 token ≈ 4 characters)
    /// </summary>
    /// <param name="text">Text to estimate</param>
    /// <returns>Estimated token count</returns>
    public static int EstimateTokens(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        // Rough estimation: 1 token ≈ 4 characters for English text
        return text.Length / 4;
    }
}
