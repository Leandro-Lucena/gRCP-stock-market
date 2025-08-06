using Grpc.Core;
using Grpc.Core.Interceptors;

namespace StockMarket.Interceptors;

public class LoggingInterceptor(ILogger<LoggingInterceptor> logger) : Interceptor
{
    private void LogCall(MethodType methodType, ServerCallContext context)
    {
        logger.LogWarning($"Starting call. Type: {methodType}. Method: {context.Method}.");
        foreach (var header in context.RequestHeaders)
        {
            logger.LogWarning($"{header.Key}: {header.Value}");
        }
    }
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request, ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        LogCall(MethodType.Unary, context);
        return await continuation(request, context);
    }
    public override async Task ServerStreamingServerHandler<TRequest, TResponse>(TRequest request, IServerStreamWriter<TResponse> responseStream, ServerCallContext context, ServerStreamingServerMethod<TRequest, TResponse> continuation)
    {
        LogCall(MethodType.ServerStreaming, context);
        await continuation(request, responseStream, context);
    }
    public override async Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(IAsyncStreamReader<TRequest> requestStream, ServerCallContext context,
        ClientStreamingServerMethod<TRequest, TResponse> continuation)
    {
        LogCall(MethodType.ClientStreaming, context);
        return await continuation(requestStream, context);
    }    
    public override async Task DuplexStreamingServerHandler<TRequest, TResponse>(IAsyncStreamReader<TRequest> requestStream, IServerStreamWriter<TResponse> responseStream, ServerCallContext context,
        DuplexStreamingServerMethod<TRequest, TResponse> continuation)
    {
        LogCall(MethodType.DuplexStreaming, context);
        await continuation(requestStream, responseStream, context);
    }
}