using Xunit;

namespace HyperMapper.IntegrationTests.Tests;

/// <summary>
/// v12.1.0: Tests for automatic qualification of ambiguous static classes.
/// Verifies that Path, File, Math, Convert, etc. are properly qualified in generated code
/// to prevent CS0104 errors when a class name exists in multiple namespaces.
/// </summary>
public class AmbiguousStaticClassesSourceGeneratorTests
{
    #region Test Models

    public class FileInfoSource
    {
        public string FilePath { get; set; } = "";
        public string FileName { get; set; } = "";
        public string Directory { get; set; } = "";
        public string StringValue { get; set; } = "";
        public double NumericValue { get; set; }
    }

    public class FileInfoDestination
    {
        public string Extension { get; set; } = "";
        public string JustFileName { get; set; } = "";
        public string CombinedPath { get; set; } = "";
        public string DirectoryName { get; set; } = "";
        public int ConvertedValue { get; set; }
        public double RoundedValue { get; set; }
        public string CurrentDirectory { get; set; } = "";
        public string MachineName { get; set; } = "";
    }

    #endregion

    #region Profile

    public class AmbiguousStaticClassesProfile : Profile
    {
        public AmbiguousStaticClassesProfile()
        {
            CreateMap<FileInfoSource, FileInfoDestination>()
                // Path.* methods
                .ForMember(d => d.Extension, opt => opt.MapFrom(s => Path.GetExtension(s.FilePath)))
                .ForMember(d => d.JustFileName, opt => opt.MapFrom(s => Path.GetFileName(s.FilePath)))
                .ForMember(d => d.CombinedPath, opt => opt.MapFrom(s => Path.Combine(s.Directory, s.FileName)))
                .ForMember(d => d.DirectoryName, opt => opt.MapFrom(s => Path.GetDirectoryName(s.FilePath) ?? ""))
                // Convert.* methods
                .ForMember(d => d.ConvertedValue, opt => opt.MapFrom(s => Convert.ToInt32(s.StringValue)))
                // Math.* methods
                .ForMember(d => d.RoundedValue, opt => opt.MapFrom(s => Math.Round(s.NumericValue, 2)))
                // Environment.* properties
                .ForMember(d => d.CurrentDirectory, opt => opt.MapFrom(s => Environment.CurrentDirectory))
                .ForMember(d => d.MachineName, opt => opt.MapFrom(s => Environment.MachineName));
        }
    }

    #endregion

    #region Path Tests

    [Fact]
    public void CodeGen_PathGetExtension_ShouldQualifyCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<AmbiguousStaticClassesProfile>());
        var mapper = config.CreateMapper();

        var source = new FileInfoSource
        {
            FilePath = "/path/to/file.txt",
            FileName = "file.txt",
            Directory = "/path/to"
        };

        // Act
        var result = mapper.Map<FileInfoDestination>(source);

        // Assert
        Assert.Equal(".txt", result.Extension);
    }

    [Fact]
    public void CodeGen_PathGetFileName_ShouldQualifyCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<AmbiguousStaticClassesProfile>());
        var mapper = config.CreateMapper();

        var source = new FileInfoSource
        {
            FilePath = "/path/to/document.pdf",
            FileName = "document.pdf",
            Directory = "/path/to"
        };

        // Act
        var result = mapper.Map<FileInfoDestination>(source);

        // Assert
        Assert.Equal("document.pdf", result.JustFileName);
    }

    [Fact]
    public void CodeGen_PathCombine_ShouldQualifyCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<AmbiguousStaticClassesProfile>());
        var mapper = config.CreateMapper();

        var source = new FileInfoSource
        {
            FilePath = "/path/to/file.txt",
            FileName = "newfile.txt",
            Directory = "/another/path"
        };

        // Act
        var result = mapper.Map<FileInfoDestination>(source);

        // Assert
        Assert.Equal(Path.Combine("/another/path", "newfile.txt"), result.CombinedPath);
    }

    [Fact]
    public void CodeGen_PathGetDirectoryName_ShouldQualifyCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<AmbiguousStaticClassesProfile>());
        var mapper = config.CreateMapper();

        var source = new FileInfoSource
        {
            FilePath = "/home/user/documents/report.docx",
            FileName = "report.docx",
            Directory = "/home/user/documents"
        };

        // Act
        var result = mapper.Map<FileInfoDestination>(source);

        // Assert
        Assert.Equal("/home/user/documents", result.DirectoryName);
    }

    #endregion

    #region Convert Tests

    [Fact]
    public void CodeGen_ConvertToInt32_ShouldQualifyCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<AmbiguousStaticClassesProfile>());
        var mapper = config.CreateMapper();

        var source = new FileInfoSource
        {
            FilePath = "/path/file.txt",
            FileName = "file.txt",
            Directory = "/path",
            StringValue = "42"
        };

        // Act
        var result = mapper.Map<FileInfoDestination>(source);

        // Assert
        Assert.Equal(42, result.ConvertedValue);
    }

    #endregion

    #region Math Tests

    [Fact]
    public void CodeGen_MathRound_ShouldQualifyCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<AmbiguousStaticClassesProfile>());
        var mapper = config.CreateMapper();

        var source = new FileInfoSource
        {
            FilePath = "/path/file.txt",
            FileName = "file.txt",
            Directory = "/path",
            StringValue = "0",
            NumericValue = 3.14159
        };

        // Act
        var result = mapper.Map<FileInfoDestination>(source);

        // Assert
        Assert.Equal(3.14, result.RoundedValue);
    }

    #endregion

    #region Environment Tests

    [Fact]
    public void CodeGen_EnvironmentCurrentDirectory_ShouldQualifyCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<AmbiguousStaticClassesProfile>());
        var mapper = config.CreateMapper();

        var source = new FileInfoSource
        {
            FilePath = "/path/file.txt",
            FileName = "file.txt",
            Directory = "/path",
            StringValue = "0"
        };

        // Act
        var result = mapper.Map<FileInfoDestination>(source);

        // Assert
        Assert.Equal(Environment.CurrentDirectory, result.CurrentDirectory);
    }

    [Fact]
    public void CodeGen_EnvironmentMachineName_ShouldQualifyCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<AmbiguousStaticClassesProfile>());
        var mapper = config.CreateMapper();

        var source = new FileInfoSource
        {
            FilePath = "/path/file.txt",
            FileName = "file.txt",
            Directory = "/path",
            StringValue = "0"
        };

        // Act
        var result = mapper.Map<FileInfoDestination>(source);

        // Assert
        Assert.Equal(Environment.MachineName, result.MachineName);
    }

    #endregion

    #region Collection Tests

    [Fact]
    public void CodeGen_AmbiguousClasses_WithCollection_ShouldMapAllItems()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<AmbiguousStaticClassesProfile>());
        var mapper = config.CreateMapper();

        var sources = new List<FileInfoSource>
        {
            new() { FilePath = "/a/file1.txt", FileName = "file1.txt", Directory = "/a", StringValue = "10", NumericValue = 1.111 },
            new() { FilePath = "/b/file2.doc", FileName = "file2.doc", Directory = "/b", StringValue = "20", NumericValue = 2.222 },
            new() { FilePath = "/c/file3.pdf", FileName = "file3.pdf", Directory = "/c", StringValue = "30", NumericValue = 3.333 }
        };

        // Act
        var results = mapper.Map<List<FileInfoDestination>>(sources);

        // Assert
        Assert.Equal(3, results.Count);
        Assert.Equal(".txt", results[0].Extension);
        Assert.Equal(".doc", results[1].Extension);
        Assert.Equal(".pdf", results[2].Extension);
        Assert.Equal(10, results[0].ConvertedValue);
        Assert.Equal(20, results[1].ConvertedValue);
        Assert.Equal(30, results[2].ConvertedValue);
        Assert.Equal(1.11, results[0].RoundedValue);
        Assert.Equal(2.22, results[1].RoundedValue);
        Assert.Equal(3.33, results[2].RoundedValue);
    }

    #endregion
}
