using Mahu.Data.Models;
using Microsoft.Data.Sqlite;

namespace Mahu.Data.Repositories;

/// <summary>
/// Repository cho bảng WordProgress (tiến trình học SM-2).
/// Đây là repository quan trọng nhất — điều khiển thuật toán Spaced Repetition.
/// </summary>
public static class WordProgressRepository
{
    // ----------------------------------------------------------------
    // READ
    // ----------------------------------------------------------------

    /// <summary>Lấy tiến trình học của một từ.</summary>
    public static WordProgress? GetById(string vocabularyId)
    {
        using var conn = AppDbContext.CreateConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM WordProgress WHERE VocabularyId = $id";
        cmd.Parameters.AddWithValue("$id", vocabularyId);
        using var reader = cmd.ExecuteReader();
        return reader.Read() ? MapReader(reader) : null;
    }

    /// <summary>Lấy tiến trình của tất cả từ trong một packet.</summary>
    public static List<WordProgress> GetByPacket(string packetId)
    {
        using var conn = AppDbContext.CreateConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT wp.*
            FROM WordProgress wp
            INNER JOIN Vocabularies v ON wp.VocabularyId = v.Id
            WHERE v.PacketId = $packetId
            """;
        cmd.Parameters.AddWithValue("$packetId", packetId);
        using var reader = cmd.ExecuteReader();
        var list = new List<WordProgress>();
        while (reader.Read()) list.Add(MapReader(reader));
        return list;
    }

    /// <summary>
    /// Lấy tất cả từ cần ôn hôm nay (NextReviewDate &lt;= hôm nay).
    /// Kết quả sắp xếp theo ngày quá hạn lâu nhất trước.
    /// </summary>
    public static List<WordProgress> GetDueToday()
    {
        using var conn = AppDbContext.CreateConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT * FROM WordProgress
            WHERE NextReviewDate <= date('now','localtime')
            ORDER BY NextReviewDate ASC
            """;
        using var reader = cmd.ExecuteReader();
        var list = new List<WordProgress>();
        while (reader.Read()) list.Add(MapReader(reader));
        return list;
    }

    /// <summary>Lấy các từ cần ôn hôm nay trong một packet cụ thể.</summary>
    public static List<WordProgress> GetDueByPacket(string packetId)
    {
        using var conn = AppDbContext.CreateConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT wp.*
            FROM WordProgress wp
            INNER JOIN Vocabularies v ON wp.VocabularyId = v.Id
            WHERE v.PacketId = $packetId
              AND wp.NextReviewDate <= date('now','localtime')
            ORDER BY wp.NextReviewDate ASC
            """;
        cmd.Parameters.AddWithValue("$packetId", packetId);
        using var reader = cmd.ExecuteReader();
        var list = new List<WordProgress>();
        while (reader.Read()) list.Add(MapReader(reader));
        return list;
    }

    /// <summary>Lấy các từ theo trạng thái học (New/Learning/Review/Mastered).</summary>
    public static List<WordProgress> GetByState(WordProgressState state)
    {
        using var conn = AppDbContext.CreateConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM WordProgress WHERE State = $state";
        cmd.Parameters.AddWithValue("$state", (int)state);
        using var reader = cmd.ExecuteReader();
        var list = new List<WordProgress>();
        while (reader.Read()) list.Add(MapReader(reader));
        return list;
    }

    /// <summary>Lấy các từ đã quá hạn ôn tập (NextReviewDate &lt; hôm nay).</summary>
    public static List<WordProgress> GetOverdue()
    {
        using var conn = AppDbContext.CreateConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT * FROM WordProgress
            WHERE NextReviewDate < date('now','localtime')
            ORDER BY NextReviewDate ASC
            """;
        using var reader = cmd.ExecuteReader();
        var list = new List<WordProgress>();
        while (reader.Read()) list.Add(MapReader(reader));
        return list;
    }

    /// <summary>Đếm nhanh số từ cần ôn hôm nay (dùng cho badge/notification).</summary>
    public static int GetDueTodayCount()
    {
        using var conn = AppDbContext.CreateConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM WordProgress WHERE NextReviewDate <= date('now','localtime')";
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    /// <summary>
    /// Thống kê số từ theo từng trạng thái trong một packet.
    /// Từ chưa có WordProgress được tính là New.
    /// </summary>
    public static Dictionary<WordProgressState, int> GetStatsByPacket(string packetId)
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
            result[(WordProgressState)reader.GetInt32(0)] = reader.GetInt32(1);
        return result;
    }

    /// <summary>
    /// Thống kê tổng toàn bộ từ vựng theo trạng thái (dùng cho Dashboard).
    /// </summary>
    public static Dictionary<WordProgressState, int> GetOverallStats()
    {
        using var conn = AppDbContext.CreateConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT State, COUNT(*) AS Count
            FROM WordProgress
            GROUP BY State
            """;

        var result = new Dictionary<WordProgressState, int>
        {
            [WordProgressState.New]      = 0,
            [WordProgressState.Learning] = 0,
            [WordProgressState.Review]   = 0,
            [WordProgressState.Mastered] = 0,
        };
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            result[(WordProgressState)reader.GetInt32(0)] = reader.GetInt32(1);
        return result;
    }

    // ----------------------------------------------------------------
    // WRITE
    // ----------------------------------------------------------------

    /// <summary>Tạo tiến trình mới cho từ khi học lần đầu.</summary>
    public static void Insert(WordProgress progress)
    {
        using var conn = AppDbContext.CreateConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO WordProgress
                (VocabularyId, EaseFactor, Interval, Repetitions, NextReviewDate, LastReviewedDate, State, CorrectCount, WrongCount)
            VALUES
                ($VocabularyId, $EaseFactor, $Interval, $Repetitions, $NextReviewDate, $LastReviewedDate, $State, $CorrectCount, $WrongCount)
            """;
        BindParams(cmd, progress);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Cập nhật tiến trình sau khi ôn tập (EaseFactor, Interval, NextReviewDate, State...).
    /// </summary>
    public static void Update(WordProgress progress)
    {
        using var conn = AppDbContext.CreateConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE WordProgress SET
                EaseFactor       = $EaseFactor,
                Interval         = $Interval,
                Repetitions      = $Repetitions,
                NextReviewDate   = $NextReviewDate,
                LastReviewedDate = $LastReviewedDate,
                State            = $State,
                CorrectCount     = $CorrectCount,
                WrongCount       = $WrongCount
            WHERE VocabularyId = $VocabularyId
            """;
        BindParams(cmd, progress);
        cmd.ExecuteNonQuery();
    }

    // ----------------------------------------------------------------
    // PRIVATE
    // ----------------------------------------------------------------

    private static void BindParams(SqliteCommand cmd, WordProgress p)
    {
        cmd.Parameters.AddWithValue("$VocabularyId",     p.VocabularyId);
        cmd.Parameters.AddWithValue("$EaseFactor",       p.EaseFactor);
        cmd.Parameters.AddWithValue("$Interval",         p.Interval);
        cmd.Parameters.AddWithValue("$Repetitions",      p.Repetitions);
        cmd.Parameters.AddWithValue("$NextReviewDate",   p.NextReviewDate);
        cmd.Parameters.AddWithValue("$LastReviewedDate", (object?)p.LastReviewedDate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$State",            (int)p.State);
        cmd.Parameters.AddWithValue("$CorrectCount",     p.CorrectCount);
        cmd.Parameters.AddWithValue("$WrongCount",       p.WrongCount);
    }

    private static WordProgress MapReader(SqliteDataReader r) => new()
    {
        VocabularyId     = r.GetString(r.GetOrdinal("VocabularyId")),
        EaseFactor       = r.GetDouble(r.GetOrdinal("EaseFactor")),
        Interval         = r.GetInt32(r.GetOrdinal("Interval")),
        Repetitions      = r.GetInt32(r.GetOrdinal("Repetitions")),
        NextReviewDate   = r.GetString(r.GetOrdinal("NextReviewDate")),
        LastReviewedDate = r.IsDBNull(r.GetOrdinal("LastReviewedDate")) ? null : r.GetString(r.GetOrdinal("LastReviewedDate")),
        State            = (WordProgressState)r.GetInt32(r.GetOrdinal("State")),
        CorrectCount     = r.GetInt32(r.GetOrdinal("CorrectCount")),
        WrongCount       = r.GetInt32(r.GetOrdinal("WrongCount")),
    };
}
