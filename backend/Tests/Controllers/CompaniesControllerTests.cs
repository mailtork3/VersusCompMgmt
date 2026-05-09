using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Moq;
using CompanyAPI.Controllers;
using CompanyAPI.Models;
using CompanyAPI.Services;
using Microsoft.Extensions.Logging;

namespace CompanyAPI.Tests.Controllers;

[TestClass]
public class CompaniesControllerTests
{
    private Mock<CompanyService> _mockCompanyService;
    private Mock<ILogger<CompaniesController>> _mockLogger;
    private CompaniesController _controller;

    [TestInitialize]
    public void Setup()
    {
        _mockCompanyService = new Mock<CompanyService>(
            MockBehavior.Strict,
            null, null, null);

        _mockLogger = new Mock<ILogger<CompaniesController>>();
        _controller = new CompaniesController(_mockCompanyService.Object, _mockLogger.Object);
    }

    #region CreateCompany Tests

    [TestMethod]
    public async Task CreateCompany_WithValidRequest_Returns201Created()
    {
        // Arrange
        var request = new CreateCompanyRequest { Name = "Apple", WebsiteUrl = "https://www.apple.com" };
        var company = new Company { Id = 1, Name = request.Name, WebsiteUrl = request.WebsiteUrl };

        _mockCompanyService
            .Setup(s => s.CreateCompanyAsync(request.Name, request.WebsiteUrl))
            .ReturnsAsync((true, company, null, 85.0));

        // Act
        var result = await _controller.CreateCompany(request);

        // Assert
        Assert.IsNotNull(result);
        var createdResult = result.Result as CreatedAtActionResult;
        Assert.IsNotNull(createdResult);
        Assert.AreEqual(StatusCodes.Status201Created, createdResult.StatusCode);
        Assert.AreEqual(nameof(CompaniesController.GetCompanyById), createdResult.ActionName);
    }

    [TestMethod]
    public async Task CreateCompany_WithNullName_Returns400BadRequest()
    {
        // Arrange
        var request = new CreateCompanyRequest { Name = null, WebsiteUrl = "https://www.apple.com" };

        // Act
        var result = await _controller.CreateCompany(request);

        // Assert
        var badRequestResult = result.Result as BadRequestObjectResult;
        Assert.IsNotNull(badRequestResult);
        Assert.AreEqual(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
    }

    [TestMethod]
    public async Task CreateCompany_WithNullWebsiteUrl_Returns400BadRequest()
    {
        // Arrange
        var request = new CreateCompanyRequest { Name = "Apple", WebsiteUrl = null };

        // Act
        var result = await _controller.CreateCompany(request);

        // Assert
        var badRequestResult = result.Result as BadRequestObjectResult;
        Assert.IsNotNull(badRequestResult);
        Assert.AreEqual(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
    }

    [TestMethod]
    public async Task CreateCompany_WithEmptyName_Returns400BadRequest()
    {
        // Arrange
        var request = new CreateCompanyRequest { Name = "", WebsiteUrl = "https://www.apple.com" };

        // Act
        var result = await _controller.CreateCompany(request);

        // Assert
        var badRequestResult = result.Result as BadRequestObjectResult;
        Assert.IsNotNull(badRequestResult);
    }

    [TestMethod]
    public async Task CreateCompany_WhenServiceReturnsFalse_Returns400BadRequest()
    {
        // Arrange
        var request = new CreateCompanyRequest { Name = "Apple", WebsiteUrl = "https://www.microsoft.com" };
        var errorMessage = "Company name does not appear relevant to the website URL.";

        _mockCompanyService
            .Setup(s => s.CreateCompanyAsync(request.Name, request.WebsiteUrl))
            .ReturnsAsync((false, null, errorMessage, 0));

        // Act
        var result = await _controller.CreateCompany(request);

        // Assert
        var badRequestResult = result.Result as BadRequestObjectResult;
        Assert.IsNotNull(badRequestResult);
    }

    [TestMethod]
    public async Task CreateCompany_IncludesRelevanceScoreInResponse()
    {
        // Arrange
        var request = new CreateCompanyRequest { Name = "Apple", WebsiteUrl = "https://www.apple.com" };
        var company = new Company { Id = 1, Name = request.Name, WebsiteUrl = request.WebsiteUrl };
        var relevanceScore = 85.5;

        _mockCompanyService
            .Setup(s => s.CreateCompanyAsync(request.Name, request.WebsiteUrl))
            .ReturnsAsync((true, company, null, relevanceScore));

        // Act
        var result = await _controller.CreateCompany(request);

        // Assert
        var createdResult = result.Result as CreatedAtActionResult;
        var response = createdResult.Value as CompanyCreatedResponse;
        Assert.AreEqual(85.5, response.RelevanceScore);
    }

    #endregion

    #region GetAllCompanies Tests

    [TestMethod]
    public async Task GetAllCompanies_WithMultipleCompanies_Returns200Ok()
    {
        // Arrange
        var companies = new List<Company>
        {
            new() { Id = 1, Name = "Apple", WebsiteUrl = "https://www.apple.com" },
            new() { Id = 2, Name = "Microsoft", WebsiteUrl = "https://www.microsoft.com" }
        };

        _mockCompanyService
            .Setup(s => s.GetAllCompaniesAsync())
            .ReturnsAsync(companies);

        // Act
        var result = await _controller.GetAllCompanies();

        // Assert
        var okResult = result.Result as OkObjectResult;
        Assert.IsNotNull(okResult);
        Assert.AreEqual(StatusCodes.Status200OK, okResult.StatusCode);

        var returnedCompanies = okResult.Value as List<CompanyDto>;
        Assert.AreEqual(2, returnedCompanies.Count);
    }

    [TestMethod]
    public async Task GetAllCompanies_WithNoCompanies_Returns200OkWithEmptyList()
    {
        // Arrange
        var companies = new List<Company>();
        _mockCompanyService
            .Setup(s => s.GetAllCompaniesAsync())
            .ReturnsAsync(companies);

        // Act
        var result = await _controller.GetAllCompanies();

        // Assert
        var okResult = result.Result as OkObjectResult;
        Assert.IsNotNull(okResult);
        
        var returnedCompanies = okResult.Value as List<CompanyDto>;
        Assert.AreEqual(0, returnedCompanies.Count);
    }

    #endregion

    #region GetCompanyById Tests

    [TestMethod]
    public async Task GetCompanyById_WithValidId_Returns200Ok()
    {
        // Arrange
        var companyId = 1;
        var company = new Company { Id = companyId, Name = "Apple", WebsiteUrl = "https://www.apple.com" };

        _mockCompanyService
            .Setup(s => s.GetCompanyByIdAsync(companyId))
            .ReturnsAsync(company);

        // Act
        var result = await _controller.GetCompanyById(companyId);

        // Assert
        var okResult = result.Result as OkObjectResult;
        Assert.IsNotNull(okResult);
        Assert.AreEqual(StatusCodes.Status200OK, okResult.StatusCode);
    }

    [TestMethod]
    public async Task GetCompanyById_WithInvalidId_Returns404NotFound()
    {
        // Arrange
        var companyId = 999;
        _mockCompanyService
            .Setup(s => s.GetCompanyByIdAsync(companyId))
            .ReturnsAsync((Company)null);

        // Act
        var result = await _controller.GetCompanyById(companyId);

        // Assert
        var notFoundResult = result.Result as NotFoundObjectResult;
        Assert.IsNotNull(notFoundResult);
        Assert.AreEqual(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
    }

    [TestMethod]
    public async Task GetCompanyById_ReturnsCorrectCompanyData()
    {
        // Arrange
        var companyId = 1;
        var company = new Company { Id = companyId, Name = "Apple", WebsiteUrl = "https://www.apple.com" };

        _mockCompanyService
            .Setup(s => s.GetCompanyByIdAsync(companyId))
            .ReturnsAsync(company);

        // Act
        var result = await _controller.GetCompanyById(companyId);

        // Assert
        var okResult = result.Result as OkObjectResult;
        var returnedCompany = okResult.Value as CompanyDto;
        Assert.AreEqual("Apple", returnedCompany.Name);
        Assert.AreEqual("https://www.apple.com", returnedCompany.WebsiteUrl);
    }

    #endregion

    #region SearchCompanies Tests

    [TestMethod]
    public async Task SearchCompanies_WithName_Returns200Ok()
    {
        // Arrange
        var companies = new List<Company>
        {
            new() { Id = 1, Name = "Apple Inc", WebsiteUrl = "https://www.apple.com" }
        };

        _mockCompanyService
            .Setup(s => s.SearchCompaniesByNameAsync("Apple"))
            .ReturnsAsync(companies);

        // Act
        var result = await _controller.SearchCompanies("Apple", null);

        // Assert
        var okResult = result.Result as OkObjectResult;
        Assert.IsNotNull(okResult);
        Assert.AreEqual(StatusCodes.Status200OK, okResult.StatusCode);
    }

    [TestMethod]
    public async Task SearchCompanies_WithDomain_Returns200Ok()
    {
        // Arrange
        var companies = new List<Company>
        {
            new() { Id = 1, Name = "Apple", WebsiteUrl = "https://www.apple.com" }
        };

        _mockCompanyService
            .Setup(s => s.SearchCompaniesByDomainAsync("apple.com"))
            .ReturnsAsync(companies);

        // Act
        var result = await _controller.SearchCompanies(null, "apple.com");

        // Assert
        var okResult = result.Result as OkObjectResult;
        Assert.IsNotNull(okResult);
    }

    [TestMethod]
    public async Task SearchCompanies_WithNoResults_Returns200OkWithEmptyList()
    {
        // Arrange
        var companies = new List<Company>();
        _mockCompanyService
            .Setup(s => s.SearchCompaniesByNameAsync("NonexistentCompany"))
            .ReturnsAsync(companies);

        // Act
        var result = await _controller.SearchCompanies("NonexistentCompany", null);

        // Assert
        var okResult = result.Result as OkObjectResult;
        var returnedCompanies = okResult.Value as List<CompanyDto>;
        Assert.AreEqual(0, returnedCompanies.Count);
    }

    #endregion
}
