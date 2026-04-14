namespace OrderingService.Models;

public class User
{
    public Guid Id { get; set; } // Matches the Id from UserService
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Contact { get; set; } = string.Empty;
}