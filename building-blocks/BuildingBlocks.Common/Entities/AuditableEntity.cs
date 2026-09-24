namespace BuildingBlocks.Common.Entities;

public abstract class AuditableEntity : BaseEntity, IAuditableEntity
{
    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedByUserId { get; set; }
    public DateTime? LastModifiedAtUtc { get; set; }
    public string? LastModifiedByUserId { get; set; }
}
