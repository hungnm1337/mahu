using Mahu.Data.Models;
using Microsoft.Data.Sqlite;

namespace Mahu.Data.Repositories;

/// <summary>
/// Repository cho bảng Achievements (thành tựu).
/// </summary>
public static class AchievementRepository
{
    // ----------------------------------------------------------------
    // READ
    // ----------------------------------------------------------------

    /// <summary>Lấy tất cả thành tựu.</summary>
    public static List<Achievement> GetAll()
    {
        using var conn = AppDbContext.CreateConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Achievements ORDER BY IsUnlocked DESC, RewardXP DESC";
        using var reader = cmd.ExecuteReader();
        var list = new List<Achievement>();
        while (reader.Read()) list.Add(MapReader(reader));
        return list;
    }

    /// <summary>Chỉ lấy các thành tựu đã mở khóa, mới nhất trước.</summary>
    public static List<Achievement> GetUnlocked()
    {
        using var conn = AppDbContext.CreateConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Achievements WHERE IsUnlocked = 1 ORDER BY UnlockedAt DESC";
        using var reader = cmd.ExecuteReader();
        var list = new List<Achievement>();
        while (reader.Read()) list.Add(MapReader(reader));
        return list;
    }

    /// <summary>Chỉ lấy các thành tựu chưa mở khóa.</summary>
    public static List<Achievement> GetLocked()
    {
        using var conn = AppDbContext.CreateConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Achievements WHERE IsUnlocked = 0 ORDER BY RewardXP ASC";
        using var reader = cmd.ExecuteReader();
        var list = new List<Achievement>();
        while (reader.Read()) list.Add(MapReader(reader));
        return list;
    }

    /// <summary>Lấy một thành tựu theo Code (dùng trong code để kiểm tra/mở khóa).</summary>
    public static Achievement? GetByCode(string code)
    {
        using var conn = AppDbContext.CreateConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Achievements WHERE Code = $code";
        cmd.Parameters.AddWithValue("$code", code);
        using var reader = cmd.ExecuteReader();
        return reader.Read() ? MapReader(reader) : null;
    }

    /// <summary>Kiểm tra nhanh một thành tựu đã mở khóa chưa.</summary>
    public static bool IsUnlocked(string code)
    {
        using var conn = AppDbContext.CreateConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT IsUnlocked FROM Achievements WHERE Code = $code";
        cmd.Parameters.AddWithValue("$code", code);
        var result = cmd.ExecuteScalar();
        return result != null && Convert.ToInt32(result) == 1;
    }

    // ----------------------------------------------------------------
    // WRITE
    // ----------------------------------------------------------------

    /// <summary>
    /// Seed thành tựu vào DB (dùng khi khởi tạo app).
    /// Dùng INSERT OR IGNORE để không lỗi khi gọi lại.
    /// Id được tự sinh GUID nếu rỗng.
    /// </summary>
    public static void Insert(Achievement achievement)
    {
        if (string.IsNullOrEmpty(achievement.Id))
            achievement.Id = Guid.NewGuid().ToString();

        using var conn = AppDbContext.CreateConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT OR IGNORE INTO Achievements (Id, Code, Name, Description, Icon, RewardXP, IsUnlocked, UnlockedAt)
            VALUES ($Id, $Code, $Name, $Description, $Icon, $RewardXP, $IsUnlocked, $UnlockedAt)
            """;
        cmd.Parameters.AddWithValue("$Id",          achievement.Id);
        cmd.Parameters.AddWithValue("$Code",        achievement.Code);
        cmd.Parameters.AddWithValue("$Name",        achievement.Name);
        cmd.Parameters.AddWithValue("$Description", (object?)achievement.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$Icon",        (object?)achievement.Icon        ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$RewardXP",    achievement.RewardXP);
        cmd.Parameters.AddWithValue("$IsUnlocked",  achievement.IsUnlocked ? 1 : 0);
        cmd.Parameters.AddWithValue("$UnlockedAt",  (object?)achievement.UnlockedAt  ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Mở khóa một thành tựu theo Id và ghi thời điểm mở khóa.
    /// Không làm gì nếu đã mở khóa rồi.
    /// </summary>
    public static void Unlock(string id)
    {
        using var conn = AppDbContext.CreateConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE Achievements
            SET IsUnlocked = 1,
                UnlockedAt = datetime('now','localtime')
            WHERE Id = $id AND IsUnlocked = 0
            """;
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    // ----------------------------------------------------------------
    // PRIVATE
    // ----------------------------------------------------------------

    private static Achievement MapReader(SqliteDataReader r) => new()
    {
        Id          = r.GetString(r.GetOrdinal("Id")),
        Code        = r.GetString(r.GetOrdinal("Code")),
        Name        = r.GetString(r.GetOrdinal("Name")),
        Description = r.IsDBNull(r.GetOrdinal("Description")) ? null : r.GetString(r.GetOrdinal("Description")),
        Icon        = r.IsDBNull(r.GetOrdinal("Icon"))        ? null : r.GetString(r.GetOrdinal("Icon")),
        RewardXP    = r.GetInt32(r.GetOrdinal("RewardXP")),
        IsUnlocked  = r.GetInt32(r.GetOrdinal("IsUnlocked"))  == 1,
        UnlockedAt  = r.IsDBNull(r.GetOrdinal("UnlockedAt"))  ? null : r.GetString(r.GetOrdinal("UnlockedAt")),
    };
}
