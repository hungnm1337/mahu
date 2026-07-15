using Mahu.Data.Models;
using Microsoft.Data.Sqlite;

namespace Mahu.Data.Repositories;

/// <summary>
/// Repository cho bảng Vocabularies (danh sách từ vựng).
/// </summary>
public static class VocabularyRepository
{
    // ----------------------------------------------------------------
    // READ
    // ----------------------------------------------------------------

    /// <summary>Lấy tất cả từ trong một packet.</summary>
    public static List<Vocabulary> GetByPacket(string packetId)
    {
        using var conn = AppDbContext.CreateConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Vocabularies WHERE PacketId = $packetId ORDER BY CreatedAt ASC";
        cmd.Parameters.AddWithValue("$packetId", packetId);
        using var reader = cmd.ExecuteReader();
        var list = new List<Vocabulary>();
        while (reader.Read()) list.Add(MapReader(reader));
        return list;
    }

    /// <summary>Lấy một từ theo Id.</summary>
    public static Vocabulary? GetById(string id)
    {
        using var conn = AppDbContext.CreateConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Vocabularies WHERE Id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        using var reader = cmd.ExecuteReader();
        return reader.Read() ? MapReader(reader) : null;
    }

    /// <summary>Tìm kiếm từ theo từ khóa (Word hoặc Meaning).</summary>
    public static List<Vocabulary> Search(string keyword)
    {
        using var conn = AppDbContext.CreateConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT * FROM Vocabularies
            WHERE Word LIKE $kw OR Meaning LIKE $kw
            ORDER BY Word ASC
            """;
        cmd.Parameters.AddWithValue("$kw", $"%{keyword}%");
        using var reader = cmd.ExecuteReader();
        var list = new List<Vocabulary>();
        while (reader.Read()) list.Add(MapReader(reader));
        return list;
    }

    /// <summary>Lọc từ theo độ khó (1–5) trong một packet.</summary>
    public static List<Vocabulary> GetByDifficulty(string packetId, int difficulty)
    {
        using var conn = AppDbContext.CreateConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT * FROM Vocabularies
            WHERE PacketId = $packetId AND Difficulty = $difficulty
            ORDER BY Word ASC
            """;
        cmd.Parameters.AddWithValue("$packetId",   packetId);
        cmd.Parameters.AddWithValue("$difficulty", difficulty);
        using var reader = cmd.ExecuteReader();
        var list = new List<Vocabulary>();
        while (reader.Read()) list.Add(MapReader(reader));
        return list;
    }

    /// <summary>Đếm số từ trong một packet.</summary>
    public static int GetCount(string packetId)
    {
        using var conn = AppDbContext.CreateConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM Vocabularies WHERE PacketId = $packetId";
        cmd.Parameters.AddWithValue("$packetId", packetId);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    /// <summary>
    /// Lấy ngẫu nhiên N từ trong một packet (dùng cho Quiz).
    /// Nếu packet có ít từ hơn count, trả về tất cả.
    /// </summary>
    public static List<Vocabulary> GetRandomSample(string packetId, int count)
    {
        using var conn = AppDbContext.CreateConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT * FROM Vocabularies
            WHERE PacketId = $packetId
            ORDER BY RANDOM()
            LIMIT $count
            """;
        cmd.Parameters.AddWithValue("$packetId", packetId);
        cmd.Parameters.AddWithValue("$count",    count);
        using var reader = cmd.ExecuteReader();
        var list = new List<Vocabulary>();
        while (reader.Read()) list.Add(MapReader(reader));
        return list;
    }

    // ----------------------------------------------------------------
    // WRITE
    // ----------------------------------------------------------------

    /// <summary>Thêm một từ mới. Id được tự sinh GUID nếu rỗng.</summary>
    public static void Insert(Vocabulary vocab)
    {
        if (string.IsNullOrEmpty(vocab.Id))
            vocab.Id = Guid.NewGuid().ToString();

        using var conn = AppDbContext.CreateConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO Vocabularies
                (Id, PacketId, Word, WordType, Meaning, Phonetic, Example, ExampleMeaning, Difficulty, ViewCount)
            VALUES
                ($Id, $PacketId, $Word, $WordType, $Meaning, $Phonetic, $Example, $ExampleMeaning, $Difficulty, 0)
            """;
        BindParams(cmd, vocab);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Thêm nhiều từ cùng lúc trong một transaction (hiệu quả hơn insert từng cái).
    /// </summary>
    public static void InsertMany(IEnumerable<Vocabulary> vocabs)
    {
        using var conn = AppDbContext.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = """
            INSERT INTO Vocabularies
                (Id, PacketId, Word, WordType, Meaning, Phonetic, Example, ExampleMeaning, Difficulty, ViewCount)
            VALUES
                ($Id, $PacketId, $Word, $WordType, $Meaning, $Phonetic, $Example, $ExampleMeaning, $Difficulty, 0)
            """;

        // Pre-create parameters
        cmd.Parameters.Add("$Id",            SqliteType.Text);
        cmd.Parameters.Add("$PacketId",      SqliteType.Text);
        cmd.Parameters.Add("$Word",          SqliteType.Text);
        cmd.Parameters.Add("$WordType",      SqliteType.Text);
        cmd.Parameters.Add("$Meaning",       SqliteType.Text);
        cmd.Parameters.Add("$Phonetic",      SqliteType.Text);
        cmd.Parameters.Add("$Example",       SqliteType.Text);
        cmd.Parameters.Add("$ExampleMeaning",SqliteType.Text);
        cmd.Parameters.Add("$Difficulty",    SqliteType.Integer);

        foreach (var vocab in vocabs)
        {
            if (string.IsNullOrEmpty(vocab.Id))
                vocab.Id = Guid.NewGuid().ToString();

            cmd.Parameters["$Id"].Value            = vocab.Id;
            cmd.Parameters["$PacketId"].Value      = vocab.PacketId;
            cmd.Parameters["$Word"].Value          = vocab.Word;
            cmd.Parameters["$WordType"].Value      = (object?)vocab.WordType      ?? DBNull.Value;
            cmd.Parameters["$Meaning"].Value       = vocab.Meaning;
            cmd.Parameters["$Phonetic"].Value      = (object?)vocab.Phonetic      ?? DBNull.Value;
            cmd.Parameters["$Example"].Value       = (object?)vocab.Example       ?? DBNull.Value;
            cmd.Parameters["$ExampleMeaning"].Value= (object?)vocab.ExampleMeaning?? DBNull.Value;
            cmd.Parameters["$Difficulty"].Value    = vocab.Difficulty;
            cmd.ExecuteNonQuery();
        }
        tx.Commit();
    }

    /// <summary>Cập nhật thông tin từ vựng.</summary>
    public static void Update(Vocabulary vocab)
    {
        using var conn = AppDbContext.CreateConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE Vocabularies SET
                Word          = $Word,
                WordType      = $WordType,
                Meaning       = $Meaning,
                Phonetic      = $Phonetic,
                Example       = $Example,
                ExampleMeaning= $ExampleMeaning,
                Difficulty    = $Difficulty,
                UpdatedAt     = datetime('now','localtime')
            WHERE Id = $Id
            """;
        BindParams(cmd, vocab);
        cmd.ExecuteNonQuery();
    }

    /// <summary>Xóa từ vựng (CASCADE → WordProgress).</summary>
    public static void Delete(string id)
    {
        using var conn = AppDbContext.CreateConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM Vocabularies WHERE Id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    /// <summary>Tăng ViewCount lên 1 mỗi khi từ được xem/ôn.</summary>
    public static void IncrementViewCount(string id)
    {
        using var conn = AppDbContext.CreateConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE Vocabularies SET ViewCount = ViewCount + 1 WHERE Id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    // ----------------------------------------------------------------
    // PRIVATE
    // ----------------------------------------------------------------

    private static void BindParams(SqliteCommand cmd, Vocabulary v)
    {
        cmd.Parameters.AddWithValue("$Id",             v.Id);
        cmd.Parameters.AddWithValue("$PacketId",       v.PacketId);
        cmd.Parameters.AddWithValue("$Word",           v.Word);
        cmd.Parameters.AddWithValue("$WordType",       (object?)v.WordType       ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$Meaning",        v.Meaning);
        cmd.Parameters.AddWithValue("$Phonetic",       (object?)v.Phonetic       ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$Example",        (object?)v.Example        ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$ExampleMeaning", (object?)v.ExampleMeaning ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$Difficulty",     v.Difficulty);
    }

    private static Vocabulary MapReader(SqliteDataReader r) => new()
    {
        Id             = r.GetString(r.GetOrdinal("Id")),
        PacketId       = r.GetString(r.GetOrdinal("PacketId")),
        Word           = r.GetString(r.GetOrdinal("Word")),
        WordType       = r.IsDBNull(r.GetOrdinal("WordType"))       ? null : r.GetString(r.GetOrdinal("WordType")),
        Meaning        = r.GetString(r.GetOrdinal("Meaning")),
        Phonetic       = r.IsDBNull(r.GetOrdinal("Phonetic"))       ? null : r.GetString(r.GetOrdinal("Phonetic")),
        Example        = r.IsDBNull(r.GetOrdinal("Example"))        ? null : r.GetString(r.GetOrdinal("Example")),
        ExampleMeaning = r.IsDBNull(r.GetOrdinal("ExampleMeaning")) ? null : r.GetString(r.GetOrdinal("ExampleMeaning")),
        Difficulty     = r.GetInt32(r.GetOrdinal("Difficulty")),
        ViewCount      = r.GetInt32(r.GetOrdinal("ViewCount")),
        CreatedAt      = r.GetString(r.GetOrdinal("CreatedAt")),
        UpdatedAt      = r.GetString(r.GetOrdinal("UpdatedAt")),
    };
}
