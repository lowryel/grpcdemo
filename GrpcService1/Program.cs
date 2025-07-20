using Grpc.Reflection;
using Grpc.Reflection.V1Alpha;
using GrpcService1.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<MyAppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();

var app = builder.Build();

app.MapGrpcService<GreeterService>();

if (app.Environment.IsDevelopment())
{
    app.MapGrpcReflectionService(); // 👈 enable reflection in development
}

app.Run();
