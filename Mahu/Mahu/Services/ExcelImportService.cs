using ClosedXML.Excel;
using Mahu.Data.Models;
using System.IO;

namespace Mahu.Services;

public class ExcelImportService
{
    public class SheetImportResult
    {
        public string SheetName { get; set; } = string.Empty;
        public List<Vocabulary> ValidWords { get; set; } = new();
        public int ErrorRows { get; set; }
    }

    /// <summary>
    /// Parse an Excel file and return a list of SheetImportResults.
    /// Each sheet corresponds to a packet.
    /// </summary>
    public static List<SheetImportResult> ParseExcelFile(string filePath)
    {
        var results = new List<SheetImportResult>();

        using var workbook = new XLWorkbook(filePath);
        foreach (var worksheet in workbook.Worksheets)
        {
            var sheetResult = new SheetImportResult { SheetName = worksheet.Name };
            var rows = worksheet.RowsUsed().Skip(1); // Skip header row

            var headers = GetHeaders(worksheet);

            foreach (var row in rows)
            {
                var vocab = ParseRow(row, headers);
                if (vocab != null)
                {
                    sheetResult.ValidWords.Add(vocab);
                }
                else
                {
                    sheetResult.ErrorRows++;
                }
            }

            // Only add the result if there's at least one valid word or error, or we just want to show empty sheets
            if (sheetResult.ValidWords.Any() || sheetResult.ErrorRows > 0)
            {
                results.Add(sheetResult);
            }
        }

        return results;
    }

    private static Dictionary<string, int> GetHeaders(IXLWorksheet ws)
    {
        var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var firstRow = ws.Row(1);
        int col = 1;
        while (!firstRow.Cell(col).IsEmpty())
        {
            string headerName = firstRow.Cell(col).GetString().Trim();
            if (!string.IsNullOrEmpty(headerName))
            {
                headers[headerName] = col;
            }
            col++;
        }
        return headers;
    }

    private static Vocabulary? ParseRow(IXLRow row, Dictionary<string, int> headers)
    {
        string GetString(string colName)
        {
            if (headers.TryGetValue(colName, out int colIdx))
            {
                var val = row.Cell(colIdx).GetString()?.Trim();
                return string.IsNullOrEmpty(val) ? null : val;
            }
            return null;
        }

        string? word = GetString("Word");
        string? meaning = GetString("Meaning");

        // Word and Meaning are required
        if (string.IsNullOrEmpty(word) || string.IsNullOrEmpty(meaning))
        {
            return null;
        }

        int difficulty = 1;
        string? diffStr = GetString("Difficulty");
        if (int.TryParse(diffStr, out int parsedDiff))
        {
            difficulty = Math.Clamp(parsedDiff, 1, 5);
        }

        return new Vocabulary
        {
            Word = word,
            Meaning = meaning,
            WordType = GetString("WordType"),
            Phonetic = GetString("Phonetic"),
            Example = GetString("Example"),
            ExampleMeaning = GetString("ExampleMeaning"),
            Difficulty = difficulty
        };
    }
}
