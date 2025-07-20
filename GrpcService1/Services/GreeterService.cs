using Grpc.Core;
using GrpcService1;
using Microsoft.EntityFrameworkCore;

namespace GrpcService1.Services;

public class GreeterService : Greeter.GreeterBase
{
    private readonly ILogger<GreeterService> _logger;
    private readonly MyAppDbContext _dbContext;


    public GreeterService(ILogger<GreeterService> logger, MyAppDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public override async Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
    {
        // Look up if this name already exists in the logs
        var existing = await _dbContext.GreetingLogs
            .FirstOrDefaultAsync(g => g.Name == request.Name);

        string greetingMessage;

        if (existing != null)
        {
            greetingMessage = $"Welcome back, {existing.Name} from {existing.City}!";
        }
        else
        {
            greetingMessage = $"Hello {request.Name}, age {request.Age}, from {request.City}";
        }

        // Log this greeting
        var log = new GreetingLog
        {
            Name = request.Name,
            Age = request.Age,
            City = request.City,
            Message = greetingMessage
        };

        _dbContext.GreetingLogs.Add(log);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(greetingMessage);

        return new HelloReply
        {
            Message = greetingMessage,
            Name = request.Name,
            Age = request.Age,
            City = request.City
        };
    }

    public async override Task<HelloReply> GetGreeter(GetGreeterRequest request, ServerCallContext context)
    {
        var greeter = await _dbContext.GreetingLogs
            .FirstOrDefaultAsync(g => g.Id == request.Id) ?? throw new RpcException(new Status(StatusCode.NotFound, $"User {request.Id} not found."));
        return new HelloReply
        {
            Message = greeter?.Message ?? string.Empty,
            Name = greeter?.Name ?? string.Empty,
            Age = greeter?.Age ?? 0,
            City = greeter?.City ?? string.Empty
        };
    }

    public override Task<GoodByeReply> SayGoodBye(GoodByeRequest request, ServerCallContext context)
    {
        var replyMessage = $"Goodbye, {request.Name}. See you next time!";

        return Task.FromResult(new GoodByeReply
        {
            Message = replyMessage
        });
    }
}

