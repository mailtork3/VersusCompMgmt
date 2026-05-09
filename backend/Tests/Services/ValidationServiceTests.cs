using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CompanyAPI.Tests.Services;
using CompanyAPI.Services;

[TestClass]
public class ValidationServiceTests
{
    private ValidationService _validationService;

    [TestInitialize]
    public void Setup()
    {
        var relevanceService = new RelevanceService();
        _validationService = new ValidationService(relevanceService);
    }

    #region ValidateName Tests

    [TestMethod]
    public void ValidateCompany_WithValidNameAndUrl_ReturnsValid()
    {
        // Arrange
        var name = "Apple Inc";
        var websiteUrl = "https://www.apple.com";

        // Act
        var result = _validationService.ValidateCompany(name, websiteUrl);

        // Assert
        Assert.IsTrue(result.IsValid);
        Assert.IsNull(result.ErrorMessage);
    }

    [TestMethod]
    public void ValidateCompany_WithEmptyName_ReturnsInvalid()
    {
        // Arrange
        var name = "";
        var websiteUrl = "https://www.apple.com";

        // Act
        var result = _validationService.ValidateCompany(name, websiteUrl);

        // Assert
        Assert.IsFalse(result.IsValid);
        Assert.IsNotNull(result.ErrorMessage);
        Assert.IsTrue(result.ErrorMessage.Contains("must not be empty"));
    }

    [TestMethod]
    public void ValidateCompany_WithNameTooShort_ReturnsInvalid()
    {
        // Arrange
        var name = "AB";
        var websiteUrl = "https://www.example.com";

        // Act
        var result = _validationService.ValidateCompany(name, websiteUrl);

        // Assert
        Assert.IsFalse(result.IsValid);
        Assert.IsNotNull(result.ErrorMessage);
        Assert.IsTrue(result.ErrorMessage.Contains("at least 3 characters"));
    }

    [TestMethod]
    public void ValidateCompany_WithNameOf3Characters_ReturnsValid()
    {
        // Arrange
        var name = "ABC";
        var websiteUrl = "https://www.abc.com";

        // Act
        var result = _validationService.ValidateCompany(name, websiteUrl);

        // Assert
        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public void ValidateCompany_WithNullName_ReturnsInvalid()
    {
        // Arrange
        var name = (string)null;
        var websiteUrl = "https://www.example.com";

        // Act
        var result = _validationService.ValidateCompany(name, websiteUrl);

        // Assert
        Assert.IsFalse(result.IsValid);
    }

    #endregion

    #region ValidateUrl Tests

    [TestMethod]
    public void ValidateCompany_WithValidHttpsUrl_ReturnsValid()
    {
        // Arrange
        var name = "Microsoft";
        var websiteUrl = "https://www.microsoft.com";

        // Act
        var result = _validationService.ValidateCompany(name, websiteUrl);

        // Assert
        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public void ValidateCompany_WithValidHttpUrl_ReturnsValid()
    {
        // Arrange
        var name = "Example Company";
        var websiteUrl = "http://www.example.com";

        // Act
        var result = _validationService.ValidateCompany(name, websiteUrl);

        // Assert
        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public void ValidateCompany_WithEmptyUrl_ReturnsInvalid()
    {
        // Arrange
        var name = "Apple Inc";
        var websiteUrl = "";

        // Act
        var result = _validationService.ValidateCompany(name, websiteUrl);

        // Assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.ErrorMessage.Contains("must not be empty"));
    }

    [TestMethod]
    public void ValidateCompany_WithInvalidUrl_ReturnsInvalid()
    {
        // Arrange
        var name = "Apple Inc";
        var websiteUrl = "not-a-valid-url";

        // Act
        var result = _validationService.ValidateCompany(name, websiteUrl);

        // Assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.ErrorMessage.Contains("valid, well-formed URL"));
    }

    [TestMethod]
    public void ValidateCompany_WithFtpUrl_ReturnsInvalid()
    {
        // Arrange
        var name = "Apple Inc";
        var websiteUrl = "ftp://ftp.example.com";

        // Act
        var result = _validationService.ValidateCompany(name, websiteUrl);

        // Assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.ErrorMessage.Contains("HTTP or HTTPS scheme"));
    }

    [TestMethod]
    public void ValidateCompany_WithUrlWithoutScheme_ReturnsInvalid()
    {
        // Arrange
        var name = "Apple Inc";
        var websiteUrl = "www.apple.com";

        // Act
        var result = _validationService.ValidateCompany(name, websiteUrl);

        // Assert
        Assert.IsFalse(result.IsValid);
    }

    #endregion

    #region ValidateRelevance Tests

    [TestMethod]
    public void ValidateCompany_WithHighRelevance_ReturnsValid()
    {
        // Arrange - Company name matches domain
        var name = "Apple";
        var websiteUrl = "https://www.apple.com";

        // Act
        var result = _validationService.ValidateCompany(name, websiteUrl);

        // Assert
        Assert.IsTrue(result.IsValid);
        Assert.IsTrue(result.RelevanceScore >= 25.0);
    }

    [TestMethod]
    public void ValidateCompany_WithLowRelevance_ReturnsInvalid()
    {
        // Arrange - Company name doesn't match domain
        var name = "Apple Inc";
        var websiteUrl = "https://www.microsoft.com";

        // Act
        var result = _validationService.ValidateCompany(name, websiteUrl);

        // Assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.ErrorMessage.Contains("relevant to the website URL"));
    }

    [TestMethod]
    public void ValidateCompany_WithMultipleKeywords_CalculatesAverageRelevance()
    {
        // Arrange - Multiple keywords, some matching
        var name = "Coca Cola Enterprises";
        var websiteUrl = "https://www.coca-cola.com";

        // Act
        var result = _validationService.ValidateCompany(name, websiteUrl);

        // Assert
        Assert.IsTrue(result.IsValid);
    }

    #endregion

    #region Integration Tests

    [TestMethod]
    public void ValidateCompany_WithAllValidCriteria_ReturnsSuccess()
    {
        // Arrange
        var name = "Google LLC";
        var websiteUrl = "https://www.google.com";

        // Act
        var result = _validationService.ValidateCompany(name, websiteUrl);

        // Assert
        Assert.IsTrue(result.IsValid);
        Assert.IsNull(result.ErrorMessage);
    }

    [TestMethod]
    public void ValidateCompany_WithAnyInvalidCriteria_ReturnsFails()
    {
        // Arrange
        var name = "Go";  // Too short
        var websiteUrl = "https://www.google.com";

        // Act
        var result = _validationService.ValidateCompany(name, websiteUrl);

        // Assert
        Assert.IsFalse(result.IsValid);
        Assert.IsNotNull(result.ErrorMessage);
    }

    #endregion
}
