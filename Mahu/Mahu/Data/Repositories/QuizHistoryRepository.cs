using Mahu.Data.Models;
using Microsoft.Data.Sqlite;

namespace Mahu.Data.Repositories;

/// <summary>
/// Repository cho bảng QuizHistories (lịch sử làm Quiz).
/// </summary>
public static class QuizHistoryRepository
{
    // ----------------------------------------------------------------
    // READ
    // ----------------------------------------------------------------

    /// <summary>Lấy N lượt quiz gần nhất.</summary>
    public static List<QuizHistory> GetRecent(int count)
    {
        using var conn = AppDbContext.CreateConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM QuizHistories ORDER BY CreatedAt DESC LIMIT $count";
        cmd.Parameters.AddWithValue("$count", count);
        using var reader = cmd.ExecuteReader();
        var list = new List<QuizHistory>();
        while (reader.Read()) list.Add(MapReader(reader));
        return list;
    }

    /// <summary>Lấy lịch sử quiz của một packet cụ thể.</summary>
    public static List<QuizHistory> GetByPacket(string packetId)
    {
        using var conn = AppDbContext.CreateConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM QuizHistories WHERE PacketId = $packetId ORDER BY CreatedAt DESC";
        cmd.Parameters.AddWithValue("$packetId", packetId);
        using var reader = cmd.ExecuteReader();
        var list = new List<QuizHistory>();
        while (reader.Read()) list.Add(MapReader(reader));
        return list;
    }

    /// <summary>Lấy điểm cao nhất từng đạt được với một packet.</summary>
    public static int GetBestScore(string packetId)
    {
        using var conn = AppDbContext.CreateConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COALESCE(MAX(Score), 0) FROM QuizHistories WHERE PacketId = $packetId";
        cmd.Parameters.AddWithValue("$packetId", packetId);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    /// <summary>Điểm trung bình toàn bộ các lượt quiz đã làm.</summary>
    public static double GetAverageScore()
    {
        using var conn = AppDbContext.CreateConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COALESCE(AVG(Score), 0) FROM QuizHistories";
        return Convert.ToDouble(cmd.ExecuteScalar());
    }

    /// <summary>
    /// Thống kê quiz theo scope: tổng lượt, điểm TB, số câu đúng/sai.
    /// Scope: "Packet" | "SpacedRepetition" | "All"
    /// </summary>
    public static QuizScopeStat GetStatsByScope(string scope)
    {
        using var conn = AppDbContext.CreateConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT
                COUNT(*)            AS TotalPlayed,
                COALESCE(AVG(Score), 0)        AS AvgScore,
                COALESCE(SUM(CorrectAnswers),0) AS TotalCorrect,
                COALESCE(SUM(WrongAnswers),0)   AS TotalWrong
            FROM QuizHistories
            WHERE QuizScope = $scope
            """;
        cmd.Parameters.AddWithValue("$scope", scope);
        using var reader = cmd.ExecuteReader();
        if (reader.Read())
            return new QuizScopeStat(
                Scope:        scope,
                TotalPlayed:  reader.GetInt32(0),
                AvgScore:     reader.GetDouble(1),
                TotalCorrect: reader.GetInt32(2),
                TotalWrong:   reader.GetInt32(3));
        return new QuizScopeStat(scope, 0, 0, 0, 0);
    }

    /// <summary>Tổng số lượt quiz đã làm.</summary>
    public static int GetTotalCount()
    {
        using var conn = AppDbContext.CreateConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM QuizHistories";
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    // ----------------------------------------------------------------
    // WRITE
    // ----------------------------------------------------------------

    /// <summary>Lưu kết quả một lượt quiz. Id được tự sinh GUID nếu rỗng.</summary>
    public static void Insert(QuizHistory history)
    {
        if (string.IsNullOrEmpty(history.Id))
            history.Id = Guid.NewGuid().ToString();

        using var conn = AppDbContext.CreateConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO QuizHistories
                (Id, PacketId, QuizScope, QuizType, TotalQuestions, CorrectAnswers, WrongAnswers, Score, Duration, XPEarned)
            VALUES
                ($Id, $PacketId, $QuizScope, $QuizType, $TotalQuestions, $CorrectAnswers, $WrongAnswers, $Score, $Duration, $XPEarned)
            """;
        cmd.Parameters.AddWithValue("$Id",             history.Id);
        cmd.Parameters.AddWithValue("$PacketId",       (object?)history.PacketId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$QuizScope",      history.QuizScope);
        cmd.Parameters.AddWithValue("$QuizType",       (int)history.QuizType);
        cmd.Parameters.AddWithValue("$TotalQuestions", history.TotalQuestions);
        cmd.Parameters.AddWithValue("$CorrectAnswers", history.CorrectAnswers);
        cmd.Parameters.AddWithValue("$WrongAnswers",   history.WrongAnswers);
        cmd.Parameters.AddWithValue("$Score",          history.Score);
        cmd.Parameters.AddWithValue("$Duration",       history.Duration);
        cmd.Parameters.AddWithValue("$XPEarned",       history.XPEarned);
        cmd.ExecuteNonQuery();
    }

    // ----------------------------------------------------------------
    // PRIVATE
    // ----------------------------------------------------------------

    private static QuizHistory MapReader(SqliteDataReader r) => new()
    {
        Id             = r.GetString(r.GetOrdinal("Id")),
        PacketId       = r.IsDBNull(r.GetOrdinal("PacketId")) ? null : r.GetString(r.GetOrdinal("PacketId")),
        QuizScope      = r.GetString(r.GetOrdinal("QuizScope")),
        QuizType       = (QuizType)r.GetInt32(r.GetOrdinal("QuizType")),
        TotalQuestions = r.GetInt32(r.GetOrdinal("TotalQuestions")),
        CorrectAnswers = r.GetInt32(r.GetOrdinal("CorrectAnswers")),
        WrongAnswers   = r.GetInt32(r.GetOrdinal("WrongAnswers")),
        Score          = r.GetInt32(r.GetOrdinal("Score")),
        Duration       = r.GetInt32(r.GetOrdinal("Duration")),
        XPEarned       = r.GetInt32(r.GetOrdinal("XPEarned")),
        CreatedAt      = r.GetString(r.GetOrdinal("CreatedAt")),
    };
}

/// <summary>Thống kê quiz theo scope.</summary>
public record QuizScopeStat(
    string Scope,
    int    TotalPlayed,
    double AvgScore,
    int    TotalCorrect,
    int    TotalWrong
);
