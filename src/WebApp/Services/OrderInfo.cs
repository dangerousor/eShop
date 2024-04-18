using System.ComponentModel.DataAnnotations;

namespace eShop.WebApp.Services;

public class OrderInfo
{
    [Required]
    public string OrderNumber { get; set; } = "TMQM20230224";

    [Required]
    public DateTime OrderDate { get; set; } = DateTime.Today;

    [Required]
    public DateTime CompletionDate { get; set; } = DateTime.Today.AddDays(7);
    
    public class OrderItem
    {
        [Required]
        public int Index { get; set; } = 1;
        
        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        public int Quantity { get; set; } = 1;

        [Required]
        public string Comment { get; set; } = string.Empty;
    }
}
