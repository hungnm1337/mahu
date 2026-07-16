using System.IO;
using Microsoft.Data.Sqlite;

namespace Mahu.Data;

/// <summary>
/// Khởi tạo database: tạo file DB và chạy toàn bộ schema nếu chưa tồn tại.
/// </summary>
public static class DatabaseInitializer
{
    /// <summary>
    /// Gọi khi app khởi động.
    /// Nếu DB chưa tồn tại → tạo file và chạy schema + seed data.
    /// Nếu DB đã tồn tại → bỏ qua.
    /// </summary>
    public static void Initialize()
    {
        bool isNewDatabase = !File.Exists(AppDbContext.DbPath);

        using var connection = AppDbContext.CreateConnection();
        connection.Open();

        if (isNewDatabase)
        {
            CreateSchema(connection);
        }
    }

    // ----------------------------------------------------------------
    // PRIVATE
    // ----------------------------------------------------------------

    private static void CreateSchema(SqliteConnection connection)
    {
        using var transaction = connection.BeginTransaction();
        try
        {
            using var cmd = connection.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = SchemaSQL;
            cmd.ExecuteNonQuery();

            // Seed Category mặc định
            cmd.CommandText = """
                INSERT INTO Categories (Id, Name, Description, Color, Icon, DisplayOrder)
                SELECT $Id, 'Chung', 'Danh mục chung', '#808080', 'Folder24', 0
                WHERE NOT EXISTS (SELECT 1 FROM Categories WHERE Name = 'Chung');
                """;
            cmd.Parameters.AddWithValue("$Id", Guid.NewGuid().ToString());
            cmd.ExecuteNonQuery();

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    // ----------------------------------------------------------------
    // SCHEMA SQL (theo db.txt)
    // ----------------------------------------------------------------

    private const string SchemaSQL = """
        PRAGMA foreign_keys = ON;

        -- ============================================================
        -- APP SETTINGS
        -- ============================================================
        CREATE TABLE AppSettings (
            Id INTEGER PRIMARY KEY CHECK (Id = 1),
            DisplayName TEXT NOT NULL DEFAULT 'User',
            Avatar TEXT,
            TotalXP INTEGER NOT NULL DEFAULT 0,
            Level INTEGER NOT NULL DEFAULT 1,
            CurrentStreak INTEGER NOT NULL DEFAULT 0,
            LastActiveDate TEXT,
            TotalStudyTime INTEGER NOT NULL DEFAULT 0,
            TotalWordsLearned INTEGER NOT NULL DEFAULT 0,
            TotalQuizPlayed INTEGER NOT NULL DEFAULT 0,
            TotalCorrectAnswers INTEGER NOT NULL DEFAULT 0,
            TotalWrongAnswers INTEGER NOT NULL DEFAULT 0,
            DailyWordGoal INTEGER NOT NULL DEFAULT 20,
            DailyStudyTimeGoal INTEGER NOT NULL DEFAULT 30,
            Theme TEXT NOT NULL DEFAULT 'System',
            Language TEXT NOT NULL DEFAULT 'en-US',
            AutoPlayPronunciation INTEGER NOT NULL DEFAULT 1,
            SpeechRate REAL NOT NULL DEFAULT 1.0,
            SpeechVolume INTEGER NOT NULL DEFAULT 100,
            SpeechVoice TEXT,
            DefaultQuizQuestionCount INTEGER NOT NULL DEFAULT 20,
            ShuffleQuestions INTEGER NOT NULL DEFAULT 1,
            ShuffleAnswers INTEGER NOT NULL DEFAULT 1,
            AutoBackup INTEGER NOT NULL DEFAULT 0,
            BackupFolder TEXT,
            EnableReminder INTEGER NOT NULL DEFAULT 0,
            ReminderTime TEXT DEFAULT '20:00',
            DatabaseVersion INTEGER NOT NULL DEFAULT 1,
            CreatedAt TEXT NOT NULL DEFAULT(datetime('now','localtime')),
            UpdatedAt TEXT NOT NULL DEFAULT(datetime('now','localtime'))
        );

        INSERT INTO AppSettings(Id) VALUES(1);

        -- ============================================================
        -- CATEGORIES
        -- ============================================================
        CREATE TABLE Categories (
            Id TEXT PRIMARY KEY,
            Name TEXT NOT NULL UNIQUE,
            Description TEXT,
            Color TEXT,
            Icon TEXT,
            DisplayOrder INTEGER NOT NULL DEFAULT 0,
            CreatedAt TEXT NOT NULL DEFAULT(datetime('now','localtime'))
        );

        -- ============================================================
        -- PACKETS
        -- ============================================================
        CREATE TABLE Packets (
            Id TEXT PRIMARY KEY,
            CategoryId TEXT NOT NULL,
            Name TEXT NOT NULL,
            Description TEXT,
            IsFavorite INTEGER NOT NULL DEFAULT 0,
            IsPinned INTEGER NOT NULL DEFAULT 0,
            IsCompleted INTEGER NOT NULL DEFAULT 0,
            CreatedAt TEXT NOT NULL DEFAULT(datetime('now','localtime')),
            UpdatedAt TEXT NOT NULL DEFAULT(datetime('now','localtime')),
            FOREIGN KEY(CategoryId)
                REFERENCES Categories(Id)
                ON DELETE CASCADE
        );

        -- ============================================================
        -- VOCABULARIES
        -- ============================================================
        CREATE TABLE Vocabularies (
            Id TEXT PRIMARY KEY,
            PacketId TEXT NOT NULL,
            Word TEXT NOT NULL,
            WordType TEXT,
            Meaning TEXT NOT NULL,
            Phonetic TEXT,
            Example TEXT,
            ExampleMeaning TEXT,
            Difficulty INTEGER NOT NULL DEFAULT 1
                CHECK(Difficulty BETWEEN 1 AND 5),
            ViewCount INTEGER NOT NULL DEFAULT 0,
            CreatedAt TEXT NOT NULL DEFAULT(datetime('now','localtime')),
            UpdatedAt TEXT NOT NULL DEFAULT(datetime('now','localtime')),
            FOREIGN KEY(PacketId)
                REFERENCES Packets(Id)
                ON DELETE CASCADE
        );

        -- ============================================================
        -- WORD PROGRESS (SM-2)
        -- ============================================================
        CREATE TABLE WordProgress (
            VocabularyId TEXT PRIMARY KEY,
            EaseFactor REAL NOT NULL DEFAULT 2.5,
            Interval INTEGER NOT NULL DEFAULT 0,
            Repetitions INTEGER NOT NULL DEFAULT 0,
            NextReviewDate TEXT NOT NULL,
            LastReviewedDate TEXT,
            State INTEGER NOT NULL DEFAULT 0
                CHECK(State BETWEEN 0 AND 3),
            CorrectCount INTEGER NOT NULL DEFAULT 0,
            WrongCount INTEGER NOT NULL DEFAULT 0,
            FOREIGN KEY(VocabularyId)
                REFERENCES Vocabularies(Id)
                ON DELETE CASCADE
        );

        -- ============================================================
        -- STUDY SESSIONS
        -- ============================================================
        CREATE TABLE StudySessions (
            Id TEXT PRIMARY KEY,
            SessionDate TEXT NOT NULL DEFAULT(date('now','localtime')),
            Duration INTEGER NOT NULL DEFAULT 0,
            WordsReviewed INTEGER NOT NULL DEFAULT 0,
            NewWordCount INTEGER NOT NULL DEFAULT 0,
            ReviewWordCount INTEGER NOT NULL DEFAULT 0,
            XPEarned INTEGER NOT NULL DEFAULT 0,
            Note TEXT,
            CreatedAt TEXT NOT NULL DEFAULT(datetime('now','localtime'))
        );

        -- ============================================================
        -- QUIZ HISTORIES
        -- ============================================================
        CREATE TABLE QuizHistories (
            Id TEXT PRIMARY KEY,
            PacketId TEXT,
            QuizScope TEXT NOT NULL DEFAULT 'Packet',
            QuizType INTEGER NOT NULL
                CHECK(QuizType IN (1,2)),
            TotalQuestions INTEGER NOT NULL,
            CorrectAnswers INTEGER NOT NULL,
            WrongAnswers INTEGER NOT NULL,
            Score INTEGER NOT NULL
                CHECK(Score BETWEEN 0 AND 100),
            Duration INTEGER NOT NULL DEFAULT 0,
            XPEarned INTEGER NOT NULL DEFAULT 0,
            CreatedAt TEXT NOT NULL DEFAULT(datetime('now','localtime')),
            FOREIGN KEY(PacketId)
                REFERENCES Packets(Id)
                ON DELETE SET NULL
        );

        -- ============================================================
        -- ACHIEVEMENTS
        -- ============================================================
        CREATE TABLE Achievements (
            Id TEXT PRIMARY KEY,
            Code TEXT NOT NULL UNIQUE,
            Name TEXT NOT NULL,
            Description TEXT,
            Icon TEXT,
            RewardXP INTEGER NOT NULL DEFAULT 0,
            IsUnlocked INTEGER NOT NULL DEFAULT 0,
            UnlockedAt TEXT
        );

        -- ============================================================
        -- INDEXES
        -- ============================================================
        CREATE INDEX IDX_StudySessions_Date   ON StudySessions(SessionDate);
        CREATE INDEX IDX_QuizHistories_Date   ON QuizHistories(CreatedAt);
        CREATE INDEX IDX_QuizHistories_Packet ON QuizHistories(PacketId);
        CREATE INDEX IDX_Achievements_Code    ON Achievements(Code);
        CREATE INDEX IDX_Categories_Name      ON Categories(Name);
        CREATE INDEX IDX_Packets_Category     ON Packets(CategoryId);
        CREATE INDEX IDX_Vocabularies_Packet  ON Vocabularies(PacketId);
        CREATE INDEX IDX_Vocabularies_Word    ON Vocabularies(Word);
        CREATE INDEX IDX_WordProgress_ReviewDate ON WordProgress(NextReviewDate);
        CREATE INDEX IDX_WordProgress_State      ON WordProgress(State);
        """;
}
