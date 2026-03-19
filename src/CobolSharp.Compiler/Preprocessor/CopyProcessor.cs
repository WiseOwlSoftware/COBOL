// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;

namespace CobolSharp.Compiler.Preprocessor;

/// <summary>
/// Handles COPY statement preprocessing. COPY inserts the contents of a copybook
/// (library text) into the source before lexing. Supports COPY ... REPLACING.
/// </summary>
public sealed class CopyProcessor(IEnumerable<string>? searchPaths = null)
{
    /// <summary>Maximum COPY nesting depth to prevent infinite recursion.</summary>
    private const int MaxCopyDepth = 20;

    /// <summary>File extensions to try when searching for copybooks.</summary>
    private static readonly string[] CopybookExtensions = ["", ".cpy", ".cob", ".cbl", ".CPY", ".COB", ".CBL"];

    private readonly List<string> _searchPaths = new(searchPaths ?? []);

    /// <summary>Add a directory to search for copybooks.</summary>
    public void AddSearchPath(string path) => _searchPaths.Add(path);

    /// <summary>
    /// Process all COPY and REPLACE statements in the source text.
    /// Returns the expanded source text with COPY expanded and REPLACE applied.
    /// </summary>
    public string Process(string sourceText, string sourceDir)
    {
        if (!_searchPaths.Contains(sourceDir))
            _searchPaths.Insert(0, sourceDir);

        string expanded = ExpandCopyStatements(sourceText, new HashSet<string>(StringComparer.OrdinalIgnoreCase), 0);
        return ApplyReplaceStatements(expanded);
    }

    /// <summary>
    /// Process REPLACE statements: REPLACE ==pseudo-text-1== BY ==pseudo-text-2==.
    /// REPLACE OFF turns off active replacements.
    /// </summary>
    private static string ApplyReplaceStatements(string text)
    {
        var result = new StringBuilder();
        var activeReplacements = new List<(string from, string to)>();
        int pos = 0;

        while (pos < text.Length)
        {
            int replaceIdx = FindKeywordAtLineStart(text, pos, "REPLACE");
            if (replaceIdx < 0)
            {
                result.Append(ApplyReplacements(text[pos..], activeReplacements));
                break;
            }

            result.Append(ApplyReplacements(text[pos..replaceIdx], activeReplacements));

            int afterReplace = replaceIdx + "REPLACE".Length;
            SkipWhitespace(text, ref afterReplace);

            if (MatchWord(text, afterReplace, "OFF"))
            {
                afterReplace += "OFF".Length;
                activeReplacements.Clear();
            }
            else
            {
                activeReplacements.Clear();
                ParseReplacements(text, ref afterReplace, activeReplacements);
            }

            while (afterReplace < text.Length && text[afterReplace] != '.')
                afterReplace++;
            if (afterReplace < text.Length) afterReplace++;

            pos = afterReplace;
        }

        return result.ToString();
    }

    private static string ApplyReplacements(string text, List<(string from, string to)> replacements)
    {
        foreach (var (from, to) in replacements)
            text = text.Replace(from, to, StringComparison.OrdinalIgnoreCase);
        return text;
    }

    private string ExpandCopyStatements(string text, HashSet<string> alreadyIncluded, int depth)
    {
        if (depth > MaxCopyDepth)
            return text;

        var result = new StringBuilder();
        int pos = 0;

        while (pos < text.Length)
        {
            int copyIdx = FindKeywordAtLineStart(text, pos, "COPY");
            if (copyIdx < 0)
            {
                result.Append(text, pos, text.Length - pos);
                break;
            }

            result.Append(text, pos, copyIdx - pos);

            int afterCopy = copyIdx + "COPY".Length;
            SkipWhitespace(text, ref afterCopy);

            string libraryName = ReadWord(text, ref afterCopy);
            SkipWhitespace(text, ref afterCopy);

            var replacements = new List<(string from, string to)>();
            if (afterCopy < text.Length && MatchWord(text, afterCopy, "REPLACING"))
            {
                afterCopy += "REPLACING".Length;
                SkipWhitespace(text, ref afterCopy);
                ParseReplacements(text, ref afterCopy, replacements);
            }

            while (afterCopy < text.Length && text[afterCopy] != '.')
                afterCopy++;
            if (afterCopy < text.Length) afterCopy++;

            string? copybookPath = FindCopybook(libraryName);
            if (copybookPath != null && alreadyIncluded.Add(copybookPath))
            {
                string copybookText = File.ReadAllText(copybookPath);

                foreach (var (from, to) in replacements)
                    copybookText = copybookText.Replace(from, to, StringComparison.OrdinalIgnoreCase);

                copybookText = ExpandCopyStatements(copybookText, alreadyIncluded, depth + 1);

                result.AppendLine();
                result.Append(copybookText);
                result.AppendLine();

                alreadyIncluded.Remove(copybookPath);
            }
            else
            {
                result.AppendLine($"*> COPY {libraryName} — copybook not found");
            }

            pos = afterCopy;
        }

        return result.ToString();
    }

    /// <summary>
    /// Find a keyword that is the first significant word on a line (after optional whitespace).
    /// Prevents false matches inside VALUE strings or other data contexts.
    /// </summary>
    private static int FindKeywordAtLineStart(string text, int startPos, string keyword)
    {
        int pos = startPos;

        while (pos < text.Length)
        {
            while (pos < text.Length && text[pos] == ' ')
                pos++;

            if (pos + keyword.Length <= text.Length &&
                MatchWord(text, pos, keyword) &&
                (pos + keyword.Length >= text.Length || !char.IsLetterOrDigit(text[pos + keyword.Length])))
            {
                return pos;
            }

            while (pos < text.Length && text[pos] != '\n')
                pos++;
            if (pos < text.Length) pos++;
        }
        return -1;
    }

    private static bool MatchWord(string text, int pos, string word)
    {
        if (pos + word.Length > text.Length) return false;
        for (int i = 0; i < word.Length; i++)
        {
            if (char.ToUpperInvariant(text[pos + i]) != word[i])
                return false;
        }
        return true;
    }

    private static void SkipWhitespace(string text, ref int pos)
    {
        while (pos < text.Length && char.IsWhiteSpace(text[pos]))
            pos++;
    }

    private static string ReadWord(string text, ref int pos)
    {
        int start = pos;
        while (pos < text.Length && (char.IsLetterOrDigit(text[pos]) || text[pos] is '-' or '_'))
            pos++;
        return text[start..pos];
    }

    private static void ParseReplacements(string text, ref int pos,
        List<(string from, string to)> replacements)
    {
        while (pos < text.Length && text[pos] != '.')
        {
            SkipWhitespace(text, ref pos);
            if (pos >= text.Length || text[pos] == '.') break;

            string from = ReadReplaceOperand(text, ref pos);
            SkipWhitespace(text, ref pos);

            if (MatchWord(text, pos, "BY"))
            {
                pos += "BY".Length;
                SkipWhitespace(text, ref pos);
            }

            string to = ReadReplaceOperand(text, ref pos);
            replacements.Add((from, to));
        }
    }

    private static string ReadReplaceOperand(string text, ref int pos)
    {
        if (pos < text.Length - 1 && text[pos] == '=' && text[pos + 1] == '=')
        {
            pos += 2;
            int start = pos;
            while (pos < text.Length - 1 && !(text[pos] == '=' && text[pos + 1] == '='))
                pos++;
            string result = text[start..pos].Trim();
            if (pos < text.Length - 1) pos += 2;
            return result;
        }

        if (pos < text.Length && text[pos] is '"' or '\'')
        {
            char quote = text[pos];
            pos++;
            int start = pos;
            while (pos < text.Length && text[pos] != quote)
                pos++;
            string result = text[start..pos];
            if (pos < text.Length) pos++;
            return result;
        }

        string word = ReadWord(text, ref pos);
        if (string.IsNullOrEmpty(word) && pos < text.Length)
            pos++;
        return word;
    }

    private string? FindCopybook(string libraryName)
    {
        foreach (var searchPath in _searchPaths)
        {
            foreach (var ext in CopybookExtensions)
            {
                string fullPath = Path.Combine(searchPath, libraryName + ext);
                if (File.Exists(fullPath))
                    return fullPath;
            }
        }
        return null;
    }
}
