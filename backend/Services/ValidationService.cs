namespace CompanyAPI.Services;

public class ValidationService
{
    private readonly RelevanceService _relevanceService;
    private const double RelevanceThreshold = 25.0;
    private const int MinNameLength = 3;

    public ValidationService(RelevanceService relevanceService)
    {
        _relevanceService = relevanceService;
    }

    public ValidationResult ValidateCompany(string name, string websiteUrl)
    {
        // Validate company name
        var nameValidation = ValidateName(name);
        if (!nameValidation.IsValid)
            return nameValidation;

        // Validate website URL
        var urlValidation = ValidateUrl(websiteUrl);
        if (!urlValidation.IsValid)
            return urlValidation;

        // Validate relevance
        var relevanceValidation = ValidateRelevance(name, websiteUrl);
        if (!relevanceValidation.IsValid)
            return relevanceValidation;

        return ValidationResult.Success(relevanceValidation.RelevanceScore);
    }

    private ValidationResult ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return ValidationResult.Failure("Company name must not be empty.");

        if (name.Length < MinNameLength)
            return ValidationResult.Failure($"Company name must contain at least {MinNameLength} characters.");

        return ValidationResult.Success();
    }

    private ValidationResult ValidateUrl(string websiteUrl)
    {
        if (string.IsNullOrWhiteSpace(websiteUrl))
            return ValidationResult.Failure("Website URL must not be empty.");

        if (!Uri.TryCreate(websiteUrl, UriKind.Absolute, out var uri))
            return ValidationResult.Failure("Website URL must be a valid, well-formed URL (e.g., https://www.example.com).");

        if (uri.Scheme != "http" && uri.Scheme != "https")
            return ValidationResult.Failure("Website URL must use HTTP or HTTPS scheme.");

        return ValidationResult.Success();
    }

    private ValidationResult ValidateRelevance(string name, string websiteUrl)
    {
        double relevance = _relevanceService.CalculateRelevance(name, websiteUrl);

        if (relevance < RelevanceThreshold)
            return ValidationResult.Failure($"Company name does not appear relevant to the website URL. Relevance score: {relevance}%.");

        return ValidationResult.Success(relevance);
    }
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public double RelevanceScore { get; set; }

    public static ValidationResult Success(double relevanceScore = 0) => new() { IsValid = true, RelevanceScore = relevanceScore };
    public static ValidationResult Failure(string error) => new() { IsValid = false, ErrorMessage = error };
}
