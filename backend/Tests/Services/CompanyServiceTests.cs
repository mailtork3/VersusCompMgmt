using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace CompanyAPI.Tests.Services;
using CompanyAPI.Models;
using CompanyAPI.Services;
using CompanyAPI.Repository;

[TestClass]
public class CompanyServiceTests
{
    private Mock<ICompanyRepository> _mockRepository;
    private Mock<ValidationService> _mockValidationService;
    private Mock<RelevanceService> _mockRelevanceService;
    private CompanyService _companyService;

    [TestInitialize]
    public void Setup()
    {
        _mockRepository = new Mock<ICompanyRepository>();
        _mockValidationService = new Mock<ValidationService>(new RelevanceService());
        _mockRelevanceService = new Mock<RelevanceService>();
        _companyService = new CompanyService(_mockRepository.Object, _mockValidationService.Object, _mockRelevanceService.Object);
    }

    #region CreateCompanyAsync Tests

    [TestMethod]
    public async Task CreateCompanyAsync_WithValidData_ReturnsSuccessTrue()
    {
        // Arrange
        var name = "Apple Inc";
        var websiteUrl = "https://www.apple.com";
        var company = new Company { Id = 1, Name = name, WebsiteUrl = websiteUrl };

        _mockValidationService
            .Setup(v => v.ValidateCompany(name, websiteUrl))
            .Returns(new ValidationResult { IsValid = true, RelevanceScore = 85.0 });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<Company>()))
            .ReturnsAsync(company);

        // Act
        var (success, result, error, relevanceScore) = await _companyService.CreateCompanyAsync(name, websiteUrl);

        // Assert
        Assert.IsTrue(success);
        Assert.IsNotNull(result);
        Assert.AreEqual(name, result.Name);
        Assert.AreEqual(websiteUrl, result.WebsiteUrl);
        Assert.AreEqual(85.0, relevanceScore);
        Assert.IsNull(error);
    }

    [TestMethod]
    public async Task CreateCompanyAsync_WithInvalidData_ReturnsSuccessFalse()
    {
        // Arrange
        var name = "AB";
        var websiteUrl = "invalid-url";
        var errorMessage = "Company name must contain at least 3 characters.";

        _mockValidationService
            .Setup(v => v.ValidateCompany(name, websiteUrl))
            .Returns(new ValidationResult { IsValid = false, ErrorMessage = errorMessage });

        // Act
        var (success, result, error, relevanceScore) = await _companyService.CreateCompanyAsync(name, websiteUrl);

        // Assert
        Assert.IsFalse(success);
        Assert.IsNull(result);
        Assert.AreEqual(errorMessage, error);
        Assert.AreEqual(0, relevanceScore);
    }

    [TestMethod]
    public async Task CreateCompanyAsync_WithLowRelevance_ReturnsError()
    {
        // Arrange
        var name = "Google";
        var websiteUrl = "https://www.amazon.com";
        var errorMessage = "Company name does not appear relevant to the website URL. Relevance score: 10%.";

        _mockValidationService
            .Setup(v => v.ValidateCompany(name, websiteUrl))
            .Returns(new ValidationResult { IsValid = false, ErrorMessage = errorMessage });

        // Act
        var (success, result, error, relevanceScore) = await _companyService.CreateCompanyAsync(name, websiteUrl);

        // Assert
        Assert.IsFalse(success);
        Assert.IsNull(result);
        Assert.IsNotNull(error);
    }

    #endregion

    #region GetCompanyByIdAsync Tests

    [TestMethod]
    public async Task GetCompanyByIdAsync_WithValidId_ReturnsCompany()
    {
        // Arrange
        var companyId = 1;
        var company = new Company { Id = companyId, Name = "Microsoft", WebsiteUrl = "https://www.microsoft.com" };

        _mockRepository
            .Setup(r => r.GetByIdAsync(companyId))
            .ReturnsAsync(company);

        // Act
        var result = await _companyService.GetCompanyByIdAsync(companyId);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(companyId, result.Id);
        Assert.AreEqual("Microsoft", result.Name);
    }

    [TestMethod]
    public async Task GetCompanyByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var companyId = 999;
        _mockRepository
            .Setup(r => r.GetByIdAsync(companyId))
            .ReturnsAsync((Company)null);

        // Act
        var result = await _companyService.GetCompanyByIdAsync(companyId);

        // Assert
        Assert.IsNull(result);
    }

    #endregion

    #region GetAllCompaniesAsync Tests

    [TestMethod]
    public async Task GetAllCompaniesAsync_WithMultipleCompanies_ReturnsList()
    {
        // Arrange
        var companies = new List<Company>
        {
            new() { Id = 1, Name = "Apple", WebsiteUrl = "https://www.apple.com" },
            new() { Id = 2, Name = "Microsoft", WebsiteUrl = "https://www.microsoft.com" },
            new() { Id = 3, Name = "Google", WebsiteUrl = "https://www.google.com" }
        };

        _mockRepository
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(companies);

        // Act
        var result = await _companyService.GetAllCompaniesAsync();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(3, result.Count);
    }

    [TestMethod]
    public async Task GetAllCompaniesAsync_WithNoCompanies_ReturnsEmptyList()
    {
        // Arrange
        var companies = new List<Company>();
        _mockRepository
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(companies);

        // Act
        var result = await _companyService.GetAllCompaniesAsync();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Count);
    }

    #endregion

    #region SearchCompaniesByNameAsync Tests

    [TestMethod]
    public async Task SearchCompaniesByNameAsync_WithValidName_ReturnsMatchingCompanies()
    {
        // Arrange
        var searchName = "Apple";
        var companies = new List<Company>
        {
            new() { Id = 1, Name = "Apple Inc", WebsiteUrl = "https://www.apple.com" }
        };

        _mockRepository
            .Setup(r => r.SearchByNameAsync(searchName))
            .ReturnsAsync(companies);

        // Act
        var result = await _companyService.SearchCompaniesByNameAsync(searchName);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Count);
    }

    [TestMethod]
    public async Task SearchCompaniesByNameAsync_WithEmptyString_ReturnsEmptyList()
    {
        // Act
        var result = await _companyService.SearchCompaniesByNameAsync(string.Empty);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Count);
    }

    #endregion

    #region SearchCompaniesByDomainAsync Tests

    [TestMethod]
    public async Task SearchCompaniesByDomainAsync_WithValidDomain_ReturnsMatchingCompanies()
    {
        // Arrange
        var searchDomain = "apple.com";
        var companies = new List<Company>
        {
            new() { Id = 1, Name = "Apple Inc", WebsiteUrl = "https://www.apple.com" }
        };

        _mockRepository
            .Setup(r => r.SearchByDomainAsync(searchDomain))
            .ReturnsAsync(companies);

        // Act
        var result = await _companyService.SearchCompaniesByDomainAsync(searchDomain);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Count);
    }

    [TestMethod]
    public async Task SearchCompaniesByDomainAsync_WithNullDomain_ReturnsEmptyList()
    {
        // Act
        var result = await _companyService.SearchCompaniesByDomainAsync(null);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Count);
    }

    #endregion

    #region SearchByRelevanceAsync Tests

    [TestMethod]
    public async Task SearchByRelevanceAsync_WithValidSearchTerm_ReturnsRelevantCompanies()
    {
        // Arrange
        var searchTerm = "Apple";
        var companies = new List<Company>
        {
            new() { Id = 1, Name = "Apple Inc", WebsiteUrl = "https://www.apple.com" },
            new() { Id = 2, Name = "Microsoft", WebsiteUrl = "https://www.microsoft.com" }
        };

        _mockRepository
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(companies);

        _mockRelevanceService
            .Setup(r => r.CalculateRelevance(searchTerm, "https://www.apple.com"))
            .Returns(100.0);
        _mockRelevanceService
            .Setup(r => r.CalculateRelevance(searchTerm, "https://www.microsoft.com"))
            .Returns(0);

        // Act
        var result = await _companyService.SearchByRelevanceAsync(searchTerm);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Count);
    }

    [TestMethod]
    public async Task SearchByRelevanceAsync_WithEmptySearchTerm_ReturnsEmptyList()
    {
        // Act
        var result = await _companyService.SearchByRelevanceAsync(string.Empty);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Count);
    }

    #endregion
}
