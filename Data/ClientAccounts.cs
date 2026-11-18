using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

namespace Barista.Data;

// Creates and verifies client accounts. The order uses the account ID from the signed-in session.
public sealed record ClientAccount(long Id, string Username);

public sealed class ClientAccounts(BaristaDatabase database)
{
    public const string Scheme = "Client";
    private readonly PasswordHasher<ClientAccount> hasher = new();

    // Store a case-insensitive username key and a password hash.
    public ClientAccount? Register(string username, string password)
    {
        var account = new ClientAccount(0, username);
        using var connection = database.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO client_account (username, username_key, password_hash)
            VALUES ($name, $key, $hash)
            ON CONFLICT (username_key) DO NOTHING
            RETURNING id;
            """;
        command.Parameters.AddWithValue("$name", username);
        command.Parameters.AddWithValue("$key", username.ToUpperInvariant());
        command.Parameters.AddWithValue("$hash", hasher.HashPassword(account, password));
        return command.ExecuteScalar() is long id ? account with { Id = id } : null;
    }

    // Look up the account and let ASP.NET verify the supplied password.
    public ClientAccount? Authenticate(string username, string password)
    {
        using var connection = database.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, username, password_hash FROM client_account WHERE username_key = $key;
            """;
        command.Parameters.AddWithValue("$key", username.ToUpperInvariant());
        using var row = command.ExecuteReader();
        if (!row.Read())
            return null;

        var account = new ClientAccount(row.GetInt64(0), row.GetString(1));
        var verification = hasher.VerifyHashedPassword(account, row.GetString(2), password);
        return verification != PasswordVerificationResult.Failed ? account : null;
    }

    public static long? GetId(ClaimsPrincipal? principal) =>
        long.TryParse(principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) && id > 0
            ? id
            : null;
}
