// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;
using CobolNet.Editions;
using CobolNet.Frontend.Common;
using CobolNet.Frontend.Diagnostics;

namespace CobolNet.Frontend.Preprocessor;

/// <summary>
/// Handles COPY statement preprocessing. COPY inserts the contents of a copybook
/// (library text) into the source before lexing. Supports COPY ... REPLACING.
/// </summary>
public sealed class CopyProcessor(
    IEnumerable<string>? searchPaths = null,
    DiagnosticBag? diagnostics = null,
    string sourceName = "<source>",
    bool strict = false,
    int dialectLevel = 85,
    bool permissive = false)
{
    // One COBOLNET0902 per compilation for the VCR-row-4 gate (COPY REPLACING non-pseudo-text, W3 — DEVLOG 598).
    private bool _nonPseudoTextFlagged;

    /// <summary>The VCR-row-4 gate: non-pseudo-text COPY REPLACING operands (identifier/literal/word forms)
    /// were REMOVED by ISO 2023 (Annex E.2 item 1 bullet 4 — "Removal of support for non-pseudo-text operands
    /// in the replacing phrase of the COPY statement"). Error strict / warning permissive at ≥2023, silent
    /// below (the ==pseudo-text== form is the only 2023-conforming shape); the pre-removal substitution
    /// semantics are preserved either way. The strict/permissive decision is the ONE
    /// <see cref="EditionSeverityPolicy"/> (P2.9 — never a local <c>if(permissive)</c>).</summary>
    private void OnNonPseudoTextOperand(MappedText mapped, int pos)
    {
        if (dialectLevel < 2023 || _nonPseudoTextFlagged || _diagnostics is null) return;
        _nonPseudoTextFlagged = true;
        var at = mapped.OriginAt(pos);   // the SOURCE origin (kb/Work PB82)
        int line = at.Line;
        const string msg = "a non-pseudo-text COPY REPLACING operand (identifier/literal/word) was removed in "
            + "COBOL-2023 (Annex E.2 item 1 bullet 4) — use ==pseudo-text==; first use at line ";
        var loc = at.ToLocation();
        var severity = EditionSeverityPolicy.For(ConstructAvailability.Removed, EditionInfo.Of(dialectLevel, permissive));
        if (severity == EditionSeverity.Error)
            _diagnostics.ReportError("COBOLNET0902", msg + line, loc, default);
        else
            _diagnostics.ReportWarning("COBOLNET0902", msg + line, loc, default);
    }
    /// <summary>Maximum COPY nesting depth to prevent infinite recursion.</summary>
    private const int MaxCopyDepth = 20;

    /// <summary>File extensions to try when searching for copybooks.</summary>
    private static readonly string[] CopybookExtensions = ["", ".cpy", ".cob", ".cbl", ".CPY", ".COB", ".CBL"];

    private readonly List<string> _searchPaths = new(searchPaths ?? []);

    // Diagnostic plumbing (DEVLOG 307). Optional so the standalone preprocess CLI and tests can construct
    // a CopyProcessor without a bag; when absent, behavior is unchanged (silent). `strict` gates the
    // missing-copybook error to named-strict dialects so the permissive Default/--nist path is unaffected.
    private readonly DiagnosticBag? _diagnostics = diagnostics;
    private readonly string _sourceName = sourceName;
    private readonly bool _strict = strict;

    /// <summary>Add a directory to search for copybooks.</summary>
    public void AddSearchPath(string path) => _searchPaths.Add(path);

    /// <summary>Ensure the source's own directory is searched FIRST (the <see cref="Process"/> setup, ISO §7.2.3)
    /// — used by the merged CC+COPY driver, which calls <see cref="ExpandCopiesOneLevel"/> directly.</summary>
    internal void RegisterSourceDir(string sourceDir)
    {
        if (!_searchPaths.Contains(sourceDir)) _searchPaths.Insert(0, sourceDir);
    }

    /// <summary>Report at a SOURCE origin (kb/Work PB82) — the file and physical line the text at a position came
    /// from, never an ordinal of the text being processed.</summary>
    private void Report(DiagnosticDescriptor descriptor, SourceOrigin at, params object[] args)
        => _diagnostics?.Report(descriptor, at.ToLocation(), TextSpan.Empty, args);

    /// <summary>
    /// Process all COPY and REPLACE statements in the source text.
    /// Returns the expanded source text with COPY expanded and REPLACE applied.
    /// </summary>
    public string Process(string sourceText, string sourceDir)
    {
        if (!_searchPaths.Contains(sourceDir))
            _searchPaths.Insert(0, sourceDir);

        string expanded = ExpandCopyStatements(sourceText, new HashSet<string>(StringComparer.OrdinalIgnoreCase), 0);
        return ApplyReplaceStatements(expanded, _diagnostics, _sourceName);
    }

    /// <summary>
    /// Process REPLACE statements: REPLACE ==pseudo-text-1== BY ==pseudo-text-2==.
    /// REPLACE OFF turns off active replacements.
    /// A non-pseudo-text operand (kb/Work R39 — the GCOS/ACU literal spelling) draws COBOLNET1641 when a
    /// <paramref name="diagnostics"/> bag is supplied: REPLACE's operands were never literals in ANY ISO
    /// edition (§7.2.4.2 general format; §7.2.4.3 SR7), unlike COPY's, whose pre-2023 literal forms ride the
    /// separate COBOLNET0902 removal gate. Before this the illegal statement was silently half-parsed and the
    /// failure surfaced downstream as an unrelated undefined-reference.
    /// </summary>
    internal static string ApplyReplaceStatements(string text, DiagnosticBag? diagnostics = null,
        string sourceName = "<source>")
        => ApplyReplaceStatements(MappedText.Identity(text, sourceName), diagnostics, sourceName).Text;

    /// <summary>The MAPPED REPLACE pass (kb/Work PB82): a REPLACE statement's own lines vanish from the resultant
    /// text, and a replacement may change a line count — the kept text keeps its origins, a replacement's lines take
    /// the origin of the line its match started on.</summary>
    internal static MappedText ApplyReplaceStatements(MappedText mapped, DiagnosticBag? diagnostics = null,
        string sourceName = "<source>")
    {
        string text = mapped.Text;
        var w = new OriginWriter();
        var activeReplacements = new List<(string from, string to, ReplaceKind kind)>();
        int pos = 0;

        while (pos < text.Length)
        {
            int replaceIdx = FindKeywordAtLineStart(text, pos, "REPLACE");
            if (replaceIdx < 0)
            {
                w.AppendMapped(ApplyReplacements(mapped.Slice(pos, text.Length - pos), activeReplacements));
                break;
            }

            w.AppendMapped(ApplyReplacements(mapped.Slice(pos, replaceIdx - pos), activeReplacements));

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
                bool flagged = false;   // one report per REPLACE statement
                ParseReplacements(text, ref afterReplace, activeReplacements, p =>
                {
                    if (flagged || diagnostics is null) return;
                    flagged = true;
                    diagnostics.ReportError(
                        Editions.Diagnostics.DiagnosticCatalog.ReplaceOperandNotPseudoText.Code,
                        "a REPLACE statement operand is not pseudo-text — REPLACE admits ==pseudo-text== "
                        + "(and ==partial-word== under LEADING/TRAILING) operands only, in every ISO edition "
                        + "(§7.2.4.2; §7.2.4.3 SR7 bars literals as partial-words). Write ==operand== "
                        + "(empty ==== deletes)",
                        mapped.OriginAt(p).ToLocation(), default);
                });
            }

            while (afterReplace < text.Length && text[afterReplace] != '.')
                afterReplace++;
            if (afterReplace < text.Length) afterReplace++;

            pos = afterReplace;
        }

        return w.Finish();
    }

    /// <summary>A COBOL text-word (ISO §7.3.2) with its span in the source string.</summary>
    private readonly record struct TextWord(string Value, int Start, int End);

    /// <summary>COPY/REPLACE replacement scope: a whole text-word sequence, or the LEADING/TRAILING characters of
    /// a single text-word (ISO §7.2.3.4 GR 9 b / §7.2.4).</summary>
    private enum ReplaceKind { Whole, Leading, Trailing }

    /// <summary>
    /// Apply COBOL REPLACE / COPY REPLACING substitutions on a TEXT-WORD basis. Pseudo-text
    /// matching ignores the amount of intervening white space and line breaks (ISO §7.2.4 /
    /// §7.5): each operand is a sequence of text words, matched word-for-word against the source
    /// words. A match's original span is replaced verbatim by the replacement text. Matching is
    /// left-to-right; at each position the first operand (in source order) that matches wins, and
    /// inserted text is not rescanned. This replaces a naive substring replace, which could not
    /// match multi-line pseudo-text nor respect word boundaries (".", "(", ")" are their own words).
    /// </summary>
    private static string ApplyReplacements(string text, List<(string from, string to, ReplaceKind kind)> replacements)
        => ApplyReplacements(MappedText.Identity(text, "<source>"), replacements).Text;

    /// <summary>The MAPPED word-replacement pass (kb/Work PB82) — the ONE implementation: unchanged text keeps its
    /// origins; a replacement's text (which may span lines) takes the origin of the line its match started on.</summary>
    private static MappedText ApplyReplacements(MappedText mapped, List<(string from, string to, ReplaceKind kind)> replacements)
    {
        var active = replacements
            .Select(r => (tokens: TokenizeTextWords(r.from).Select(w => w.Value).ToList(), r.to, r.kind))
            .Where(r => r.tokens.Count > 0)   // empty/malformed operand cannot match
            .ToList();
        if (active.Count == 0) return mapped;

        string text = mapped.Text;
        var words = TokenizeTextWords(text);
        var sb = new OriginWriter();
        int copiedUpTo = 0; // chars of `text` already emitted
        int w = 0;
        while (w < words.Count)
        {
            bool matched = false;
            foreach (var (tokens, to, kind) in active)
            {
                if (kind == ReplaceKind.Whole)
                {
                    if (w + tokens.Count > words.Count) continue;
                    bool eq = true;
                    for (int k = 0; k < tokens.Count; k++)
                        if (!string.Equals(words[w + k].Value, tokens[k], StringComparison.OrdinalIgnoreCase))
                        { eq = false; break; }
                    if (!eq) continue;

                    int matchStart = words[w].Start;
                    int matchEnd = words[w + tokens.Count - 1].End;
                    sb.AppendSlice(mapped, copiedUpTo, matchStart - copiedUpTo);
                    sb.Append(to.AsSpan(), mapped.OriginAt(matchStart));
                    copiedUpTo = matchEnd;
                    w += tokens.Count;
                    matched = true;
                    break;
                }

                // LEADING / TRAILING: the partial-word operand is a single text-word, matched against the
                // start/end of one source text-word; only the matched run is replaced (ISO §7.2.3.4 GR 9 b).
                string part = tokens[0];
                string word = words[w].Value;
                bool leading = kind == ReplaceKind.Leading
                    && word.Length >= part.Length && word.StartsWith(part, StringComparison.OrdinalIgnoreCase);
                bool trailing = kind == ReplaceKind.Trailing
                    && word.Length >= part.Length && word.EndsWith(part, StringComparison.OrdinalIgnoreCase);
                if (!leading && !trailing) continue;

                sb.AppendSlice(mapped, copiedUpTo, words[w].Start - copiedUpTo);
                SourceOrigin at = mapped.OriginAt(words[w].Start);
                if (leading)
                {
                    sb.Append(to.AsSpan(), at);
                    sb.Append(word.AsSpan(part.Length, word.Length - part.Length), at);
                }
                else // trailing
                {
                    sb.Append(word.AsSpan(0, word.Length - part.Length), at);
                    sb.Append(to.AsSpan(), at);
                }
                copiedUpTo = words[w].End;
                w++;
                matched = true;
                break;
            }
            if (!matched) w++;
        }
        sb.AppendSlice(mapped, copiedUpTo, text.Length - copiedUpTo);
        return sb.Finish();
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

            // Comment lines are not text words (ISO §7.3.2): an ordinary '*>' comment is
            // transparent to REPLACE matching, so a pseudo-text operand can span words separated
            // by comment lines. A DEBUG line, however, is conditionally-compiled SOURCE, and
            // COPY/REPLACE (text manipulation) runs before the debugging-mode determination — so
            // its content DOES participate. The normalizer renders fixed-form comment lines as
            // "*> …" and debug lines as "*> DEBUG: …"; here we skip only the "*> DEBUG:" prefix
            // (tokenizing the content) but drop ordinary comment lines whole.
            if (c == '*' && i + 1 < n && text[i + 1] == '>')
            {
                const string debugPrefix = "*> DEBUG:";
                if (i + debugPrefix.Length <= n &&
                    string.CompareOrdinal(text, i, debugPrefix, 0, debugPrefix.Length) == 0)
                {
                    i += debugPrefix.Length;
                    continue;
                }
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

    /// <summary>The recursive COPY-only expansion behind <see cref="Process"/> (the legacy compiler's path): the ONE
    /// one-level expander, <see cref="ExpandCopiesOneLevel"/>, fed with itself as the copybook expander.</summary>
    private string ExpandCopyStatements(string text, HashSet<string> alreadyIncluded, int depth)
        => ExpandCopiesOneLevel(text, alreadyIncluded, depth, (copybook, d) => ExpandCopyStatements(copybook, alreadyIncluded, d));

    /// <summary>The disposition of one resolved COPY statement.</summary>
    internal enum CopyOutcome { Found, NotFound, Circular }

    /// <summary>The result of resolving ONE COPY statement (non-recursive): the text to splice and, when
    /// <see cref="CopyOutcome.Found"/>, the copybook's path (so the caller removes it from the include set after
    /// recursing). <see cref="Text"/> is the copybook's NormalizeCopybook+ApplyReplacements text (Found) or the
    /// comment fallback (NotFound/Circular).</summary>
    /// <param name="Mapped">The incorporated library text with its origins (the copybook's path and physical lines,
    /// after normalization and COPY … REPLACING — kb/Work PB82); null for the not-found / circular comment lines.</param>
    internal readonly record struct OneCopyResult(CopyOutcome Outcome, string Text, string? CopybookPath, MappedText? Mapped = null);

    /// <summary>Parse and resolve ONE COPY statement whose keyword is at <paramref name="copyIdx"/> — read the
    /// text-name + optional IN/OF qualifier + REPLACING (advancing <paramref name="afterCopy"/> past the
    /// terminating period), find the copybook, and return its NormalizeCopybook+ApplyReplacements text (NOT
    /// recursively expanded — the caller recurses). Shared by <see cref="ExpandCopyStatements"/> (legacy path) and
    /// <see cref="ExpandCopiesOneLevel"/> (the merged CC+COPY driver), so the two never diverge.</summary>
    internal OneCopyResult ResolveOneCopy(MappedText mapped, int copyIdx, HashSet<string> alreadyIncluded, out int afterCopy)
    {
        string text = mapped.Text;
        SourceOrigin at = mapped.OriginAt(copyIdx);   // the COPY statement's SOURCE origin (kb/Work PB82)
        afterCopy = copyIdx + "COPY".Length;
        SkipWhitespace(text, ref afterCopy);

        string libraryName = ReadTextNameOrLiteral(text, ref afterCopy);
        SkipWhitespace(text, ref afterCopy);

        // Optional library qualifier: COPY text-name (IN | OF) library-name (ISO §7.2.3).
        string? libraryQualifier = null;
        if (afterCopy < text.Length && (MatchWord(text, afterCopy, "IN") || MatchWord(text, afterCopy, "OF")))
        {
            afterCopy += 2;
            SkipWhitespace(text, ref afterCopy);
            libraryQualifier = ReadTextNameOrLiteral(text, ref afterCopy);
            SkipWhitespace(text, ref afterCopy);
        }

        var replacements = new List<(string from, string to, ReplaceKind kind)>();
        if (afterCopy < text.Length && MatchWord(text, afterCopy, "REPLACING"))
        {
            afterCopy += "REPLACING".Length;
            SkipWhitespace(text, ref afterCopy);
            // The VCR-row-4 gate rides the operand reads (COPY only — REPLACE is not in the E.2 removal).
            ParseReplacements(text, ref afterCopy, replacements, p => OnNonPseudoTextOperand(mapped, p));
        }

        while (afterCopy < text.Length && text[afterCopy] != '.')
            afterCopy++;
        if (afterCopy < text.Length) afterCopy++;

        string? copybookPath = FindCopybook(libraryName, libraryQualifier);
        if (copybookPath == null)
        {
            // ISO §7.2.3.4 GR 2: library text shall be available. Hard error under named-strict dialects;
            // Default/--nist keep the lenient comment fallback (NIST safe).
            if (_strict)
                Report(DiagnosticDescriptors.CBL3620, at,
                    libraryName, string.Join("; ", _searchPaths));
            return new OneCopyResult(CopyOutcome.NotFound, $"*> COPY {libraryName} — copybook not found", null);
        }
        if (!alreadyIncluded.Add(copybookPath))
        {
            // ISO §7.2.3.3 SR 1: a COPY shall not directly or indirectly include itself.
            Report(DiagnosticDescriptors.CBL3621, at, libraryName);
            return new OneCopyResult(CopyOutcome.Circular, $"*> COPY {libraryName} — circular include skipped", null);
        }

        // Library text is itself in reference (fixed) format — normalize to free form so inserted lines align in
        // the program's source area; then COPY … REPLACING (same text-word matching as REPLACE, ISO §7.2.4).
        var normalizedMapped = NormalizeCopybookMapped(File.ReadAllText(copybookPath), copybookPath);
        string normalized = normalizedMapped.Text;
        // §7.2.3.4 GR10 (kb/Work R34): "If the REPLACING phrase is specified, the library text shall not
        // contain a COPY statement" — GR12 permits nesting only WITHOUT replacing. Before this check the
        // caller recursed into the spliced text OUTSIDE the replacement scope, so the illegal combination
        // produced arbitrary partial text and a misleading downstream undefined-reference on whatever name
        // failed to materialize (GnuCOBOL's recursive-replacement EXTENSION accepts this shape; ISO does
        // not). Detection uses the SAME FindCopyKeyword the expander splices by, so the report and the
        // recursion can never disagree about what counts as a COPY statement. Expansion continues after the
        // report — the diagnostic is the verdict; the splice keeps the downstream parse coherent.
        if (replacements.Count > 0 && FindCopyKeyword(normalized, 0) >= 0)
            _diagnostics?.ReportError(Editions.Diagnostics.DiagnosticCatalog.CopyReplacingNestedCopy.Code,
                $"COPY {libraryName} REPLACING: the library text contains a COPY statement — ISO §7.2.3.4 "
                + "GR10 forbids the combination (\"If the REPLACING phrase is specified, the library text "
                + "shall not contain a COPY statement\"); nesting is permitted only without REPLACING "
                + "(GR12). Flatten the copybook, or drop the REPLACING phrase.",
                at.ToLocation(), default);
        var copybookMapped = ApplyReplacements(normalizedMapped, replacements);
        return new OneCopyResult(CopyOutcome.Found, copybookMapped.Text, copybookPath, copybookMapped);
    }

    /// <summary>Expand every COPY statement in <paramref name="text"/> ONE level (no recursion into the copybook):
    /// for each found copybook, its resolved text is handed to <paramref name="expandCopybook"/> (the merged
    /// CC+COPY driver, which processes the copybook's own directives AND its nested COPY), and the result is
    /// spliced with the same blank-line framing as <see cref="ExpandCopyStatements"/>. This is the COPY half of the
    /// interleaved text-manipulation driver (ISO §7.2.1) — the CC half drives, calling this only on emitting-branch
    /// text so an omitted-branch COPY is never expanded (design SSOT <c>DESIGN-cc-in-copy.md</c>).</summary>
    internal string ExpandCopiesOneLevel(string text, HashSet<string> alreadyIncluded, int depth,
        Func<string, int, string> expandCopybook)
        => ExpandCopiesOneLevel(MappedText.Identity(text, _sourceName), alreadyIncluded, depth,
            (m, d) => MappedText.Identity(expandCopybook(m.Text, d), m.Lines[0].File)).Text;

    /// <summary>The MAPPED one-level expansion (kb/Work PB82): the text before a COPY keeps its origins, the
    /// incorporated copybook's lines carry the copybook's, and the framing newlines belong to the COPY statement's
    /// own line — so a diagnostic or EXCEPTION-LOCATION inside copied text names the copybook file and line, and
    /// one after the COPY names the main source's own line, not the resultant ordinal.</summary>
    internal MappedText ExpandCopiesOneLevel(MappedText mapped, HashSet<string> alreadyIncluded, int depth,
        Func<MappedText, int, MappedText> expandCopybook)
    {
        string text = mapped.Text;
        if (depth > MaxCopyDepth)
        {
            Report(DiagnosticDescriptors.CBL3622, mapped.OriginAt(0), MaxCopyDepth);
            return mapped;
        }

        var w = new OriginWriter();
        int pos = 0;
        while (pos < text.Length)
        {
            int copyIdx = FindCopyKeyword(text, pos);
            if (copyIdx < 0)
            {
                w.AppendSlice(mapped, pos, text.Length - pos);
                break;
            }
            w.AppendSlice(mapped, pos, copyIdx - pos);
            SourceOrigin copyLine = mapped.OriginAt(copyIdx);

            var one = ResolveOneCopy(mapped, copyIdx, alreadyIncluded, out int afterCopy);
            if (one.Outcome == CopyOutcome.Found)
            {
                var processed = expandCopybook(one.Mapped!, depth + 1);   // CC + nested COPY on the copybook
                w.NewLine(copyLine);
                w.AppendMapped(processed);
                w.NewLine(copyLine);
                alreadyIncluded.Remove(one.CopybookPath!);
            }
            else
            {
                w.Append(one.Text.AsSpan(), copyLine);
                w.NewLine(copyLine);
            }
            pos = afterCopy;
        }
        return w.Finish();
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
    private static string NormalizeCopybook(string text) => NormalizeCopybookMapped(text, "<copybook>").Text;

    /// <summary>The MAPPED copybook normalization (kb/Work PB82) — the ONE implementation: the free-form library text
    /// with, per line, the copybook's path and physical line (a fixed-form member's continuation joins are tracked
    /// exactly as the main source's are).</summary>
    private static MappedText NormalizeCopybookMapped(string text, string copybookPath)
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
            ? ReferenceFormatProcessor.ConvertFixedToFreeMapped(text, copybookPath)
            : ReferenceFormatProcessor.NormalizeToFreeFormMapped(text, dialectLevel: 85, permissive: false, diagnostics: null, copybookPath);
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

    /// <summary>
    /// Read a COPY text-name or library-name: either a quoted alphanumeric literal (ISO §7.2.3 — the
    /// <c>literal-1</c>/<c>literal-2</c> alternative) whose surrounding quotes are stripped (a doubled embedded
    /// quote collapses to one), or an ordinary unquoted COBOL word. <c>ReadWord</c> alone stopped at the opening
    /// quote and returned an empty name, so <c>COPY "MYBOOK"</c> was reported "not found".
    /// </summary>
    private static string ReadTextNameOrLiteral(string text, ref int pos)
    {
        if (pos < text.Length && (text[pos] == '"' || text[pos] == '\''))
        {
            char quote = text[pos];
            pos++; // opening quote
            var sb = new System.Text.StringBuilder();
            while (pos < text.Length)
            {
                if (text[pos] == quote)
                {
                    if (pos + 1 < text.Length && text[pos + 1] == quote) { sb.Append(quote); pos += 2; continue; }
                    pos++; // closing quote
                    break;
                }
                sb.Append(text[pos]);
                pos++;
            }
            return sb.ToString();
        }
        return ReadWord(text, ref pos);
    }

    private static void ParseReplacements(string text, ref int pos,
        List<(string from, string to, ReplaceKind kind)> replacements, Action<int>? onNonPseudoText = null)
    {
        while (pos < text.Length && text[pos] != '.')
        {
            SkipWhitespace(text, ref pos);
            if (pos >= text.Length || text[pos] == '.') break;

            // Optional LEADING / TRAILING phrase → partial-word substitution (ISO §7.2.3.4 GR 9 b).
            ReplaceKind kind = ReplaceKind.Whole;
            if (MatchWord(text, pos, "LEADING"))
            { pos += "LEADING".Length; SkipWhitespace(text, ref pos); kind = ReplaceKind.Leading; }
            else if (MatchWord(text, pos, "TRAILING"))
            { pos += "TRAILING".Length; SkipWhitespace(text, ref pos); kind = ReplaceKind.Trailing; }

            NoteNonPseudoText(text, pos, onNonPseudoText);
            string from = ReadReplaceOperand(text, ref pos);
            SkipWhitespace(text, ref pos);

            if (MatchWord(text, pos, "BY"))
            {
                pos += "BY".Length;
                SkipWhitespace(text, ref pos);
            }

            NoteNonPseudoText(text, pos, onNonPseudoText);
            string to = ReadReplaceOperand(text, ref pos);
            replacements.Add((from, to, kind));
        }
    }

    /// <summary>Report an operand about to be read that is NOT <c>==pseudo-text==</c> (the identifier /
    /// literal / word forms) to <paramref name="onNonPseudoText"/> — the COPY REPLACING call site's VCR-row-4
    /// gate (removed 2023); the REPLACE statement's caller passes null (REPLACE is pseudo-text-only surface
    /// and not part of the E.2 removal).</summary>
    private static void NoteNonPseudoText(string text, int pos, Action<int>? onNonPseudoText)
    {
        if (onNonPseudoText is not null
            && !(pos < text.Length - 1 && text[pos] == '=' && text[pos + 1] == '='))
            onNonPseudoText(pos);
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

    private string? FindCopybook(string textName, string? libraryName = null)
    {
        // COPY text-name OF/IN library-name selects the copy library (ISO §7.2.3). A library
        // name is resolved to a same-named subdirectory of a search path, so the same text-name
        // can resolve to different text in different libraries. If the qualified library has no
        // such member, fall back to the unqualified search (a single default library).
        if (!string.IsNullOrEmpty(libraryName))
        {
            foreach (var searchPath in _searchPaths)
            {
                string libDir = Path.Combine(searchPath, libraryName);
                if (!Directory.Exists(libDir)) continue;
                foreach (var ext in CopybookExtensions)
                {
                    string fullPath = Path.Combine(libDir, textName + ext);
                    if (File.Exists(fullPath))
                        return fullPath;
                }
            }
        }

        foreach (var searchPath in _searchPaths)
        {
            foreach (var ext in CopybookExtensions)
            {
                string fullPath = Path.Combine(searchPath, textName + ext);
                if (File.Exists(fullPath))
                    return fullPath;
            }
        }
        return null;
    }
}
