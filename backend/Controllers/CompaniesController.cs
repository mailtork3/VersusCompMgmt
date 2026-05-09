namespace CompanyAPI.Controllers;

using Microsoft.AspNetCore.Mvc;
using CompanyAPI.Models;
using CompanyAPI.Services;

[ApiController]
[Route("api/[controller]")]
public class CompaniesController : ControllerBase
{
    private readonly CompanyService _companyService;
    private readonly ILogger<CompaniesController> _logger;

    public CompaniesController(CompanyService companyService, ILogger<CompaniesController> logger)
    {
        _companyService = companyService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new company with validation
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CompanyCreatedResponse>> CreateCompany([FromBody] CreateCompanyRequest request)
    {
        if (string.IsNullOrEmpty(request.Name) || string.IsNullOrEmpty(request.WebsiteUrl))
        {
            return BadRequest(new { error = "Company name and website URL are required." });
        }

        var (success, company, error, relevanceScore) = await _companyService.CreateCompanyAsync(request.Name, request.WebsiteUrl);

        if (!success)
        {
            _logger.LogWarning($"Company creation failed: {error}");
            return BadRequest(new { error });
        }

        var response = new CompanyCreatedResponse
        {
            Id = company!.Id,
            Name = company.Name,
            WebsiteUrl = company.WebsiteUrl,
            CreatedAt = company.CreatedAt,
            RelevanceScore = Math.Round(relevanceScore, 1)
        };

        return CreatedAtAction(nameof(GetCompanyById), new { id = company.Id }, response);
    }

    /// <summary>
    /// Retrieve all companies
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CompanyDto>>> GetAllCompanies()
    {
        var companies = await _companyService.GetAllCompaniesAsync();
        var dtos = companies.Select(MapToDto).ToList();
        return Ok(dtos);
    }

    /// <summary>
    /// Retrieve a single company by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CompanyDto>> GetCompanyById(int id)
    {
        var company = await _companyService.GetCompanyByIdAsync(id);
        if (company == null)
        {
            return NotFound(new { error = $"Company with ID {id} not found." });
        }

        return Ok(MapToDto(company));
    }

    /// <summary>
    /// Search companies by name or domain
    /// </summary>
    [HttpGet("search")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CompanyDto>>> SearchCompanies(
        [FromQuery] string? name,
        [FromQuery] string? domain)
    {
        List<Company> results = new();

        if (!string.IsNullOrEmpty(name))
        {
            results = await _companyService.SearchCompaniesByNameAsync(name);
        }
        else if (!string.IsNullOrEmpty(domain))
        {
            results = await _companyService.SearchCompaniesByDomainAsync(domain);
        }

        var dtos = results.Select(MapToDto).ToList();
        return Ok(dtos);
    }

    /// <summary>
    /// Search companies by relevance to a search term
    /// </summary>
    [HttpGet("relevance")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CompanyDto>>> SearchByRelevance(
        [FromQuery] string search)
    {
        if (string.IsNullOrEmpty(search))
        {
            return BadRequest(new { error = "Search term is required." });
        }

        var results = await _companyService.SearchByRelevanceAsync(search);
        var dtos = results.Select(MapToDto).ToList();
        return Ok(dtos);
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy" });
    }

    private CompanyDto MapToDto(Company company) => new()
    {
        Id = company.Id,
        Name = company.Name,
        WebsiteUrl = company.WebsiteUrl,
        CreatedAt = company.CreatedAt
    };
}

public class CreateCompanyRequest
{
    public required string Name { get; set; }
    public required string WebsiteUrl { get; set; }
}

public class CompanyDto
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string WebsiteUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CompanyCreatedResponse
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string WebsiteUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public double RelevanceScore { get; set; }
}
