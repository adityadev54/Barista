using System.Text.Json;
using Barista.Data;
using Barista.Domain;
using Microsoft.Data.Sqlite;

namespace Barista.Services;

// Handles menu availability, ticket placement, and pickup status in SQLite.
public sealed record OrderTicket(
    int Number,
    string Item,
    OrderReceipt Receipt,
    bool Ready = false,
    bool Completed = false)
{
    public string Status => Completed ? "Completed" : Ready ? "Ready" : "Preparing";
}

public sealed class OrderWorkspace(BaristaDatabase database)
{
    public bool IsAvailable(MenuKey key)
    {
        using var connection = database.Open();
        return IsAvailable(connection, key);
    }

    // The table holds hidden items. Removing an entry puts the item back on the menu.
    public void SetAvailability(MenuKey key, bool available)
    {
        using var connection = database.Open();
        using var command = connection.CreateCommand();
        command.CommandText = available
            ? "DELETE FROM menu_availability WHERE item_key = $key;"
            : "INSERT OR IGNORE INTO menu_availability (item_key) VALUES ($key);";
        command.Parameters.AddWithValue("$key", key.ToString());
        command.ExecuteNonQuery();
    }

    public OrderTicket Place(
        Guid requestId,
        MenuKey key,
        string item,
        OrderReceipt receipt,
        long? clientId = null)
    {
        using var connection = database.Open();
        // Keep the retry check, availability check, and insert in one write transaction.
        using var transaction = connection.BeginTransaction(deferred: false);

        // A repeated submission returns its existing ticket instead of placing it twice.
        if (FindTicket(connection, transaction, requestId) is { } existing)
        {
            transaction.Commit();
            return existing;
        }

        if (!IsAvailable(connection, key, transaction))
            throw new OrderValidationException("This item is currently unavailable.");

        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO order_ticket (request_id, item, receipt, client_id, created_at)
            VALUES ($request, $item, $receipt, $client, $created)
            RETURNING number;
            """;
        command.Parameters.AddWithValue("$request", requestId.ToString());
        command.Parameters.AddWithValue("$item", item);
        command.Parameters.AddWithValue("$receipt", JsonSerializer.Serialize(receipt));
        command.Parameters.AddWithValue("$client", (object?)clientId ?? DBNull.Value);
        command.Parameters.AddWithValue("$created", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        var number = checked((int)(long)command.ExecuteScalar()!);
        transaction.Commit();
        return new(number, item, receipt);
    }

    // The client supplies an account ID. Staff can ask for active tickets or a date range.
    public IReadOnlyList<OrderTicket> GetTickets(
        long? clientId = null,
        bool activeOnly = false,
        DateTimeOffset? since = null,
        DateTimeOffset? until = null)
    {
        using var connection = database.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT number, item, receipt, ready, completed
            FROM order_ticket
            WHERE ($client IS NULL OR client_id = $client)
              AND ($active = 0 OR completed = 0)
              AND ($since IS NULL OR created_at >= $since)
              AND ($until IS NULL OR created_at < $until)
            ORDER BY number DESC;
            """;
        command.Parameters.AddWithValue("$client", (object?)clientId ?? DBNull.Value);
        command.Parameters.AddWithValue("$active", activeOnly);
        command.Parameters.AddWithValue("$since", (object?)since?.ToUnixTimeSeconds() ?? DBNull.Value);
        command.Parameters.AddWithValue("$until", (object?)until?.ToUnixTimeSeconds() ?? DBNull.Value);
        using var rows = command.ExecuteReader();
        var tickets = new List<OrderTicket>();
        while (rows.Read())
            tickets.Add(ReadTicket(rows));

        return tickets;
    }

    public void MarkReady(int number) => UpdateStatus(number,
        "UPDATE order_ticket SET ready = 1 WHERE number = $number;");

    // The database only allows handoff after staff marks the order ready.
    public void Complete(int number) => UpdateStatus(number,
        "UPDATE order_ticket SET completed = 1 WHERE number = $number AND ready = 1;");

    private void UpdateStatus(int number, string sql)
    {
        using var connection = database.Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$number", number);
        command.ExecuteNonQuery();
    }

    private static bool IsAvailable(
        SqliteConnection connection,
        MenuKey key,
        SqliteTransaction? transaction = null)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT NOT EXISTS (SELECT 1 FROM menu_availability WHERE item_key = $key);
            """;
        command.Parameters.AddWithValue("$key", key.ToString());
        return (long)command.ExecuteScalar()! != 0;
    }

    private static OrderTicket? FindTicket(
        SqliteConnection connection,
        SqliteTransaction transaction,
        Guid requestId)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT number, item, receipt, ready, completed FROM order_ticket WHERE request_id = $request;
            """;
        command.Parameters.AddWithValue("$request", requestId.ToString());
        using var row = command.ExecuteReader();
        return row.Read() ? ReadTicket(row) : null;
    }

    private static OrderTicket ReadTicket(SqliteDataReader row) => new(
        row.GetInt32(0),
        row.GetString(1),
        JsonSerializer.Deserialize<OrderReceipt>(row.GetString(2))!,
        row.GetBoolean(3),
        row.GetBoolean(4));
}
