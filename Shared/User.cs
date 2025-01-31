namespace Shared;

public class User
{
    public required string ConnectionId { get; set; }
    public required string Username { get; set; }
    public char MovesAs { get; set; } = '-';
}