using System.Text.Json;
using Google.Protobuf.WellKnownTypes;
using Google.Rpc;
using Grpc.Core;
using StockMarket.Protos;
using Status = Grpc.Core.Status;

namespace StockMarket.Services;

public class StockPriceService(ILogger<StockPriceService> logger) : StockPrice.StockPriceBase
{
    private DateTime _openMarket = DateTime.UtcNow.Date.AddHours(9).AddMinutes(30);
    private DateTime _closeMarket = DateTime.UtcNow.Date.AddHours(22);

    private static int _requestCount;

    private void EnsureMarketIsOpen()
    {
        // _requestCount++;
        // if (_requestCount <= 4)
        // {
        //     logger.LogInformation("Request {count}: {time}", _requestCount, DateTime.UtcNow.TimeOfDay);
        //     switch (_requestCount)
        //     {
        //         case 1:   
        //             throw new RpcException(new Status(StatusCode.Unavailable, "Service unavailable."));
        //         case 2:   
        //             throw new RpcException(new Status(StatusCode.Internal, "Internal error."));
        //         case 3:   
        //             throw new RpcException(new Status(StatusCode.Aborted, "Abort for conflict."));
        //         default:
        //             throw new RpcException(new Status(StatusCode.Unknown, "Unknown error."));
        //     }
        // }

        _requestCount = 0;
        if (DateTime.UtcNow.AddHours(-4) < _openMarket || DateTime.UtcNow.AddHours(-4) > _closeMarket)
        {
            throw new Google.Rpc.Status
            {
                Code = (int)Code.FailedPrecondition,
                Message = "The market is closed",
                Details = { Any.Pack(new PreconditionFailure
                {
                    Violations =
                    {
                        new PreconditionFailure.Types.Violation
                        {
                            Type = "INVALID_OPERATION_TIME",
                            Subject = "Stock Market",
                            Description = "The market is open from 09:30 AM to 05:00 PM"
                        }
                    }
                }) }
            }.ToRpcException();
        }
    }

    private void ValidateStock(UpdateStockPriceRequest stock)
    {
        List<BadRequest.Types.FieldViolation> violations = new();
        if (stock.Price <= 0)
        {
            violations.Add(new BadRequest.Types.FieldViolation
            {
                Field = nameof(UpdateStockPriceRequest.Price),
                Description = "The action price must be greater than 0."
            });

        }
        if (stock.Symbol.Length < 4 || stock.Symbol.Length > 5)
        {
            violations.Add(new BadRequest.Types.FieldViolation
            {
                Field = nameof(UpdateStockPriceRequest.Symbol),
                Description = "The code must be greater than 4 and under 5 letters."
            });
        }
        if(violations.Count > 0)
        {
            throw new Google.Rpc.Status
            {
                Code = (int)Code.InvalidArgument,
                Message = "Invalid request.",
                Details = { 
                    Any.Pack(new BadRequest
                    {
                        FieldViolations =
                        {
                            violations
                        }
                    }) 
                }
            }.ToRpcException();
        }
    }
    public override Task<StockResponse> GetStockPrice(StockRequest request, ServerCallContext context)
    {
        EnsureMarketIsOpen();
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
            throw new RpcException(new Status(StatusCode.Cancelled, "Deadline time error!"));
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
            EnsureMarketIsOpen();
            var stock = requestStream.Current;
            ValidateStock(stock);
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

    public override async Task GetStockPriceBidirectionalStreaming(IAsyncStreamReader<StockRequest> requestStream,
        IServerStreamWriter<StockResponse> responseStream, ServerCallContext context)
    {
        while (await requestStream.MoveNext(context.CancellationToken))
        {
            await responseStream.WriteAsync(await GetStockPrice(requestStream.Current, context));
        }
    }
}