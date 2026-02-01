namespace HyperMapper.Benchmarks.Models;

/// <summary>
/// Source object for collection benchmark
/// </summary>
public class CollectionItemSource
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}

/// <summary>
/// Destination object for collection benchmark
/// </summary>
public class CollectionItemDestination
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}

/// <summary>
/// Container with collection
/// </summary>
public class CollectionContainerSource
{
    public string ContainerName { get; set; } = string.Empty;
    public List<CollectionItemSource> Items { get; set; } = new();
}

/// <summary>
/// Destination container with collection
/// </summary>
public class CollectionContainerDestination
{
    public string ContainerName { get; set; } = string.Empty;
    public List<CollectionItemDestination> Items { get; set; } = new();
}
