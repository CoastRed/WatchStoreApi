using System.Text.Json.Serialization;

namespace WatchStoreApi.Models;

public class Order
{
    public int Id { get; set; }
    public string Address { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime OrderDate { get; set; }
    public string Status { get; set; }

    public ICollection<OrderDetail> OrderDetails { get; set; }

    public int UserId { get; set; }
    
    [JsonIgnore]
    public User User { get; set; }
}
