


using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Dommel; 
    
namespace ReactWithDotNet.VisualDesigner.DbModels;

[Table("Project")]
sealed record ProjectEntity
{
    [Key]
    public int Id { get; init; }
    
    public string Name { get; init; }
    
    public string ConfigAsYaml { get; init; }
}

[Table("Component")]
public sealed record ComponentEntity
{
    // @formatter:off
    
    [Key]
    public int Id { get; init; }
    
    public required int ProjectId { get; init; }
    
    public required string Name { get; init; }
    
    public string RootElementAsYaml { get; init; }

    public required string UserName { get; init; }
    
     public required DateTime LastAccessTime { get; init; }
    
    // @formatter:on
}

[Table("ComponentHistory")]
public sealed record ComponentHistoryEntity
{
    // @formatter:off
    
    [Key]
    public int Id { get; init; }
    
    public required int ProjectId { get; init; }
    
    public required string Name { get; init; }
    
    public string RootElementAsYaml { get; init; }

    public required string UserName { get; init; }
    
     public required DateTime LastAccessTime { get; init; }
    
    // @formatter:on
}

[Table("User")]
public sealed record UserEntity
{
    // @formatter:off
    
    [Key]
    public int Id { get; init; }
    
    public string UserName { get; init; }
    
    public int ProjectId { get; init; }

    public DateTime LastAccessTime { get; init; }
    
    public string LastStateAsYaml { get; init; }
    
    // @formatter:on
}