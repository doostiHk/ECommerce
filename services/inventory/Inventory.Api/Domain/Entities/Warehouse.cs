using BuildingBlocks.Common.Entities;

namespace Inventory.Api.Domain.Entities;

public class Warehouse : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
