namespace HyperMapper.Benchmarks.Models;

// ========== SMALL MODELS (single resolver) ==========

/// <summary>
/// Source for small ValueResolver benchmark - single resolver test.
/// </summary>
public class ValueResolverSmallSource
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}

/// <summary>
/// Destination for small ValueResolver benchmark - single resolved property.
/// </summary>
public class ValueResolverSmallDestination
{
    public string FullName { get; set; } = string.Empty;
}

// ========== FULL MODELS (multi-resolver) ==========

/// <summary>
/// Source for full ValueResolver benchmark - multiple resolver test.
/// </summary>
public class ValueResolverSource
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Destination for full ValueResolver benchmark - multiple resolved properties.
/// </summary>
public class ValueResolverDestination
{
    public string FullName { get; set; } = string.Empty;
    public string FormattedAmount { get; set; } = string.Empty;
    public VRStatusEnum StatusEnum { get; set; }
}

/// <summary>
/// Status enum for ValueResolver benchmark.
/// </summary>
public enum VRStatusEnum
{
    Unknown = 0,
    Active = 1,
    Inactive = 2,
    Pending = 3
}
