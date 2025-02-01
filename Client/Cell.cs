namespace Client;

public class Cell
{
    public int Id { get; set; }
    
    public char Value { get; set; }
    
    public Cell(int id, char value)
    {
        Id = id;
        Value = value;
    }
    
}