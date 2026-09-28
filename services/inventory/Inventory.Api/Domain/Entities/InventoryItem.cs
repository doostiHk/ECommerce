using BuildingBlocks.Common.Entities;
namespace Inventory.Api.Domain.Entities;

public class InventoryItem : AuditableEntity
{
    public Guid WarehouseId { get; set; }
    public Guid ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal ReservedQuantity { get; set; }

}
