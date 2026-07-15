using Mahu.Data.Models;
using Microsoft.Data.Sqlite;

namespace Mahu.Data.Repositories;

// ----------------------------------------------------------------
// Result DTOs
// ----------------------------------------------------------------

/// <summary>Tóm tắt thống kê học tập trong một ngày.</summary>
public record DailySummary(
    string Date,
    int    TotalMinutes,
    int    WordsReviewed,
    int    XPEarned
);

/// <summary>
/// Repository cho bảng StudySessions (lịch sử phiên học).
/// </summary>
public static class StudySessionRepository
{
    // ----------------------------------------------------------------
    // READ
    // ----------------------------------------------------------------

    /// <summary>Lấy N phiên học gần nhất.</summary>
    public static List<StudySession> GetRecent(int count)
    {
        using var conn = AppDbContext.CreateConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM StudySessions ORDER BY CreatedAt DESC LIMIT $count";
        cmd.Parameters.AddWithValue("$count", count);
        using var reader = cmd.ExecuteReader();
        var list = new List<StudySession>();
        while (reader.Read()) list.Add(MapReader(reader));
        return list;
    }

    /// <summary>Lấy tất cả phiên học trong một ngày cụ thể (yyyy-MM-dd).</summary>
    public static List<StudySession> GetByDate(string date)
    {
        using var conn = AppDbContext.CreateConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM StudySessions WHERE SessionDate = $date ORDER BY CreatedAt ASC";
        cmd.Parameters.AddWithValue("$date", date);
        using var reader = cmd.ExecuteReader();
        var list = new List<StudySession>();
        while (reader.Read()) list.Add(MapReader(reader));
        return list;
    }

    /// <summary>Lấy phiên học trong khoảng thời gian (từ ngày → đến ngày, yyyy-MM-dd).</summary>
    public static List<StudySession> GetByDateRange(string from, string to)
    {
        using var conn = AppDbContext.CreateConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT * FROM StudySessions
            WHERE SessionDate BETWEEN $from AND $to
            ORDER BY SessionDate ASC, CreatedAt ASC
            """;
        cmd.Parameters.AddWithValue("$from", from);
        cmd.Parameters.AddWithValue("$to",   to);
        using var reader = cmd.ExecuteReader();
        var list = new List<StudySession>();
        while (reader.Read()) list.Add(MapReader(reader));
        return list;
    }

    /// <summary>
    /// Tổng hợp stats trong ngày hôm nay:
    /// tổng thời gian học (phút), tổng từ ôn, tổng XP nhận được.
    /// </summary>
    public static DailySummary GetTodaySummary()
    {
        using var conn = AppDbContext.CreateConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT
                date('now','localtime')     AS Date,
                COALESCE(SUM(Duration), 0)      AS TotalMinutes,
                COALESCE(SUM(WordsReviewed), 0) AS WordsReviewed,
                COALESCE(SUM(XPEarned), 0)      AS XPEarned
            FROM StudySessions
            WHERE SessionDate = date('now','localtime')
            """;
        using var reader = cmd.ExecuteReader();
        if (reader.Read())
            return new DailySummary(
                reader.GetString(0),
                reader.GetInt32(1),
                reader.GetInt32(2),
                reader.GetInt32(3));
        return new DailySummary(DateTime.Today.ToString("yyyy-MM-dd"), 0, 0, 0);
    }

    /// <summary>
    /// Stats cho 7 ngày gần nhất (bao gồm ngày không có phiên học).
    /// Kết quả sắp xếp từ xa đến gần (dùng cho biểu đồ).
    /// </summary>
    public static List<DailySummary> GetWeeklySummary()
    {
        using var conn = AppDbContext.CreateConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT
                SessionDate,
                COALESCE(SUM(Duration), 0)      AS TotalMinutes,
                COALESCE(SUM(WordsReviewed), 0) AS WordsReviewed,
                COALESCE(SUM(XPEarned), 0)      AS XPEarned
            FROM StudySessions
            WHERE SessionDate >= date('now','localtime','-6 days')
            GROUP BY SessionDate
            ORDER BY SessionDate ASC
            """;
        using var reader = cmd.ExecuteReader();
        var rows = new Dictionary<string, DailySummary>();
        while (reader.Read())
        {
            var date = reader.GetString(0);
            rows[date] = new DailySummary(date, reader.GetInt32(1), reader.GetInt32(2), reader.GetInt32(3));
        }

        // Lấp đầy các ngày không có phiên học bằng 0
        var result = new List<DailySummary>();
        for (int i = 6; i >= 0; i--)
        {
            var date = DateTime.Today.AddDays(-i).ToString("yyyy-MM-dd");
            result.Add(rows.TryGetValue(date, out var s) ? s : new DailySummary(date, 0, 0, 0));
        }
        return result;
    }

    /// <summary>
    /// Lấy danh sách ngày có học trong N ngày gần nhất.
    /// Dùng để tính streak ở tầng service/logic.
    /// </summary>
    public static List<string> GetActiveDays(int lastNDays)
    {
        using var conn = AppDbContext.CreateConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT DISTINCT SessionDate FROM StudySessions
            WHERE SessionDate >= date('now','localtime',$offset)
            ORDER BY SessionDate DESC
            """;
        cmd.Parameters.AddWithValue("$offset", $"-{lastNDays - 1} days");
        using var reader = cmd.ExecuteReader();
        var list = new List<string>();
        while (reader.Read()) list.Add(reader.GetString(0));
        return list;
    }

    /// <summary>
    /// Tính chuỗi học dài nhất từ trước đến nay.
    /// Tính bằng C# vì chuỗi ngày liên tiếp khó compute thuần SQL.
    /// </summary>
    public static int GetLongestStreak()
    {
        using var conn = AppDbContext.CreateConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT DISTINCT SessionDate FROM StudySessions ORDER BY SessionDate ASC";
        using var reader = cmd.ExecuteReader();
        var dates = new List<DateTime>();
        while (reader.Read())
        {
            if (DateTime.TryParse(reader.GetString(0), out var d))
                dates.Add(d.Date);
        }

        if (dates.Count == 0) return 0;

        int longest = 1, current = 1;
        for (int i = 1; i < dates.Count; i++)
        {
            current = (dates[i] - dates[i - 1]).Days == 1 ? current + 1 : 1;
            if (current > longest) longest = current;
        }
        return longest;
    }

    // ----------------------------------------------------------------
    // WRITE
    // ----------------------------------------------------------------

    /// <summary>Lưu phiên học mới. Id được tự sinh GUID nếu rỗng.</summary>
    public static void Insert(StudySession session)
    {
        if (string.IsNullOrEmpty(session.Id))
            session.Id = Guid.NewGuid().ToString();

        using var conn = AppDbContext.CreateConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO StudySessions
                (Id, SessionDate, Duration, WordsReviewed, NewWordCount, ReviewWordCount, XPEarned, Note)
            VALUES
                ($Id, $SessionDate, $Duration, $WordsReviewed, $NewWordCount, $ReviewWordCount, $XPEarned, $Note)
            """;
        cmd.Parameters.AddWithValue("$Id",              session.Id);
        cmd.Parameters.AddWithValue("$SessionDate",     session.SessionDate);
        cmd.Parameters.AddWithValue("$Duration",        session.Duration);
        cmd.Parameters.AddWithValue("$WordsReviewed",   session.WordsReviewed);
        cmd.Parameters.AddWithValue("$NewWordCount",    session.NewWordCount);
        cmd.Parameters.AddWithValue("$ReviewWordCount", session.ReviewWordCount);
        cmd.Parameters.AddWithValue("$XPEarned",        session.XPEarned);
        cmd.Parameters.AddWithValue("$Note",            (object?)session.Note ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    // ----------------------------------------------------------------
    // PRIVATE
    // ----------------------------------------------------------------

    private static StudySession MapReader(SqliteDataReader r) => new()
    {
        Id              = r.GetString(r.GetOrdinal("Id")),
        SessionDate     = r.GetString(r.GetOrdinal("SessionDate")),
        Duration        = r.GetInt32(r.GetOrdinal("Duration")),
        WordsReviewed   = r.GetInt32(r.GetOrdinal("WordsReviewed")),
        NewWordCount    = r.GetInt32(r.GetOrdinal("NewWordCount")),
        ReviewWordCount = r.GetInt32(r.GetOrdinal("ReviewWordCount")),
        XPEarned        = r.GetInt32(r.GetOrdinal("XPEarned")),
        Note            = r.IsDBNull(r.GetOrdinal("Note")) ? null : r.GetString(r.GetOrdinal("Note")),
        CreatedAt       = r.GetString(r.GetOrdinal("CreatedAt")),
    };
}
