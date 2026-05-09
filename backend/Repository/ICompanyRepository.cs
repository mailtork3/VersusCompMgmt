namespace CompanyAPI.Repository;

using CompanyAPI.Models;

public interface ICompanyRepository
{
    Task<Company> AddAsync(Company company);
    Task<Company?> GetByIdAsync(int id);
    Task<List<Company>> GetAllAsync();
    Task<List<Company>> SearchByNameAsync(string name);
    Task<List<Company>> SearchByDomainAsync(string domain);
}
