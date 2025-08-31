using MongoDB.Driver;
using SchedulerAPI.Models;
using Microsoft.Extensions.Options;
 
namespace SchedulerAPI.Services;

public class UserService
{
    private readonly IMongoCollection<User> _users;

    public UserService(IOptions<MongoDbSettings> settings)
    {   if (string.IsNullOrEmpty(settings.Value.MONGODB_URI))
        throw new ArgumentException("MongoDB connection string is not set!");
        var client = new MongoClient(settings.Value.MONGODB_URI);
        var db = client.GetDatabase(settings.Value.DatabaseName);
        _users = db.GetCollection<User>("users");

        // Ensure unique email
        var indexKeys = Builders<User>.IndexKeys.Ascending(u => u.Email);
        _users.Indexes.CreateOne(new CreateIndexModel<User>(indexKeys, new CreateIndexOptions { Unique = true }));
    }

    public async Task<User?> GetByEmailAsync(string email) =>
        await _users.Find(u => u.Email == email).FirstOrDefaultAsync();

    public async Task<User> CreateAsync(User user)
    {
        await _users.InsertOneAsync(user);
        return user;
    }
}
