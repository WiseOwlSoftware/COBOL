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

    /// <summary>A COBOL text-word (ISO §7.3.2) with its span in the source string.</summary>
    private readonly record struct TextWord(string Value, int Start, int End);

    /// <summary>
    /// Apply COBOL REPLACE / COPY REPLACING substitutions on a TEXT-WORD basis. Pseudo-text
    /// matching ignores the amount of intervening white space and line breaks (ISO §7.4.6 /
    /// §7.5): each operand is a sequence of text words, matched word-for-word against the source
    /// words. A match's original span is replaced verbatim by the replacement text. Matching is
    /// left-to-right; at each position the first operand (in source order) that matches wins, and
    /// inserted text is not rescanned. This replaces a naive substring replace, which could not
    /// match multi-line pseudo-text nor respect word boundaries (".", "(", ")" are their own words).
    /// </summary>
    private static string ApplyReplacements(string text, List<(string from, string to)> replacements)
    {
        var active = replacements
            .Select(r => (tokens: TokenizeTextWords(r.from).Select(w => w.Value).ToList(), r.to))
            .Where(r => r.tokens.Count > 0)   // empty/malformed operand cannot match
            .ToList();
        if (active.Count == 0) return text;

        var words = TokenizeTextWords(text);
        var sb = new StringBuilder();
        int copiedUpTo = 0; // chars of `text` already emitted
        int w = 0;
        while (w < words.Count)
        {
            bool matched = false;
            foreach (var (tokens, to) in active)
            {
                if (w + tokens.Count > words.Count) continue;
                bool eq = true;
                for (int k = 0; k < tokens.Count; k++)
                    if (!string.Equals(words[w + k].Value, tokens[k], StringComparison.OrdinalIgnoreCase))
                    { eq = false; break; }
                if (!eq) continue;

                int matchStart = words[w].Start;
                int matchEnd = words[w + tokens.Count - 1].End;
                sb.Append(text, copiedUpTo, matchStart - copiedUpTo);
                sb.Append(to);
                copiedUpTo = matchEnd;
                w += tokens.Count;
                matched = true;
                break;
            }
            if (!matched) w++;
        }
        sb.Append(text, copiedUpTo, text.Length - copiedUpTo);
        return sb.ToString();
    }

    /// <summary>
    /// Tokenize COBOL text into text-words with their source spans. White space separates words;
    /// '(' and ')' and a separator period (a '.' not acting as a decimal point) are standalone
    /// words; an alphanumeric literal ("…" or '…') is a single word. Used for REPLACE matching.
    /// </summary>
    private static List<TextWord> TokenizeTextWords(string text)
    {
        var words = new List<TextWord>();
        int i = 0, n = text.Length;
        while (i < n)
        {
            char c = text[i];
            // White space and the separator comma/semicolon are equivalent and insignificant
            // (COBOL-85 REPLACE/COPY matching) — skip them.
            if (char.IsWhiteSpace(c) || IsSpaceEquivalentSeparator(text, i)) { i++; continue; }

            // Comment lines are not text words (ISO §7.3.2): a free-form '*>' comment (which a
            // fixed-form comment or debug line was normalized into) is transparent to REPLACE
            // matching, so a pseudo-text operand can span words separated by comment lines.
            if (c == '*' && i + 1 < n && text[i + 1] == '>')
            {
                while (i < n && text[i] != '\n') i++;
                continue;
            }

            if (c is '(' or ')')
            {
                words.Add(new TextWord(c.ToString(), i, i + 1));
                i++;
                continue;
            }

            if (c is '"' or '\'')
            {
                int start = i;
                char q = c;
                i++;
                while (i < n && text[i] != q) i++;
                if (i < n) i++; // closing quote
                words.Add(new TextWord(text[start..i], start, i));
                continue;
            }

            // A separator period/comma/semicolon is its own text word.
            if (IsSeparatorPunctuation(text, i))
            {
                words.Add(new TextWord(text[i].ToString(), i, i + 1));
                i++;
                continue;
            }

            int ws = i;
            while (i < n)
            {
                char d = text[i];
                if (char.IsWhiteSpace(d) || d is '(' or ')' or '"' or '\'') break;
                if (IsSeparatorPunctuation(text, i) || IsSpaceEquivalentSeparator(text, i)) break;
                i++;
            }
            words.Add(new TextWord(text[ws..i], ws, i));
        }
        return words;
    }

    /// <summary>
    /// True if the character at <paramref name="i"/> is a separator period — a '.' that stands as
    /// its own text word, i.e. not acting as a decimal point (not immediately followed by a digit).
    /// The separator comma and semicolon are NOT text words: per COBOL-85 they are equivalent to a
    /// space in REPLACE/COPY matching (handled by <see cref="IsSpaceEquivalentSeparator"/>).
    /// </summary>
    private static bool IsSeparatorPunctuation(string text, int i)
    {
        if (text[i] != '.') return false;
        return i + 1 >= text.Length || !char.IsDigit(text[i + 1]);
    }

    /// <summary>
    /// True if the character at <paramref name="i"/> is a separator comma or semicolon. COBOL-85
    /// treats these as equivalent to a space when matching pseudo-text (so "MOVE; X , Y" and
    /// "MOVE X Y" match), so the tokenizer skips them like white space rather than emitting a word.
    /// A comma/semicolon immediately followed by a digit is left intact (possible decimal comma).
    /// </summary>
    private static bool IsSpaceEquivalentSeparator(string text, int i)
    {
        char c = text[i];
        if (c is not (',' or ';')) return false;
        return i + 1 >= text.Length || !char.IsDigit(text[i + 1]);
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

                // COPY … REPLACING uses the same text-word matching as REPLACE (ISO §7.4.6):
                // operands match library text-words ignoring intervening white space/line breaks.
                copybookText = ApplyReplacements(copybookText, replacements);

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

        // identifier-1/2 or word-1/2 (COBOL-85 COPY … REPLACING): a data-name with optional
        // OF/IN qualifiers and an optional subscript — e.g. WRK IN GRP-002 (1). A plain word
        // (including a signed number such as +2) is the degenerate single-text-word case. The
        // verbatim span is returned: for matching it is tokenized into text words, and as a
        // replacement it is inserted as written.
        int idStart = pos;
        if (string.IsNullOrEmpty(ReadTextWord(text, ref pos)))
        {
            if (pos < text.Length) pos++; // make progress on an unexpected character
            return text[idStart..pos];
        }

        // OF/IN qualifier chain.
        while (true)
        {
            int mark = pos;
            SkipWhitespace(text, ref pos);
            string kw = ReadTextWord(text, ref pos);
            if (string.Equals(kw, "OF", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(kw, "IN", StringComparison.OrdinalIgnoreCase))
            {
                SkipWhitespace(text, ref pos);
                ReadTextWord(text, ref pos); // qualifier data-name
                continue;
            }
            pos = mark; // not a qualifier — leave it for the next operand
            break;
        }

        // Optional subscript: a balanced ( … ) group immediately following the name.
        int beforeSub = pos;
        SkipWhitespace(text, ref pos);
        if (pos < text.Length && text[pos] == '(')
        {
            int depth = 0;
            while (pos < text.Length)
            {
                char d = text[pos];
                if (d == '(') depth++;
                else if (d == ')') { depth--; pos++; if (depth == 0) break; continue; }
                pos++;
            }
        }
        else
        {
            pos = beforeSub;
        }

        return text[idStart..pos];
    }

    /// <summary>
    /// Read one COBOL text word starting at <paramref name="pos"/>: a maximal run of characters
    /// that are not white space, parentheses, quotes, or a separator period/comma/semicolon.
    /// (Quotes and parentheses are handled by the callers.) Used to read REPLACING operands.
    /// </summary>
    private static string ReadTextWord(string text, ref int pos)
    {
        int start = pos;
        while (pos < text.Length)
        {
            char d = text[pos];
            if (char.IsWhiteSpace(d) || d is '(' or ')' or '"' or '\'') break;
            if (IsSeparatorPunctuation(text, pos) || IsSpaceEquivalentSeparator(text, pos)) break;
            pos++;
        }
        return text[start..pos];
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
