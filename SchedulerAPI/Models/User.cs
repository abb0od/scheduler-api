using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SchedulerAPI.Models;

public enum UserType
{
    Normal,
    Business
}

public class User
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string? FullName { get; set; }

    public UserType Type { get; set; } = UserType.Normal;
}
