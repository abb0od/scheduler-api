using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using SchedulerAPI.Enums;

namespace SchedulerAPI.Models;


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
