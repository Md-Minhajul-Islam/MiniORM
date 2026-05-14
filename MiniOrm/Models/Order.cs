using MiniOrm.Attributes;

namespace MiniOrm.Models;

[Table("orders")]
public class Order
{
    [PrimaryKey]
    public int Id { get; set; }

    [Column("product_id")]
    public int ProductId { get; set; }

    [Column("quantity")]
    public int Quantity { get; set; }

    [Column("order_date")]
    public DateTime OrderDate { get; set; }

}
 
