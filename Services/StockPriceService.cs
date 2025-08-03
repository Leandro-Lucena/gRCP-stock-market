using Grpc.Core;
using StockMarket.Protos;

namespace StockMarket.Services;

public class StockPriceService(ILogger<StockPriceService> logger) : StockPrice.StockPriceBase
{
    public override Task<StockResponse> GetStockPrice(StockRequest request, ServerCallContext context)
    {
        var random = new Random();
        var price = Math.Round(random.NextDouble() * (300 - 50) + 50, 2);

        var response = new StockResponse
        {
            Symbol = request.Symbol,
            Price = price,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
        
        logger.LogInformation("Request price for {action}: $ {price}", request.Symbol, price);
        
        return Task.FromResult(response);
    }
}