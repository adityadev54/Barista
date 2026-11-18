using Microsoft.AspNetCore.Identity;

namespace Barista.Data;

// Sets up and verifies the single staff account stored in SQLite.
public sealed record StaffAccount(string Username);

public sealed class StaffAccounts(BaristaDatabase database)
{
    public const string Scheme = "Staff";

    private readonly PasswordHasher<StaffAccount> hasher = new();

    public bool Exists()
    {
        using var connection = database.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT EXISTS (SELECT 1 FROM staff_account);";
        return (long)command.ExecuteScalar()! != 0;
    }

    // Only the first setup request can claim the staff account row.
    public bool TryCreate(StaffAccount account, string password)
    {
        var hash = hasher.HashPassword(account, password);
        using var connection = database.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO staff_account (id, username, password_hash)
            VALUES (1, $username, $hash)
            ON CONFLICT (id) DO NOTHING;
            """;
        command.Parameters.AddWithValue("$username", account.Username);
        command.Parameters.AddWithValue("$hash", hash);
        return command.ExecuteNonQuery() == 1;
    }

    // Compare the submitted username and verify the password against its stored hash.
    public StaffAccount? Authenticate(string username, string password)
    {
        using var connection = database.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT username, password_hash FROM staff_account WHERE id = 1;";
        using var row = command.ExecuteReader();
        if (!row.Read())
            return null;

        var account = new StaffAccount(row.GetString(0));
        var result = hasher.VerifyHashedPassword(account, row.GetString(1), password);
        return result != PasswordVerificationResult.Failed &&
            string.Equals(account.Username, username, StringComparison.OrdinalIgnoreCase)
            ? account
            : null;
    }
}
