namespace OrderingService.Models;

public class InboxMessage
{
    public Guid Id { get; set; }
    public DateTime ProcessedOn { get; set; }
}