namespace CompanyAPI.Services;

public class RelevanceService
{
    /// <summary>
    /// Calculate relevance score between company name and website URL using domain/keyword matching.
    /// Returns a score from 0-100.
    /// </summary>
    public virtual double CalculateRelevance(string companyName, string websiteUrl)
    {
        string domain = ExtractDomain(websiteUrl);
        List<string> companyKeywords = ExtractKeywords(companyName);

        if (companyKeywords.Count == 0)
            return 0;

        double matchCount = companyKeywords.Count(keyword =>
            domain.Contains(keyword, StringComparison.OrdinalIgnoreCase));

        double score = (matchCount / companyKeywords.Count) * 100;
        return Math.Round(score, 2);
    }

    /// <summary>
    /// Extract the domain name from a URL (e.g., "apple.com" from "https://www.apple.com")
    /// </summary>
    private string ExtractDomain(string url)
    {
        try
        {
            var uri = new Uri(url);
            var hostParts = uri.Host.Split('.');
            
            // Remove 'www.' prefix if present
            if (hostParts.Length > 1 && hostParts[0].Equals("www", StringComparison.OrdinalIgnoreCase))
            {
                hostParts = hostParts.Skip(1).ToArray();
            }

            // Return base domain (e.g., "apple" from "apple.com")
            return hostParts.Length > 0 ? hostParts[0] : string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Extract meaningful keywords from company name (split by spaces and remove common stop words)
    /// </summary>
    private List<string> ExtractKeywords(string companyName)
    {
        var stopWords = new HashSet<string> { "inc", "ltd", "llc", "corp", "corporation", "company", "co" };
        
        return companyName
            .Split(new[] { ' ', '-', '&', ',' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(word => word.Length > 2 && !stopWords.Contains(word.ToLower()))
            .Select(word => word.ToLower())
            .Distinct()
            .ToList();
    }
}
