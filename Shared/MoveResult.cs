namespace Shared;

public class MoveResult
{
    public required ResultState Result { get; set; }
    public List<FieldEntry>? Board { get; set; }
    public string? CurrentPlayer { get; set; }
    public string? Winner { get; set; }
}

public enum ResultState
{
    Continue,
    Win,
    InvalidMove,
    Draw
}