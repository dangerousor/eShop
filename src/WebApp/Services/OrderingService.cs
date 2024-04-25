namespace eShop.WebApp.Services;

public class OrderingService(HttpClient httpClient)
{
    private readonly string remoteServiceBaseUrl = "/api/v1/Orders/";

    public Task<OrderRecord[]> GetOrders()
    {
        return httpClient.GetFromJsonAsync<OrderRecord[]>(remoteServiceBaseUrl)!;
    }
}

public record OrderRecord(
    int OrderNumber,
    DateTime Date,
    string Status,
    decimal Total);
