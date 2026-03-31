using System.Text.Json.Serialization;

namespace WatchStoreApi.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string ImageUrl { get; set; }
    public string Material { get; set; }
    public string Gender { get; set; }
    public decimal Price { get; set; }


    public int CategoryId { get; set; }
    [JsonIgnore]
    public Category Category { get; set; }


    public ICollection<ShoppingCartItem> ShoppingCartItems { get; set; }

    public ICollection<OrderDetail> OrderDetails { get; set; }

}
