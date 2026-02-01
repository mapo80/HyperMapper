namespace HyperMapper.Benchmarks.Models;

/// <summary>
/// Deep nesting models - 10 levels deep
/// </summary>
public class DeepLevel1Source
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DeepLevel2Source? Level2 { get; set; }
}

public class DeepLevel2Source
{
    public string Value { get; set; } = string.Empty;
    public DeepLevel3Source? Level3 { get; set; }
}

public class DeepLevel3Source
{
    public string Value { get; set; } = string.Empty;
    public DeepLevel4Source? Level4 { get; set; }
}

public class DeepLevel4Source
{
    public string Value { get; set; } = string.Empty;
    public DeepLevel5Source? Level5 { get; set; }
}

public class DeepLevel5Source
{
    public string Value { get; set; } = string.Empty;
    public DeepLevel6Source? Level6 { get; set; }
}

public class DeepLevel6Source
{
    public string Value { get; set; } = string.Empty;
    public DeepLevel7Source? Level7 { get; set; }
}

public class DeepLevel7Source
{
    public string Value { get; set; } = string.Empty;
    public DeepLevel8Source? Level8 { get; set; }
}

public class DeepLevel8Source
{
    public string Value { get; set; } = string.Empty;
    public DeepLevel9Source? Level9 { get; set; }
}

public class DeepLevel9Source
{
    public string Value { get; set; } = string.Empty;
    public DeepLevel10Source? Level10 { get; set; }
}

public class DeepLevel10Source
{
    public string FinalValue { get; set; } = string.Empty;
}

// Destination classes

public class DeepLevel1Destination
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DeepLevel2Destination? Level2 { get; set; }
}

public class DeepLevel2Destination
{
    public string Value { get; set; } = string.Empty;
    public DeepLevel3Destination? Level3 { get; set; }
}

public class DeepLevel3Destination
{
    public string Value { get; set; } = string.Empty;
    public DeepLevel4Destination? Level4 { get; set; }
}

public class DeepLevel4Destination
{
    public string Value { get; set; } = string.Empty;
    public DeepLevel5Destination? Level5 { get; set; }
}

public class DeepLevel5Destination
{
    public string Value { get; set; } = string.Empty;
    public DeepLevel6Destination? Level6 { get; set; }
}

public class DeepLevel6Destination
{
    public string Value { get; set; } = string.Empty;
    public DeepLevel7Destination? Level7 { get; set; }
}

public class DeepLevel7Destination
{
    public string Value { get; set; } = string.Empty;
    public DeepLevel8Destination? Level8 { get; set; }
}

public class DeepLevel8Destination
{
    public string Value { get; set; } = string.Empty;
    public DeepLevel9Destination? Level9 { get; set; }
}

public class DeepLevel9Destination
{
    public string Value { get; set; } = string.Empty;
    public DeepLevel10Destination? Level10 { get; set; }
}

public class DeepLevel10Destination
{
    public string FinalValue { get; set; } = string.Empty;
}
