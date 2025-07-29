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

    public override async Task<HelloReply> CreateEvent(HelloRequest request, ServerCallContext context)
    {
        // Look up if this name already exists in the logs
        var existing = await _dbContext.GreetingLogs
            .FirstOrDefaultAsync(g => g.Name == request.Name);

        // validate inputs
        if (string.IsNullOrEmpty(request.Name))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Name is required"));
        }
        if (request.Age <= 0)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Age must be a positive integer"));
        }
        if (string.IsNullOrEmpty(request.City))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "City is required"));
        }
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
        try
        {
            var greeter = await _dbContext.GreetingLogs
                .FirstOrDefaultAsync(g => g.Id == request.Id) ?? throw new RpcException(new Status(StatusCode.NotFound, $"User {request.Id} not found."));

            return new HelloReply
            {
                Message = greeter.Message,
                Name = greeter.Name,
                Age = greeter.Age,
                City = greeter.City
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving Greeter with ID {Id}", request.Id);
            throw new RpcException(new Status(StatusCode.Internal, "Failed to retrieve Greeter."));
        }
    }

    public override async Task GetManyGreeters(GetManyGreetersRequest request, IServerStreamWriter<HelloReply> responseStream, ServerCallContext context)
    {
        // Retrieve the requested number of Greeters from your data source (e.g., database)
        var greeters = await _dbContext.GreetingLogs.Take(request.Count).ToListAsync();

        foreach (var greeter in greeters)
        {
            var reply = new HelloReply
            {
                Message = $"Hello from {greeter.Name}!",
                Name = greeter.Name,
                Age = greeter.Age,
                City = greeter.City
            };

            // Write each reply to the response stream
            await responseStream.WriteAsync(reply);
        }
    }

    public async override Task<HelloReply> UpdateGreeter(UpdateGreeterRequest request, ServerCallContext context)
    {
        if (request.Id <= 0)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid ID."));
        }

        var greeter = await _dbContext.GreetingLogs
            .FirstOrDefaultAsync(g => g.Id == request.Id) ??
            throw new RpcException(new Status(StatusCode.NotFound, $"Greeter with ID {request.Id} not found."));

        if (!string.IsNullOrEmpty(request.Name))
        {
            greeter.Name = request.Name;
        }
        if (request.Age > 0)
        {
            greeter.Age = request.Age;
        }
        if (!string.IsNullOrEmpty(request.City))
        {
            greeter.City = request.City;
        }

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Error updating Greeter in the database.");
            throw new RpcException(new Status(StatusCode.Internal, "Failed to update Greeter."));
        }

        var reply = new HelloReply
        {
            Message = $"Greeter with ID {request.Id} updated successfully!",
            Name = greeter.Name,
            Age = greeter.Age,
            City = greeter.City
        };

        return reply;
    }

    public async override Task<GoodByeReply> DeleteGreeter(GetGreeterRequest request, ServerCallContext context)
    {
        if (request.Id <= 0)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid ID."));
        }

        var greeter = await _dbContext.GreetingLogs
            .FirstOrDefaultAsync(g => g.Id == request.Id) ??
            throw new RpcException(new Status(StatusCode.NotFound, $"Greeter with ID {request.Id} not found."));

        _dbContext.GreetingLogs.Remove(greeter);

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Error deleting Greeter from the database.");
            throw new RpcException(new Status(StatusCode.Internal, "Failed to delete Greeter."));
        }

        var reply = new GoodByeReply
        {
            Message = $"Greeter with ID {request.Id} has been deleted successfully!",
        };

        return reply;
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

