public class GreetingLog
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public int Age { get; set; }
    public string? City { get; set; }
    public string? Message { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
