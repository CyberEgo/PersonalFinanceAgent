using System.Text.Json;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Checkpointing;
using Microsoft.Data.SqlClient;

namespace PersonalFinance.AgentBackend;

public sealed class SqlCheckpointStore : ICheckpointStore<JsonElement>
{
    private readonly string _connectionString;

    public SqlCheckpointStore(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        _connectionString = connectionString;
    }

    public async ValueTask<CheckpointInfo> CreateCheckpointAsync(
        string sessionId, JsonElement value, CheckpointInfo? parent = null)
    {
        var checkpointId = Guid.NewGuid().ToString("N");
        var data = value.GetRawText();
        var parentId = parent?.CheckpointId;

        const string sql = """
            INSERT INTO Checkpoints (SessionId, CheckpointId, ParentCheckpointId, Data, CreatedAt)
            VALUES (@SessionId, @CheckpointId, @ParentCheckpointId, @Data, @CreatedAt)
            """;

        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@SessionId", sessionId);
        cmd.Parameters.AddWithValue("@CheckpointId", checkpointId);
        cmd.Parameters.AddWithValue("@ParentCheckpointId", (object?)parentId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Data", data);
        cmd.Parameters.AddWithValue("@CreatedAt", DateTimeOffset.UtcNow);
        await cmd.ExecuteNonQueryAsync();

        return new CheckpointInfo(sessionId, checkpointId);
    }

    public async ValueTask<JsonElement> RetrieveCheckpointAsync(
        string sessionId, CheckpointInfo key)
    {
        const string sql = """
            SELECT Data FROM Checkpoints
            WHERE SessionId = @SessionId AND CheckpointId = @CheckpointId
            """;

        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@SessionId", sessionId);
        cmd.Parameters.AddWithValue("@CheckpointId", key.CheckpointId);

        var result = await cmd.ExecuteScalarAsync();
        if (result is not string json)
            return default;

        return JsonDocument.Parse(json).RootElement.Clone();
    }

    public async ValueTask<IEnumerable<CheckpointInfo>> RetrieveIndexAsync(
        string sessionId, CheckpointInfo? withParent = null)
    {
        var hasParentFilter = withParent is not null && !string.IsNullOrEmpty(withParent.CheckpointId);

        var sql = hasParentFilter
            ? "SELECT CheckpointId FROM Checkpoints WHERE SessionId = @SessionId AND ParentCheckpointId = @ParentCheckpointId ORDER BY CreatedAt"
            : "SELECT CheckpointId FROM Checkpoints WHERE SessionId = @SessionId ORDER BY CreatedAt";

        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@SessionId", sessionId);

        if (hasParentFilter)
            cmd.Parameters.AddWithValue("@ParentCheckpointId", withParent!.CheckpointId);

        var results = new List<CheckpointInfo>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(new CheckpointInfo(sessionId, reader.GetString(0)));
        }

        return results;
    }
}
