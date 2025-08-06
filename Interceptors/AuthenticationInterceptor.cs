using Grpc.Core;
using Grpc.Core.Interceptors;

namespace StockMarket.Interceptors;

public class AuthenticationInterceptor : Interceptor
{
    private const string ExpectedToken = "jwt-token";

    private void ValidateAuthentication(ServerCallContext context)
    {
        var authHeader = context.RequestHeaders.FirstOrDefault(h => h.Key == "authorization")?.Value;
        if (authHeader != ExpectedToken)
        {
            throw new RpcException(new Status(StatusCode.Unauthenticated, "Invalid authentication token."));
        }
    }
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request, ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        ValidateAuthentication(context);
        return await continuation(request, context);
    }
    public override async Task ServerStreamingServerHandler<TRequest, TResponse>(TRequest request, IServerStreamWriter<TResponse> responseStream, ServerCallContext context, ServerStreamingServerMethod<TRequest, TResponse> continuation)
    {
        ValidateAuthentication(context);
        await continuation(request, responseStream, context);
    }
    public override async Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(IAsyncStreamReader<TRequest> requestStream, ServerCallContext context,
        ClientStreamingServerMethod<TRequest, TResponse> continuation)
    {
        ValidateAuthentication(context);
        return await continuation(requestStream, context);
    }    
    public override async Task DuplexStreamingServerHandler<TRequest, TResponse>(IAsyncStreamReader<TRequest> requestStream, IServerStreamWriter<TResponse> responseStream, ServerCallContext context,
        DuplexStreamingServerMethod<TRequest, TResponse> continuation)
    {
        ValidateAuthentication(context);
        await continuation(requestStream, responseStream, context);
    }
    
}