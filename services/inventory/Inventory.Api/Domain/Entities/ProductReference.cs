namespace Inventory.Api.Domain.Entities;

public class ProductReference
{
    public Guid ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
