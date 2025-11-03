using System;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace GrpcService1.Services;

public class GrpcAuthInterceptor : Interceptor
{
    private readonly ILogger<GrpcAuthInterceptor> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAuthenticationSchemeProvider _schemeProvider;

    public GrpcAuthInterceptor(
        ILogger<GrpcAuthInterceptor> logger,
        IHttpContextAccessor httpContextAccessor,
        IAuthenticationSchemeProvider schemeProvider)
    {
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _schemeProvider = schemeProvider;
    }

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        var methodName = context.Method; // e.g. /auth.AuthService/Login

        // 🚫 Skip authentication for the Login method
        if (methodName.EndsWith("greet.Greeter/Login", StringComparison.OrdinalIgnoreCase))
        {
            return await continuation(request, context);
        }

        var httpContext = _httpContextAccessor.HttpContext;

        // Extract token from metadata
        var authHeader = context.RequestHeaders
            .FirstOrDefault(h => h.Key.Equals("authorization", StringComparison.OrdinalIgnoreCase))?.Value;

        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            _logger.LogWarning("Unauthorized gRPC call to {Method} - Missing token", context.Method);
            throw new RpcException(new Status(StatusCode.Unauthenticated, "Missing or invalid Authorization header"));
        }

        httpContext!.Request.Headers.Authorization = authHeader;

        var result = await httpContext.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme);
        if (!result.Succeeded)
        {
            _logger.LogWarning("Unauthorized gRPC call to {Method} - Invalid token", context.Method);
            throw new RpcException(new Status(StatusCode.Unauthenticated, "Invalid or expired token"));
        }

        // Optionally attach user claims to context
        httpContext.User = result.Principal;

        _logger.LogInformation("✅ Authenticated gRPC user {User} for {Method}",
            httpContext.User.Identity?.Name ?? "Anonymous", context.Method);

        // Continue execution if authenticated
        return await continuation(request, context);
    }
}
