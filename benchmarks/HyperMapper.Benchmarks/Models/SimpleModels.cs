namespace HyperMapper.Benchmarks.Models;

/// <summary>
/// Simple flat source object with 5 properties
/// </summary>
public class SimpleSource
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// Simple flat destination object matching source
/// </summary>
public class SimpleDestination
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
}
