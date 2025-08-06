using Grpc.Core;
using Grpc.Core.Interceptors;

namespace StockMarket.Interceptors;

public class AuthenticationInterceptor : Interceptor
{
    public override Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request, ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        return base.UnaryServerHandler(request, context, continuation);
    }
}