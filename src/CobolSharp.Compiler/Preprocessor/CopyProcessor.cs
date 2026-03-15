// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolSharp.Compiler.Preprocessor;

/// <summary>
/// Handles COPY statement preprocessing. COPY inserts the contents of a copybook
/// (library text) into the source before lexing. Supports COPY ... REPLACING.
/// </summary>
public sealed class CopyProcessor
{
    private readonly List<string> _searchPaths;

    public CopyProcessor(IEnumerable<string>? searchPaths = null)
    {
        _searchPaths = new List<string>(searchPaths ?? Array.Empty<string>());
    }

    /// <summary>
    /// Add a directory to search for copybooks.
    /// </summary>
    public void AddSearchPath(string path)
    {
        _searchPaths.Add(path);
    }

    /// <summary>
    /// Process all COPY and REPLACE statements in the source text.
    /// Returns the expanded source text with COPY expanded and REPLACE applied.
    /// </summary>
    public string Process(string sourceText, string sourceDir)
    {
        // Ensure source directory is in search path
        if (!_searchPaths.Contains(sourceDir))
            _searchPaths.Insert(0, sourceDir);

        string expanded = ExpandCopyStatements(sourceText, new HashSet<string>(StringComparer.OrdinalIgnoreCase), 0);

        // Apply REPLACE statements
        expanded = ApplyReplaceStatements(expanded);

        return expanded;
    }

    /// <summary>
    /// Process REPLACE statements: REPLACE ==pseudo-text-1== BY ==pseudo-text-2==.
    /// REPLACE OFF turns off active replacements.
    /// </summary>
    private static string ApplyReplaceStatements(string text)
    {
        var result = new System.Text.StringBuilder();
        var activeReplacements = new List<(string from, string to)>();
        int pos = 0;

        while (pos < text.Length)
        {
            int replaceIdx = FindReplaceStatement(text, pos);
            if (replaceIdx < 0)
            {
                // Apply active replacements to remaining text
                string remaining = text[pos..];
                result.Append(ApplyReplacements(remaining, activeReplacements));
                break;
            }

            // Apply replacements to text before REPLACE
            string before = text[pos..replaceIdx];
            result.Append(ApplyReplacements(before, activeReplacements));

            // Parse REPLACE statement
            int afterReplace = replaceIdx + 7; // past "REPLACE"
            SkipWhitespace(text, ref afterReplace);

            if (MatchWord(text, afterReplace, "OFF"))
            {
                afterReplace += 3;
                activeReplacements.Clear();
            }
            else
            {
                activeReplacements.Clear();
                ParseReplacements(text, ref afterReplace, activeReplacements);
            }

            // Skip to period
            while (afterReplace < text.Length && text[afterReplace] != '.')
                afterReplace++;
            if (afterReplace < text.Length) afterReplace++;

            pos = afterReplace;
        }

        return result.ToString();
    }

    private static int FindReplaceStatement(string text, int startPos)
    {
        // Search line-by-line: REPLACE must be first significant word on a line
        int pos = startPos;

        while (pos < text.Length)
        {
            while (pos < text.Length && text[pos] == ' ')
                pos++;

            if (pos < text.Length - 6 &&
                MatchWord(text, pos, "REPLACE") &&
                (pos + 7 >= text.Length || !char.IsLetterOrDigit(text[pos + 7])))
            {
                return pos;
            }

            while (pos < text.Length && text[pos] != '\n')
                pos++;
            if (pos < text.Length) pos++;
        }
        return -1;
    }

    private static string ApplyReplacements(string text, List<(string from, string to)> replacements)
    {
        foreach (var (from, to) in replacements)
        {
            text = text.Replace(from, to, StringComparison.OrdinalIgnoreCase);
        }
        return text;
    }

    private string ExpandCopyStatements(string text, HashSet<string> alreadyIncluded, int depth)
    {
        if (depth > 20)
            return text; // guard against infinite recursion

        var result = new System.Text.StringBuilder();
        int pos = 0;

        while (pos < text.Length)
        {
            // Look for COPY keyword (case-insensitive)
            int copyIdx = FindCopyStatement(text, pos);
            if (copyIdx < 0)
            {
                result.Append(text, pos, text.Length - pos);
                break;
            }

            // Append everything before COPY
            result.Append(text, pos, copyIdx - pos);

            // Parse the COPY statement
            int afterCopy = copyIdx + 4; // past "COPY"
            SkipWhitespace(text, ref afterCopy);

            // Read library name
            string libraryName = ReadWord(text, ref afterCopy);
            SkipWhitespace(text, ref afterCopy);

            // Check for REPLACING
            var replacements = new List<(string from, string to)>();
            if (afterCopy < text.Length && MatchWord(text, afterCopy, "REPLACING"))
            {
                afterCopy += 9; // past "REPLACING"
                SkipWhitespace(text, ref afterCopy);
                ParseReplacements(text, ref afterCopy, replacements);
            }

            // Skip to period
            while (afterCopy < text.Length && text[afterCopy] != '.')
                afterCopy++;
            if (afterCopy < text.Length) afterCopy++; // skip period

            // Find and load the copybook
            string? copybookPath = FindCopybook(libraryName);
            if (copybookPath != null && alreadyIncluded.Add(copybookPath))
            {
                string copybookText = File.ReadAllText(copybookPath);

                // Apply REPLACING
                foreach (var (from, to) in replacements)
                {
                    copybookText = copybookText.Replace(from, to, StringComparison.OrdinalIgnoreCase);
                }

                // Recursively expand nested COPY statements
                copybookText = ExpandCopyStatements(copybookText, alreadyIncluded, depth + 1);

                result.AppendLine(); // ensure separation
                result.Append(copybookText);
                result.AppendLine();

                alreadyIncluded.Remove(copybookPath); // allow re-inclusion in different contexts
            }
            else
            {
                // Copybook not found — leave a comment
                result.AppendLine($"*> COPY {libraryName} — copybook not found");
            }

            pos = afterCopy;
        }

        return result.ToString();
    }

    private static int FindCopyStatement(string text, int startPos)
    {
        // Search line-by-line: COPY must be the first significant word on a line
        // (after optional whitespace). This avoids matching COPY inside VALUE strings
        // and other data contexts.
        int pos = startPos;

        while (pos < text.Length)
        {
            // Find start of next line
            int lineStart = pos;

            // Skip to first non-whitespace on this line
            while (pos < text.Length && text[pos] == ' ')
                pos++;

            // Check if line starts with COPY (case-insensitive)
            if (pos < text.Length - 3 &&
                MatchWord(text, pos, "COPY") &&
                (pos + 4 >= text.Length || !char.IsLetterOrDigit(text[pos + 4])))
            {
                return pos;
            }

            // Skip to end of line
            while (pos < text.Length && text[pos] != '\n')
                pos++;
            if (pos < text.Length) pos++; // skip \n
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
        while (pos < text.Length && (char.IsLetterOrDigit(text[pos]) || text[pos] == '-' || text[pos] == '_'))
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

            // Read ==pseudo-text== or word
            string from = ReadReplaceOperand(text, ref pos);
            SkipWhitespace(text, ref pos);

            // Expect BY
            if (MatchWord(text, pos, "BY"))
            {
                pos += 2;
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
            // Pseudo-text: ==...==
            pos += 2;
            int start = pos;
            while (pos < text.Length - 1 && !(text[pos] == '=' && text[pos + 1] == '='))
                pos++;
            string result = text[start..pos].Trim();
            if (pos < text.Length - 1) pos += 2; // skip closing ==
            return result;
        }
        else if (pos < text.Length && (text[pos] == '"' || text[pos] == '\''))
        {
            // Quoted string: "..." or '...'
            char quote = text[pos];
            pos++;
            int start = pos;
            while (pos < text.Length && text[pos] != quote)
                pos++;
            string result = text[start..pos];
            if (pos < text.Length) pos++; // skip closing quote
            return result;
        }
        else
        {
            string word = ReadWord(text, ref pos);
            if (string.IsNullOrEmpty(word) && pos < text.Length)
            {
                // Skip unrecognized character to avoid infinite loop
                pos++;
            }
            return word;
        }
    }

    private string? FindCopybook(string libraryName)
    {
        // Try common COBOL copybook extensions
        string[] extensions = { "", ".cpy", ".cob", ".cbl", ".CPY", ".COB", ".CBL" };

        foreach (var searchPath in _searchPaths)
        {
            foreach (var ext in extensions)
            {
                string fullPath = Path.Combine(searchPath, libraryName + ext);
                if (File.Exists(fullPath))
                    return fullPath;
            }
        }

        return null;
    }
}
