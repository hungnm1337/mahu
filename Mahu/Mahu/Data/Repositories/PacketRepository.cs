using Mahu.Data.Models;
using Microsoft.Data.Sqlite;

namespace Mahu.Data.Repositories;

/// <summary>
/// Repository cho bảng Packets (gói từ vựng).
/// </summary>
public static class PacketRepository
{
    // ----------------------------------------------------------------
    // READ
    // ----------------------------------------------------------------

    /// <summary>Lấy toàn bộ packets, sắp xếp mới nhất lên trên.</summary>
    public static List<Packet> GetAll()
    {
        using var conn = AppDbContext.CreateConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Packets ORDER BY CreatedAt DESC";
        using var reader = cmd.ExecuteReader();
        var list = new List<Packet>();
        while (reader.Read()) list.Add(MapReader(reader));
        return list;
    }

    /// <summary>Lấy tất cả packets thuộc một danh mục.</summary>
    public static List<Packet> GetByCategory(string categoryId)
    {
        using var conn = AppDbContext.CreateConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Packets WHERE CategoryId = $categoryId ORDER BY CreatedAt DESC";
        cmd.Parameters.AddWithValue("$categoryId", categoryId);
        using var reader = cmd.ExecuteReader();
        var list = new List<Packet>();
        while (reader.Read()) list.Add(MapReader(reader));
        return list;
    }

    /// <summary>Lấy một packet theo Id.</summary>
    public static Packet? GetById(string id)
    {
        using var conn = AppDbContext.CreateConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Packets WHERE Id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        using var reader = cmd.ExecuteReader();
        return reader.Read() ? MapReader(reader) : null;
    }

    /// <summary>Tìm một packet theo Tên (không phân biệt hoa thường).</summary>
    public static Packet? FindByName(string name, string? excludeId = null)
    {
        using var conn = AppDbContext.CreateConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        
        if (excludeId == null)
        {
            cmd.CommandText = "SELECT * FROM Packets WHERE Name = $name COLLATE NOCASE";
            cmd.Parameters.AddWithValue("$name", name);
        }
        else
        {
            cmd.CommandText = "SELECT * FROM Packets WHERE Name = $name COLLATE NOCASE AND Id != $excludeId";
            cmd.Parameters.AddWithValue("$name", name);
            cmd.Parameters.AddWithValue("$excludeId", excludeId);
        }

        using var reader = cmd.ExecuteReader();
        return reader.Read() ? MapReader(reader) : null;
    }

    /// <summary>Lấy danh sách packet được đánh dấu yêu thích.</summary>
    public static List<Packet> GetFavorites()
    {
        using var conn = AppDbContext.CreateConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Packets WHERE IsFavorite = 1 ORDER BY UpdatedAt DESC";
        using var reader = cmd.ExecuteReader();
        var list = new List<Packet>();
        while (reader.Read()) list.Add(MapReader(reader));
        return list;
    }

    /// <summary>Lấy danh sách packet được ghim lên Dashboard.</summary>
    public static List<Packet> GetPinned()
    {
        using var conn = AppDbContext.CreateConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Packets WHERE IsPinned = 1 ORDER BY UpdatedAt DESC";
        using var reader = cmd.ExecuteReader();
        var list = new List<Packet>();
        while (reader.Read()) list.Add(MapReader(reader));
        return list;
    }

    /// <summary>Đếm tổng số từ vựng trong một packet.</summary>
    public static int GetWordCount(string packetId)
    {
        using var conn = AppDbContext.CreateConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM Vocabularies WHERE PacketId = $packetId";
        cmd.Parameters.AddWithValue("$packetId", packetId);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    /// <summary>
    /// Trả về số lượng từ theo từng trạng thái học trong một packet.
    /// Từ chưa có WordProgress được tính là New (0).
    /// </summary>
    public static Dictionary<WordProgressState, int> GetProgressSummary(string packetId)
    {
        using var conn = AppDbContext.CreateConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT COALESCE(wp.State, 0) AS State, COUNT(*) AS Count
            FROM Vocabularies v
            LEFT JOIN WordProgress wp ON v.Id = wp.VocabularyId
            WHERE v.PacketId = $packetId
            GROUP BY COALESCE(wp.State, 0)
            """;
        cmd.Parameters.AddWithValue("$packetId", packetId);

        var result = new Dictionary<WordProgressState, int>
        {
            [WordProgressState.New]      = 0,
            [WordProgressState.Learning] = 0,
            [WordProgressState.Review]   = 0,
            [WordProgressState.Mastered] = 0,
        };

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var state = (WordProgressState)reader.GetInt32(0);
            result[state] = reader.GetInt32(1);
        }
        return result;
    }

    // ----------------------------------------------------------------
    // WRITE
    // ----------------------------------------------------------------

    /// <summary>Thêm packet mới. Id được tự sinh GUID nếu rỗng.</summary>
    public static void Insert(Packet packet)
    {
        if (string.IsNullOrEmpty(packet.Id))
            packet.Id = Guid.NewGuid().ToString();

        using var conn = AppDbContext.CreateConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO Packets (Id, CategoryId, Name, Description, IsFavorite, IsPinned, IsCompleted)
            VALUES ($Id, $CategoryId, $Name, $Description, $IsFavorite, $IsPinned, $IsCompleted)
            """;
        cmd.Parameters.AddWithValue("$Id",          packet.Id);
        cmd.Parameters.AddWithValue("$CategoryId",  packet.CategoryId);
        cmd.Parameters.AddWithValue("$Name",        packet.Name);
        cmd.Parameters.AddWithValue("$Description", (object?)packet.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$IsFavorite",  packet.IsFavorite  ? 1 : 0);
        cmd.Parameters.AddWithValue("$IsPinned",    packet.IsPinned    ? 1 : 0);
        cmd.Parameters.AddWithValue("$IsCompleted", packet.IsCompleted ? 1 : 0);
        cmd.ExecuteNonQuery();
    }

    /// <summary>Cập nhật thông tin packet.</summary>
    public static void Update(Packet packet)
    {
        using var conn = AppDbContext.CreateConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE Packets SET
                CategoryId  = $CategoryId,
                Name        = $Name,
                Description = $Description,
                IsFavorite  = $IsFavorite,
                IsPinned    = $IsPinned,
                IsCompleted = $IsCompleted,
                UpdatedAt   = datetime('now','localtime')
            WHERE Id = $Id
            """;
        cmd.Parameters.AddWithValue("$Id",          packet.Id);
        cmd.Parameters.AddWithValue("$CategoryId",  packet.CategoryId);
        cmd.Parameters.AddWithValue("$Name",        packet.Name);
        cmd.Parameters.AddWithValue("$Description", (object?)packet.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$IsFavorite",  packet.IsFavorite  ? 1 : 0);
        cmd.Parameters.AddWithValue("$IsPinned",    packet.IsPinned    ? 1 : 0);
        cmd.Parameters.AddWithValue("$IsCompleted", packet.IsCompleted ? 1 : 0);
        cmd.ExecuteNonQuery();
    }

    /// <summary>Xóa packet (CASCADE → Vocabularies → WordProgress).</summary>
    public static void Delete(string id)
    {
        using var conn = AppDbContext.CreateConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM Packets WHERE Id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    // ----------------------------------------------------------------
    // QUICK TOGGLES
    // ----------------------------------------------------------------

    /// <summary>Bật/tắt yêu thích.</summary>
    public static void SetFavorite(string id, bool value)
        => RunToggle(id, "IsFavorite", value);

    /// <summary>Bật/tắt ghim Dashboard.</summary>
    public static void SetPinned(string id, bool value)
        => RunToggle(id, "IsPinned", value);

    /// <summary>Đánh dấu đã hoàn thành toàn bộ từ trong packet.</summary>
    public static void SetCompleted(string id, bool value)
        => RunToggle(id, "IsCompleted", value);

    // ----------------------------------------------------------------
    // PRIVATE
    // ----------------------------------------------------------------

    private static void RunToggle(string id, string column, bool value)
    {
        using var conn = AppDbContext.CreateConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"UPDATE Packets SET {column} = $v, UpdatedAt = datetime('now','localtime') WHERE Id = $id";
        cmd.Parameters.AddWithValue("$v",  value ? 1 : 0);
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    private static Packet MapReader(SqliteDataReader r) => new()
    {
        Id          = r.GetString(r.GetOrdinal("Id")),
        CategoryId  = r.GetString(r.GetOrdinal("CategoryId")),
        Name        = r.GetString(r.GetOrdinal("Name")),
        Description = r.IsDBNull(r.GetOrdinal("Description")) ? null : r.GetString(r.GetOrdinal("Description")),
        IsFavorite  = r.GetInt32(r.GetOrdinal("IsFavorite"))  == 1,
        IsPinned    = r.GetInt32(r.GetOrdinal("IsPinned"))    == 1,
        IsCompleted = r.GetInt32(r.GetOrdinal("IsCompleted")) == 1,
        CreatedAt   = r.GetString(r.GetOrdinal("CreatedAt")),
        UpdatedAt   = r.GetString(r.GetOrdinal("UpdatedAt")),
    };
}
