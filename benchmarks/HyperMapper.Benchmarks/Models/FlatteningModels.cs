namespace HyperMapper.Benchmarks.Models;

/// <summary>
/// Nested source object (3 levels deep) - from AutoMapper benchmark
/// </summary>
public class ModelObject
{
    public DateTime BaseDate { get; set; }
    public ModelSubObject Sub { get; set; } = null!;
    public ModelSubObject Sub2 { get; set; } = null!;
    public ModelSubObject SubWithExtraName { get; set; } = null!;
}

public class ModelSubObject
{
    public string ProperName { get; set; } = string.Empty;
    public ModelSubSubObject? SubSub { get; set; }
}

public class ModelSubSubObject
{
    public string IAmACoolProperty { get; set; } = string.Empty;
}

/// <summary>
/// Flattened destination object
/// </summary>
public class ModelDto
{
    public DateTime BaseDate { get; set; }
    public string SubProperName { get; set; } = string.Empty;
    public string Sub2ProperName { get; set; } = string.Empty;
    public string SubWithExtraNameProperName { get; set; } = string.Empty;
    public string SubSubSubIAmACoolProperty { get; set; } = string.Empty;
}
