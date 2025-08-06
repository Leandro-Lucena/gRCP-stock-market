using StockMarket.Interceptors;
using StockMarket.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddGrpc(options =>
{
    options.Interceptors.Add<AuthenticationInterceptor>();
    options.Interceptors.Add<LoggingInterceptor>();
    options.Interceptors.Add<AddTraceIdInterceptor>();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<StockPriceService>();
app.MapGet("/",
    () =>
        "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();