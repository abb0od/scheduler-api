using Jose; // jose-jwt
using SchedulerAPI.Models;
using System.Security.Cryptography;
 
namespace SchedulerAPI.Services;

public class AuthService
{
    private readonly RSA _privateKey;
    private readonly RSA _publicKey;

    public AuthService()
    {
        // Generate RSA keys (for demo; in production, store securely)
        RSA rsa = RSA.Create(2048);
        _privateKey = rsa;
        _publicKey = rsa;
    }

    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool VerifyPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }

    public string GenerateJweToken(User user)
    {
        // Create a list of scopes based on user data
        var scopes = new List<string>
        {
            $"user.id:{user.Id}",
            $"user.email:{user.Email}",
            $"user.type:{user.Type}",
        };

        if (!string.IsNullOrEmpty(user.FullName))
        {
            scopes.Add($"user.fullname:{user.FullName}");
        }

        var payload = new Dictionary<string, object>
        {
            { "sub", user.Id },
            { "email", user.Email },
            { "scope", string.Join(" ", scopes) },
            { "exp", DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds() }
        };

        return JWT.Encode(payload, _privateKey, JweAlgorithm.RSA_OAEP_256, JweEncryption.A256GCM);
    }

    public string? ValidateToken(string token)
    {
        try
        {
            // Using private key for decryption (RSA decryption requires private key)
            var payload = JWT.Decode<Dictionary<string, object>>(
                token, 
                _privateKey, 
                JweAlgorithm.RSA_OAEP_256, 
                JweEncryption.A256GCM
            );

            // Check if token is expired
            if (payload.TryGetValue("exp", out var expValue) &&
                expValue is long exp &&
                DateTimeOffset.FromUnixTimeSeconds(exp) < DateTimeOffset.UtcNow)
            {
                return null;
            }

            // Return the user ID from the subject claim
            return payload.TryGetValue("sub", out var subValue) ? subValue?.ToString() : null;
        }
        catch
        {
            return null;
        }
    }
       

    public IDictionary<string, object> DecryptJweToken(string token)
    {
        return JWT.Decode<IDictionary<string, object>>(token, _privateKey, JweAlgorithm.RSA_OAEP_256, JweEncryption.A256GCM);
    }
}
 