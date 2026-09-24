namespace BuildingBlocks.Common.Entities;

public interface IAuditableEntity
{
    DateTime CreatedAtUtc { get; set; }
    string? CreatedByUserId { get; set; }
    DateTime? LastModifiedAtUtc { get; set; }
    string? LastModifiedByUserId { get; set; }
}
