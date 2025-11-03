using System;
using System.Diagnostics;
using System.Text.Json;
using Grpc.Core;
using Grpc.Core.Interceptors;

namespace GrpcService1.Services;

public class ClientLoggingInterceptor(ILogger<ClientLoggingInterceptor> logger) : Interceptor
{
    private readonly ILogger<ClientLoggingInterceptor> _logger = logger;
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
            TRequest request,
            ServerCallContext context,
            UnaryServerMethod<TRequest, TResponse> continuation)
    {
        var methodName = context.Method;
        var stopwatch = Stopwatch.StartNew();

        var user = context.GetHttpContext().User.Identity?.Name ?? "Anonymous";
        _logger.LogInformation($"👤 User: {user}");
        try
        {
            _logger.LogInformation("➡️  gRPC Request: {Method} | Request: {Request}",
                methodName,
                JsonSerializer.Serialize(request));

            var response = await continuation(request, context);

            stopwatch.Stop();
            _logger.LogInformation("✅  gRPC Response: {Method} | Duration: {Duration} ms | Response: {Response}",
                methodName,
                stopwatch.ElapsedMilliseconds,
                JsonSerializer.Serialize(response));

            return response;
        }
        catch (RpcException rpcEx)
        {
            stopwatch.Stop();
            _logger.LogError(rpcEx, "❌ gRPC Error in {Method} after {Duration} ms: {Message}",
                methodName, stopwatch.ElapsedMilliseconds, rpcEx.Status.Detail);
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "💥 Unexpected error in {Method} after {Duration} ms: {Message}",
                methodName, stopwatch.ElapsedMilliseconds, ex.Message);
            throw;
        }
    }
}
