namespace CompanyAPI.Repository;

using CompanyAPI.Models;

public class InMemoryCompanyRepository : ICompanyRepository
{
    private readonly Dictionary<int, Company> _companies = new();
    private int _nextId = 1;
    private readonly object _lock = new();

    public Task<Company> AddAsync(Company company)
    {
        lock (_lock)
        {
            company.Id = _nextId++;
            _companies[company.Id] = company;
            return Task.FromResult(company);
        }
    }

    public Task<Company?> GetByIdAsync(int id)
    {
        lock (_lock)
        {
            _companies.TryGetValue(id, out var company);
            return Task.FromResult(company);
        }
    }

    public Task<List<Company>> GetAllAsync()
    {
        lock (_lock)
        {
            return Task.FromResult(_companies.Values.ToList());
        }
    }

    public Task<List<Company>> SearchByNameAsync(string name)
    {
        lock (_lock)
        {
            var results = _companies.Values
                .Where(c => c.Name.Contains(name, StringComparison.OrdinalIgnoreCase))
                .ToList();
            return Task.FromResult(results);
        }
    }

    public Task<List<Company>> SearchByDomainAsync(string domain)
    {
        lock (_lock)
        {
            var results = _companies.Values
                .Where(c => c.WebsiteUrl.Contains(domain, StringComparison.OrdinalIgnoreCase))
                .ToList();
            return Task.FromResult(results);
        }
    }
}
