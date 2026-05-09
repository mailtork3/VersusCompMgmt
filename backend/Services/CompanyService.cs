namespace CompanyAPI.Services;

using CompanyAPI.Models;
using CompanyAPI.Repository;

public class CompanyService
{
    private readonly ICompanyRepository _repository;
    private readonly ValidationService _validationService;
    private readonly RelevanceService _relevanceService;

    public CompanyService(
        ICompanyRepository repository,
        ValidationService validationService,
        RelevanceService relevanceService)
    {
        _repository = repository;
        _validationService = validationService;
        _relevanceService = relevanceService;
    }

    public virtual async Task<(bool Success, Company? Company, string? Error, double RelevanceScore)> CreateCompanyAsync(string name, string websiteUrl)
    {
        var validation = _validationService.ValidateCompany(name, websiteUrl);
        if (!validation.IsValid)
            return (false, null, validation.ErrorMessage, 0);

        var company = new Company { Name = name, WebsiteUrl = websiteUrl };
        var result = await _repository.AddAsync(company);
        return (true, result, null, validation.RelevanceScore);
    }

    public virtual async Task<Company?> GetCompanyByIdAsync(int id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public virtual async Task<List<Company>> GetAllCompaniesAsync()
    {
        return await _repository.GetAllAsync();
    }

    public virtual async Task<List<Company>> SearchCompaniesByNameAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return new List<Company>();

        return await _repository.SearchByNameAsync(name);
    }

    public virtual async Task<List<Company>> SearchCompaniesByDomainAsync(string domain)
    {
        if (string.IsNullOrWhiteSpace(domain))
            return new List<Company>();

        return await _repository.SearchByDomainAsync(domain);
    }

    public virtual async Task<List<Company>> SearchByRelevanceAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return new List<Company>();

        var allCompanies = await _repository.GetAllAsync();

        // Score each company by relevance to search term
        var scored = allCompanies
            .Select(c => new
            {
                Company = c,
                RelevanceScore = _relevanceService.CalculateRelevance(searchTerm, c.WebsiteUrl)
            })
            .Where(x => x.RelevanceScore > 0)  // Only include matches
            .OrderByDescending(x => x.RelevanceScore)
            .Select(x => x.Company)
            .ToList();

        return scored;
    }
}
