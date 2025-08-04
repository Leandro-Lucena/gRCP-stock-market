using System.Text.Json;
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

    public override async Task GetStockPriceServerStreaming(StockRequest request, IServerStreamWriter<StockResponse> responseStream, ServerCallContext context)
    {
        const int max = 5;
        var iterations = 0;
        var deadline = context.Deadline.AddSeconds(-3);
        if (deadline < DateTime.UtcNow)
        {
            throw new RpcException(Status.DefaultCancelled, "Deadline time error!");
        }
        while (!context.CancellationToken.IsCancellationRequested && iterations < max && DateTime.UtcNow < deadline)
        {
            await responseStream.WriteAsync(await GetStockPrice(request, context));
            await Task.Delay(2000);
            iterations++;
        }
    }

    public override async Task<UpdateStockPriceBatchResponse> UpdateStockPriceClientStreaming(IAsyncStreamReader<UpdateStockPriceRequest> requestStream, ServerCallContext context)
    {
        var prices = new List<string>();
        const int batchSize = 2;
        var count = 0;
        while (await requestStream.MoveNext(context.CancellationToken))
        {
            var stock = requestStream.Current;
            logger.LogInformation("New price for {action}: $ {price}", stock.Symbol, stock.Price);
            prices.Add(JsonSerializer.Serialize(stock));
            count++;
            if (count % batchSize == 0)
            {
                logger.LogInformation("Saving batch.");
                await File.AppendAllLinesAsync("stockprices.txt", prices, context.CancellationToken);
                prices.Clear();
            }
        }
        await File.AppendAllLinesAsync("stockprices.txt", prices, context.CancellationToken);
        return new UpdateStockPriceBatchResponse {Message = $"{count} actions done."};
    }
}