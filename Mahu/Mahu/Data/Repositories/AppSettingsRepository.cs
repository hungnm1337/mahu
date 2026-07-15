using Mahu.Data.Models;
using Microsoft.Data.Sqlite;

namespace Mahu.Data.Repositories;

/// <summary>
/// Repository cho bảng AppSettings (singleton — Id = 1).
/// </summary>
public static class AppSettingsRepository
{
    // ----------------------------------------------------------------
    // READ
    // ----------------------------------------------------------------

    /// <summary>Lấy bản ghi cài đặt duy nhất của ứng dụng.</summary>
    public static AppSettings? Get()
    {
        using var conn = AppDbContext.CreateConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM AppSettings WHERE Id = 1";
        using var reader = cmd.ExecuteReader();
        return reader.Read() ? MapReader(reader) : null;
    }

    /// <summary>Kiểm tra xem người dùng đã thiết lập tên chưa (nếu tên = "User" là người dùng mới).</summary>
    public static bool IsNewUser()
    {
        using var conn = AppDbContext.CreateConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT DisplayName FROM AppSettings WHERE Id = 1";
        var name = cmd.ExecuteScalar()?.ToString();
        return string.IsNullOrEmpty(name) || name == "User";
    }

    /// <summary>Lưu lại tên hiển thị. Trả về true nếu thành công, false nếu lỗi.</summary>
    public static bool SaveDisplayName(string newName)
    {
        try
        {
            using var conn = AppDbContext.CreateConnection();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE AppSettings SET DisplayName = $newName, UpdatedAt = datetime('now','localtime') WHERE Id = 1";
            cmd.Parameters.AddWithValue("$newName", newName);
            return cmd.ExecuteNonQuery() > 0;
        }
        catch
        {
            return false;
        }
    }

    // ----------------------------------------------------------------
    // UPDATE — FULL
    // ----------------------------------------------------------------

    /// <summary>Cập nhật toàn bộ thông tin settings.</summary>
    public static void Update(AppSettings s)
    {
        using var conn = AppDbContext.CreateConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE AppSettings SET
                DisplayName              = $DisplayName,
                Avatar                   = $Avatar,
                TotalXP                  = $TotalXP,
                Level                    = $Level,
                CurrentStreak            = $CurrentStreak,
                LastActiveDate           = $LastActiveDate,
                TotalStudyTime           = $TotalStudyTime,
                TotalWordsLearned        = $TotalWordsLearned,
                TotalQuizPlayed          = $TotalQuizPlayed,
                TotalCorrectAnswers      = $TotalCorrectAnswers,
                TotalWrongAnswers        = $TotalWrongAnswers,
                DailyWordGoal            = $DailyWordGoal,
                DailyStudyTimeGoal       = $DailyStudyTimeGoal,
                Theme                    = $Theme,
                Language                 = $Language,
                AutoPlayPronunciation    = $AutoPlayPronunciation,
                SpeechRate               = $SpeechRate,
                SpeechVolume             = $SpeechVolume,
                SpeechVoice              = $SpeechVoice,
                DefaultQuizQuestionCount = $DefaultQuizQuestionCount,
                ShuffleQuestions         = $ShuffleQuestions,
                ShuffleAnswers           = $ShuffleAnswers,
                AutoBackup               = $AutoBackup,
                BackupFolder             = $BackupFolder,
                EnableReminder           = $EnableReminder,
                ReminderTime             = $ReminderTime,
                UpdatedAt                = datetime('now','localtime')
            WHERE Id = 1
            """;

        cmd.Parameters.AddWithValue("$DisplayName",              s.DisplayName);
        cmd.Parameters.AddWithValue("$Avatar",                   (object?)s.Avatar ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$TotalXP",                  s.TotalXP);
        cmd.Parameters.AddWithValue("$Level",                    s.Level);
        cmd.Parameters.AddWithValue("$CurrentStreak",            s.CurrentStreak);
        cmd.Parameters.AddWithValue("$LastActiveDate",           (object?)s.LastActiveDate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$TotalStudyTime",           s.TotalStudyTime);
        cmd.Parameters.AddWithValue("$TotalWordsLearned",        s.TotalWordsLearned);
        cmd.Parameters.AddWithValue("$TotalQuizPlayed",          s.TotalQuizPlayed);
        cmd.Parameters.AddWithValue("$TotalCorrectAnswers",      s.TotalCorrectAnswers);
        cmd.Parameters.AddWithValue("$TotalWrongAnswers",        s.TotalWrongAnswers);
        cmd.Parameters.AddWithValue("$DailyWordGoal",            s.DailyWordGoal);
        cmd.Parameters.AddWithValue("$DailyStudyTimeGoal",       s.DailyStudyTimeGoal);
        cmd.Parameters.AddWithValue("$Theme",                    s.Theme);
        cmd.Parameters.AddWithValue("$Language",                 s.Language);
        cmd.Parameters.AddWithValue("$AutoPlayPronunciation",    s.AutoPlayPronunciation ? 1 : 0);
        cmd.Parameters.AddWithValue("$SpeechRate",               s.SpeechRate);
        cmd.Parameters.AddWithValue("$SpeechVolume",             s.SpeechVolume);
        cmd.Parameters.AddWithValue("$SpeechVoice",              (object?)s.SpeechVoice ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$DefaultQuizQuestionCount", s.DefaultQuizQuestionCount);
        cmd.Parameters.AddWithValue("$ShuffleQuestions",         s.ShuffleQuestions ? 1 : 0);
        cmd.Parameters.AddWithValue("$ShuffleAnswers",           s.ShuffleAnswers ? 1 : 0);
        cmd.Parameters.AddWithValue("$AutoBackup",               s.AutoBackup ? 1 : 0);
        cmd.Parameters.AddWithValue("$BackupFolder",             (object?)s.BackupFolder ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$EnableReminder",           s.EnableReminder ? 1 : 0);
        cmd.Parameters.AddWithValue("$ReminderTime",             (object?)s.ReminderTime ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    // ----------------------------------------------------------------
    // UPDATE — QUICK SETTERS
    // ----------------------------------------------------------------

    /// <summary>Cập nhật nhanh Theme: "Light" | "Dark" | "System".</summary>
    public static void UpdateTheme(string theme)
        => RunSimple(
            "UPDATE AppSettings SET Theme = $v, UpdatedAt = datetime('now','localtime') WHERE Id = 1",
            ("$v", theme));

    /// <summary>Cập nhật nhanh Language: "vi-VN" | "en-US".</summary>
    public static void UpdateLanguage(string lang)
        => RunSimple(
            "UPDATE AppSettings SET Language = $v, UpdatedAt = datetime('now','localtime') WHERE Id = 1",
            ("$v", lang));

    // ----------------------------------------------------------------
    // UPDATE — LEARNING STATS
    // ----------------------------------------------------------------

    /// <summary>
    /// Cộng XP và tự động tính lại Level.
    /// Level = MAX(1,  1 + floor(sqrt(TotalXP / 100)))
    /// </summary>
    public static void AddXP(int amount)
    {
        using var conn = AppDbContext.CreateConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE AppSettings
            SET TotalXP   = TotalXP + $amount,
                Level     = MAX(1, 1 + CAST(SQRT((TotalXP + $amount) / 100.0) AS INTEGER)),
                UpdatedAt = datetime('now','localtime')
            WHERE Id = 1
            """;
        cmd.Parameters.AddWithValue("$amount", amount);
        cmd.ExecuteNonQuery();
    }

    /// <summary>Tăng chuỗi học liên tiếp lên 1 ngày.</summary>
    public static void IncrementStreak()
        => RunSimple(
            "UPDATE AppSettings SET CurrentStreak = CurrentStreak + 1, UpdatedAt = datetime('now','localtime') WHERE Id = 1");

    /// <summary>Reset chuỗi học về 0 (bỏ lỡ ngày học).</summary>
    public static void ResetStreak()
        => RunSimple(
            "UPDATE AppSettings SET CurrentStreak = 0, UpdatedAt = datetime('now','localtime') WHERE Id = 1");

    /// <summary>Cập nhật ngày học gần nhất = hôm nay.</summary>
    public static void UpdateLastActiveDate()
        => RunSimple(
            "UPDATE AppSettings SET LastActiveDate = date('now','localtime'), UpdatedAt = datetime('now','localtime') WHERE Id = 1");

    /// <summary>Cộng thêm thời gian học (phút).</summary>
    public static void AddStudyTime(int minutes)
        => RunSimple(
            "UPDATE AppSettings SET TotalStudyTime = TotalStudyTime + $v, UpdatedAt = datetime('now','localtime') WHERE Id = 1",
            ("$v", minutes));

    /// <summary>Cộng số từ đã học.</summary>
    public static void IncrementWordsLearned(int count)
        => RunSimple(
            "UPDATE AppSettings SET TotalWordsLearned = TotalWordsLearned + $v, UpdatedAt = datetime('now','localtime') WHERE Id = 1",
            ("$v", count));

    /// <summary>Tăng tổng lượt quiz đã chơi lên 1.</summary>
    public static void IncrementQuizPlayed()
        => RunSimple(
            "UPDATE AppSettings SET TotalQuizPlayed = TotalQuizPlayed + 1, UpdatedAt = datetime('now','localtime') WHERE Id = 1");

    /// <summary>Cộng số câu đúng/sai vào tổng thống kê sau khi quiz.</summary>
    public static void AddQuizResult(int correct, int wrong)
    {
        using var conn = AppDbContext.CreateConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE AppSettings
            SET TotalCorrectAnswers = TotalCorrectAnswers + $correct,
                TotalWrongAnswers   = TotalWrongAnswers   + $wrong,
                UpdatedAt           = datetime('now','localtime')
            WHERE Id = 1
            """;
        cmd.Parameters.AddWithValue("$correct", correct);
        cmd.Parameters.AddWithValue("$wrong",   wrong);
        cmd.ExecuteNonQuery();
    }

    // ----------------------------------------------------------------
    // PRIVATE HELPERS
    // ----------------------------------------------------------------

    private static void RunSimple(string sql, params (string Name, object Value)[] parameters)
    {
        using var conn = AppDbContext.CreateConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        foreach (var (name, value) in parameters)
            cmd.Parameters.AddWithValue(name, value);
        cmd.ExecuteNonQuery();
    }

    private static AppSettings MapReader(SqliteDataReader r) => new()
    {
        Id                       = r.GetInt32(r.GetOrdinal("Id")),
        DisplayName              = r.GetString(r.GetOrdinal("DisplayName")),
        Avatar                   = r.IsDBNull(r.GetOrdinal("Avatar"))            ? null : r.GetString(r.GetOrdinal("Avatar")),
        TotalXP                  = r.GetInt32(r.GetOrdinal("TotalXP")),
        Level                    = r.GetInt32(r.GetOrdinal("Level")),
        CurrentStreak            = r.GetInt32(r.GetOrdinal("CurrentStreak")),
        LastActiveDate           = r.IsDBNull(r.GetOrdinal("LastActiveDate"))    ? null : r.GetString(r.GetOrdinal("LastActiveDate")),
        TotalStudyTime           = r.GetInt32(r.GetOrdinal("TotalStudyTime")),
        TotalWordsLearned        = r.GetInt32(r.GetOrdinal("TotalWordsLearned")),
        TotalQuizPlayed          = r.GetInt32(r.GetOrdinal("TotalQuizPlayed")),
        TotalCorrectAnswers      = r.GetInt32(r.GetOrdinal("TotalCorrectAnswers")),
        TotalWrongAnswers        = r.GetInt32(r.GetOrdinal("TotalWrongAnswers")),
        DailyWordGoal            = r.GetInt32(r.GetOrdinal("DailyWordGoal")),
        DailyStudyTimeGoal       = r.GetInt32(r.GetOrdinal("DailyStudyTimeGoal")),
        Theme                    = r.GetString(r.GetOrdinal("Theme")),
        Language                 = r.GetString(r.GetOrdinal("Language")),
        AutoPlayPronunciation    = r.GetInt32(r.GetOrdinal("AutoPlayPronunciation")) == 1,
        SpeechRate               = r.GetDouble(r.GetOrdinal("SpeechRate")),
        SpeechVolume             = r.GetInt32(r.GetOrdinal("SpeechVolume")),
        SpeechVoice              = r.IsDBNull(r.GetOrdinal("SpeechVoice"))       ? null : r.GetString(r.GetOrdinal("SpeechVoice")),
        DefaultQuizQuestionCount = r.GetInt32(r.GetOrdinal("DefaultQuizQuestionCount")),
        ShuffleQuestions         = r.GetInt32(r.GetOrdinal("ShuffleQuestions"))  == 1,
        ShuffleAnswers           = r.GetInt32(r.GetOrdinal("ShuffleAnswers"))    == 1,
        AutoBackup               = r.GetInt32(r.GetOrdinal("AutoBackup"))        == 1,
        BackupFolder             = r.IsDBNull(r.GetOrdinal("BackupFolder"))      ? null : r.GetString(r.GetOrdinal("BackupFolder")),
        EnableReminder           = r.GetInt32(r.GetOrdinal("EnableReminder"))    == 1,
        ReminderTime             = r.IsDBNull(r.GetOrdinal("ReminderTime"))      ? null : r.GetString(r.GetOrdinal("ReminderTime")),
        DatabaseVersion          = r.GetInt32(r.GetOrdinal("DatabaseVersion")),
        CreatedAt                = r.GetString(r.GetOrdinal("CreatedAt")),
        UpdatedAt                = r.GetString(r.GetOrdinal("UpdatedAt")),
    };
}
