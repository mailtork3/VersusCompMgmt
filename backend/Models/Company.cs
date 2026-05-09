namespace CompanyAPI.Models;

public class Company
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string WebsiteUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
