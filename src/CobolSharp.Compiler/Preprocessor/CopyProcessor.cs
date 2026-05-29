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
            int copyIdx = FindCopyKeyword(text, pos);
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

            // Optional library qualifier: COPY text-name (IN | OF) library-name.
            if (afterCopy < text.Length && (MatchWord(text, afterCopy, "IN") || MatchWord(text, afterCopy, "OF")))
            {
                afterCopy += 2;
                SkipWhitespace(text, ref afterCopy);
                ReadWord(text, ref afterCopy); // library-name — we search all paths regardless
                SkipWhitespace(text, ref afterCopy);
            }

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
                // Library text is itself in reference (fixed) format — normalize it to free
                // form (strip sequence/identification areas, expand continuations) exactly as
                // the main source was, so inserted lines align in the program's source area.
                string copybookText = NormalizeCopybook(File.ReadAllText(copybookPath));

                foreach (var (from, to) in replacements)
                {
                    if (string.IsNullOrEmpty(from)) continue; // malformed/empty operand — skip
                    copybookText = copybookText.Replace(from, to, StringComparison.OrdinalIgnoreCase);
                }

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

    /// <summary>
    /// Normalize copy-library text to free form. Library members are reference (fixed) format,
    /// but CCVS members use non-standard indicator letters (C, G) in column 7 that the general
    /// <see cref="ReferenceFormatProcessor.IsFixedForm"/> heuristic rejects. Detect fixed form
    /// from the sequence-number area (columns 1-6 numeric) instead, then convert; fall back to
    /// the general normalizer for anything that does not look like a sequence-numbered member.
    /// </summary>
    private static string NormalizeCopybook(string text)
    {
        var lines = text.Split('\n');
        int seqLines = 0, total = 0;
        foreach (var raw in lines)
        {
            var line = raw.TrimEnd('\r');
            if (string.IsNullOrWhiteSpace(line) || line.Length < 7) continue;
            total++;
            bool seqDigits = true, anyDigit = false;
            for (int i = 0; i < 6 && i < line.Length; i++)
            {
                if (char.IsDigit(line[i])) anyDigit = true;
                else if (line[i] != ' ') { seqDigits = false; break; }
            }
            if (seqDigits && anyDigit) seqLines++;
        }
        bool fixedForm = total > 0 && seqLines * 100 / total >= 50;
        return fixedForm
            ? ReferenceFormatProcessor.ConvertFixedToFree(text)
            : ReferenceFormatProcessor.NormalizeToFreeForm(text);
    }

    /// <summary>
    /// Find the next COPY statement keyword from <paramref name="startPos"/>. Unlike a
    /// line-start scan, COPY may appear anywhere a separator is allowed — after a level number
    /// (77 COPY K1W03.), after a data-name (01 TST-TEST COPY K101A.), or inside a statement
    /// (ADD COPY K1P01. TO …). The scan honours source structure: it never matches inside an
    /// alphanumeric literal ("… COPY …") nor inside a free-form '*&gt;' comment, and requires
    /// word boundaries so COPYSECT-1 is not mistaken for COPY.
    /// </summary>
    private static int FindCopyKeyword(string text, int startPos)
    {
        bool inString = false;
        char quote = '"';
        for (int i = startPos; i < text.Length; i++)
        {
            char c = text[i];

            if (inString)
            {
                if (c == quote)
                {
                    if (i + 1 < text.Length && text[i + 1] == quote) { i++; continue; } // doubled quote
                    inString = false;
                }
                continue;
            }

            if (c == '"' || c == '\'') { inString = true; quote = c; continue; }

            // Free-form comment: '*>' to end of line.
            if (c == '*' && i + 1 < text.Length && text[i + 1] == '>')
            {
                while (i < text.Length && text[i] != '\n') i++;
                continue;
            }

            if ((c is 'C' or 'c') && MatchWord(text, i, "COPY"))
            {
                bool boundBefore = i == 0 || (!char.IsLetterOrDigit(text[i - 1]) && text[i - 1] is not ('-' or '_'));
                int after = i + 4;
                bool boundAfter = after >= text.Length || (!char.IsLetterOrDigit(text[after]) && text[after] is not ('-' or '_'));
                if (boundBefore && boundAfter)
                    return i;
            }
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
            // An alphanumeric literal operand. The quotation marks are part of the literal
            // token (REPLACE …-1 BY "TRUE " must yield a quoted literal in the source, not the
            // bare word TRUE), so include them in the returned text.
            char quote = text[pos];
            int start = pos;
            pos++;
            while (pos < text.Length && text[pos] != quote)
                pos++;
            if (pos < text.Length) pos++; // consume closing quote
            return text[start..pos];
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
