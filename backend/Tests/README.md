# CompanyAPI Unit Tests

This folder contains comprehensive MSTest unit tests for the CompanyAPI backend.

## Test Structure

```
Tests/
├── Services/
│   ├── CompanyServiceTests.cs      # Tests for CompanyService
│   ├── ValidationServiceTests.cs   # Tests for ValidationService
│   └── RelevanceServiceTests.cs    # Tests for RelevanceService
├── Controllers/
│   └── CompaniesControllerTests.cs # Tests for CompaniesController
└── CompanyAPI.Tests.csproj         # Test project file
```

## Test Coverage

### CompanyServiceTests

- **CreateCompanyAsync**: Valid/invalid data handling, relevance scoring
- **GetCompanyByIdAsync**: Valid/invalid ID handling
- **GetAllCompaniesAsync**: Multiple companies, empty list
- **SearchCompaniesByNameAsync**: Name search with various inputs
- **SearchCompaniesByDomainAsync**: Domain search functionality
- **SearchByRelevanceAsync**: Relevance-based search results

### ValidationServiceTests

- **Name Validation**: Empty names, minimum length, null values
- **URL Validation**: Valid HTTP(S) URLs, invalid/empty URLs, unsupported schemes
- **Relevance Validation**: High/low relevance scoring, stop words handling
- **Integration Tests**: All validation criteria combined

### RelevanceServiceTests

- **Domain Extraction**: Various URL formats (HTTPS, HTTP, subdomains, hyphens)
- **Keyword Extraction**: Stop words filtering, special characters, case insensitivity
- **Relevance Calculation**: Exact matches, partial matches, no matches
- **Edge Cases**: Empty inputs, invalid URLs, only stop words
- **Real-World Examples**: LinkedIn, Tesla, AWS scenarios

### CompaniesControllerTests

- **CreateCompany**: Valid requests, validation errors, error responses
- **GetAllCompanies**: Multiple companies, empty lists
- **GetCompanyById**: Valid/invalid IDs, correct data mapping
- **SearchCompanies**: Search by name and domain

## Running the Tests

### Prerequisites

- .NET 8.0 SDK installed
- Visual Studio or VS Code with C# extensions

### Run All Tests

```bash
cd backend/Tests
dotnet test
```

### Run Specific Test Class

```bash
dotnet test --filter "ClassName=CompanyAPI.Tests.Services.CompanyServiceTests"
```

### Run with Verbose Output

```bash
dotnet test --verbosity detailed
```

### Run and Generate Coverage Report

```bash
dotnet test --collect:"XPlat Code Coverage"
```

## Test Statistics

| Test Class               | Total Tests | Coverage Areas                               |
| ------------------------ | ----------- | -------------------------------------------- |
| CompanyServiceTests      | 15          | Service layer logic, data retrieval, search  |
| ValidationServiceTests   | 20          | Input validation, URL validation, relevance  |
| RelevanceServiceTests    | 24          | Domain extraction, keyword matching, scoring |
| CompaniesControllerTests | 13          | HTTP responses, status codes, DTO mapping    |
| **Total**                | **72**      | Controllers, Services, and Business Logic    |

## Test Examples

### Example 1: Test Valid Company Creation

```csharp
[TestMethod]
public async Task CreateCompanyAsync_WithValidData_ReturnsSuccessTrue()
{
    // Arrange
    var name = "Apple Inc";
    var websiteUrl = "https://www.apple.com";

    // Act
    var (success, company, error, score) = await _companyService.CreateCompanyAsync(name, websiteUrl);

    // Assert
    Assert.IsTrue(success);
    Assert.IsNotNull(company);
}
```

### Example 2: Test URL Validation

```csharp
[TestMethod]
public void ValidateCompany_WithInvalidUrl_ReturnsInvalid()
{
    // Arrange
    var result = _validationService.ValidateCompany("Apple", "not-a-url");

    // Assert
    Assert.IsFalse(result.IsValid);
    Assert.IsTrue(result.ErrorMessage.Contains("well-formed URL"));
}
```

### Example 3: Test Relevance Calculation

```csharp
[TestMethod]
public void CalculateRelevance_WithExactMatch_Returns100()
{
    // Arrange & Act
    var score = _relevanceService.CalculateRelevance("Apple", "https://www.apple.com");

    // Assert
    Assert.AreEqual(100.0, score);
}
```

## Mocking Strategy

The tests use **Moq** for mocking dependencies:

- `ICompanyRepository`: Mocked to simulate data access
- `ValidationService`: Mocked to test service in isolation
- `RelevanceService`: Mocked for controller tests
- `ILogger<T>`: Mocked to prevent logging output during tests

## Adding New Tests

When adding new features:

1. Create test methods following the **AAA pattern** (Arrange, Act, Assert)
2. Use `[TestMethod]` attribute for test methods
3. Use descriptive names: `MethodName_Condition_ExpectedResult`
4. Keep tests focused on single responsibility
5. Use mocks to isolate units under test

## Continuous Integration

These tests are designed to run in CI/CD pipelines:

- All tests are independent
- No external dependencies required
- Tests run in < 5 seconds
- Clear failure messages

## Notes

- Tests use **MSTest Framework** (Microsoft's testing framework)
- **Moq** is used for creating mock objects
- All async operations tested properly with `async Task`
- Edge cases and error scenarios covered extensively
