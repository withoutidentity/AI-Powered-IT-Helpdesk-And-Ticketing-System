using System.Text;
using System.Text.RegularExpressions;
using Application.Common.Interfaces;
using Application.Common.Models;

namespace Application.KnowledgeBase.Services;

public sealed class MarkdownKnowledgeDocumentChunker : IKnowledgeDocumentChunker
{
    private const int TargetWords = 220;
    private const int OverlapWords = 35;
    private static readonly Regex HeadingRegex = new(@"^#{1,6}\s+", RegexOptions.Compiled);

    public IReadOnlyList<KnowledgeDocumentChunk> Split(string content)
    {
        var normalized = Normalize(content);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return [];
        }

        var sections = SplitIntoMarkdownSections(normalized);
        var chunks = new List<string>();

        foreach (var section in sections)
        {
            chunks.AddRange(SplitSection(section));
        }

        return chunks
            .Where(chunk => !string.IsNullOrWhiteSpace(chunk))
            .Select((chunk, index) => new KnowledgeDocumentChunk(index, chunk.Trim(), EstimateTokenCount(chunk)))
            .ToList();
    }

    private static IReadOnlyList<string> SplitIntoMarkdownSections(string content)
    {
        var sections = new List<string>();
        var current = new StringBuilder();

        foreach (var line in content.Split('\n'))
        {
            var trimmedLine = line.TrimEnd();
            if (HeadingRegex.IsMatch(trimmedLine) && current.Length > 0)
            {
                sections.Add(current.ToString().Trim());
                current.Clear();
            }

            current.AppendLine(trimmedLine);
        }

        if (current.Length > 0)
        {
            sections.Add(current.ToString().Trim());
        }

        return sections.Count == 0 ? [content] : sections;
    }

    private static IReadOnlyList<string> SplitSection(string section)
    {
        var words = SplitWords(section);
        if (words.Count <= TargetWords)
        {
            return [section.Trim()];
        }

        var chunks = new List<string>();
        var start = 0;
        while (start < words.Count)
        {
            var take = Math.Min(TargetWords, words.Count - start);
            chunks.Add(string.Join(' ', words.Skip(start).Take(take)));

            if (start + take >= words.Count)
            {
                break;
            }

            start += TargetWords - OverlapWords;
        }

        return chunks;
    }

    private static string Normalize(string content)
    {
        return content.Replace("\r\n", "\n").Replace('\r', '\n').Trim();
    }

    private static IReadOnlyList<string> SplitWords(string content)
    {
        return content.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).ToList();
    }

    private static int EstimateTokenCount(string content)
    {
        return SplitWords(content).Count;
    }
}