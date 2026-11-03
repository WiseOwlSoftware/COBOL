// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Binding.Repository;
using WiseOwl.Cobol.Compiler.Oo;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Frontend.Common;

namespace WiseOwl.Cobol;

/// <summary>
/// ⛔ PLACEMENT (a): AN EXPANSION IN AN ASSEMBLY OF ITS OWN (ISO §9.3.12; DESIGN-external-repository §10.5, §7.2; kb/Work PB2104). An
/// expansion whose code names no class or interface of the expanding group is compiled apart: "as its OWN compilation — the skeleton's
/// recorded source under its recorded state, the referenced assemblies as references — into <c>&lt;externalized name&gt;.dll</c> beside
/// the group's output". That is exactly what this does: the expansion's text (the skeleton's tokens with the header renamed and each
/// formal replaced by its actual, <see cref="ExpansionOrigin.Text"/>) goes through <see cref="CompilerDriver.Compile"/> as a source of
/// its own, preceded by the directive lines that state its <see cref="CompileState"/>, so the one pipeline binds and emits it under the
/// edition, options, words and directive folds the skeleton was published under — not the expanding group's — and the module it
/// produces carries the expansion's identity and key like any other (<c>[CobolUnit(Kind = Expansion)]</c>).
/// <para>The search space is the group's, plus each expansion written before this one (an actual may itself be an expansion), so the
/// classes the text names are found where the group finds them. The file is deterministic for one text and one search space (§7.6); an
/// existing file of this name and key never reaches here — the placement pass imported it.</para>
/// </summary>
internal static class ExpansionAssemblies
{
    /// <summary>What <see cref="Write"/> produced: the files written, and either the errors of the compilation that failed (with the
    /// generated C# of that compilation, null when it reached none, and <paramref name="Failure"/>, the outcome that compilation
    /// reached, which the group's result takes as its own: an expansion whose assembly the host refuses is OutputNotWritable, not a
    /// backend error; kb/Work PB4502 review R2-1) or none (<paramref name="Failure"/> is then Success), with the warnings of every
    /// compilation that ran of the text of a skeleton read from the repository (one of a skeleton written in the group is the group's to
    /// report, <see cref="Write"/>).</summary>
    public sealed record Written(IReadOnlyList<string> Files, IReadOnlyList<string> Errors, IReadOnlyList<string> Warnings, string? GeneratedCsPath,
        CompilerDriver.Outcome Failure);

    /// <summary>Compile every expansion <paramref name="expansions"/> places in an assembly of its own, dependency order, and return the files
    /// written; the first failure stops the run and is returned as the group's result, under the outcome that compilation reached (`Written.Failure`), with the generated C# of the compilation that
    /// failed. Every diagnostic of such a compilation, error or warning, is reported where the expansion was asked for — the EXPANDS entry's
    /// file, line and column (<see cref="AtTheEntry"/>), where the group's own report prints a diagnostic about the text of an expansion of a
    /// skeleton it read from the repository (<c>DiagnosticRegion.ReportedAt</c>) — and never at the throw-away file its text was compiled from,
    /// which is gone by the time the user reads it (kb/Work PB4457). Who answers for the text's WARNINGS is the region's rule
    /// (<c>DiagnosticRegion</c>; kb/Work PB4469, PB4483): for a skeleton read from the repository this compilation does, under the state the
    /// skeleton was published with, and once it has reported <paramref name="edition"/> withdraws what the group's bind of the same text raised
    /// (<c>EditionContext.WithdrawWarningsOf</c>); for a skeleton written in the group the group does, where the user wrote it, under the same
    /// state, and this compilation's warnings are those facts a second time, so they are not reported. Errors are always reported. A
    /// <c>CheckOnly</c> compilation never gets here and keeps the group's report of the text. They are reported for each expansion made, and
    /// name it (<see cref="DiagnosticRegion.InTheExpansion"/>).</summary>
    public static Written Write(ExpansionPlacements expansions, CompilerDriver.Options options, EditionContext edition)
    {
        var files = new List<string>();
        var warnings = new List<string>();
        var references = new List<string>(options.ReferencePaths ?? []);
        foreach (var (text, state, placement, requestedAt, region) in expansions.ToWrite)
        {
            string source = state.CobolWordsMap + state.DirectiveFolds + state.CompilationVariables + FreeForm(text) + "\n";
            string temp = Path.Combine(Path.GetTempPath(), $"expansion-{Guid.NewGuid():N}.cob");
            File.WriteAllText(temp, source);
            try
            {
                string target = placement.HoldingFile!;
                string name = Path.GetFileNameWithoutExtension(target);
                var result = CompilerDriver.Compile(new CompilerDriver.Options(
                    temp, target, NistTestName: null, DialectLevel: state.Edition, CopyPaths: null, Permissive: state.Permissive,
                    CheckOnly: false, SignEncoding: state.SignEncoding, SourceFormat: Frontend.Preprocessor.InitialReferenceFormat.Free,
                    FlagExtensions: state.FlagExtensions, NumericDisplay: RepositoryVocabulary.FromPersisted(state.NumericDisplay),
                    RepositoryUpdate: options.RepositoryUpdate, RepositoryPaths: options.RepositoryDirectories(),
                    ReferencePaths: references, RepositoryCheck: false) { PublishAs = placement.Identity });
                // Who answers for the text's warnings (DiagnosticRegion; kb/Work PB4469, PB4483): the text of a skeleton read from the
                // repository is this compilation's, reported at the entry, and the group's bind of it withdraws what it raised; the text
                // of a skeleton written in the group is the user's own, which the group has reported where the user wrote it, under the
                // same state, so this compilation's report of it would be the same facts a second time.
                if (region.IsRepositoryText)
                {
                    warnings.AddRange(result.Warnings.Select(w => AtTheEntry(w, temp, name, requestedAt)));
                    edition.WithdrawWarningsOf(region);
                }
                if (!result.Success)
                    return new(files, [$"the expansion assembly '{target}' (ISO §9.3.12; DESIGN-external-repository §10.5):",
                        .. result.Errors.Select(e => AtTheEntry(e, temp, name, requestedAt))], warnings, result.GeneratedCsPath, result.Status);
                files.AddRange(result.OutputFiles);
                references.Add(target);
            }
            finally
            {
                File.Delete(temp);
            }
        }
        return new(files, [], warnings, null, CompilerDriver.Outcome.Success);
    }

    /// <summary>A diagnostic line of the compilation of the text of expansion <paramref name="expansion"/>, reported where the expansion
    /// was asked for: <paramref name="requestedAt"/> replaces the position in <paramref name="temp"/> (a line of text the user never wrote,
    /// in a file that no longer exists), a diagnostic that names no position takes it, and the expansion is named after the message so the
    /// text it concerns can be found. Any other line (a header, a Roslyn diagnostic of the generated C#) is returned as it was, except
    /// that a remaining mention of <paramref name="temp"/> becomes the expansion's name.</summary>
    internal static string AtTheEntry(string diagnostic, string temp, string expansion, SourceLocation requestedAt)
    {
        string named = DiagnosticRegion.InTheExpansion(expansion);
        if (diagnostic.StartsWith(temp + "(", StringComparison.Ordinal) && diagnostic.IndexOf("): ", temp.Length, StringComparison.Ordinal) is var end and >= 0)
            diagnostic = $"{requestedAt}: {diagnostic[(end + 3)..]} {named}";
        else if (diagnostic.StartsWith("error ", StringComparison.Ordinal) || diagnostic.StartsWith("warning ", StringComparison.Ordinal)
            || diagnostic.StartsWith("info ", StringComparison.Ordinal))
            diagnostic = $"{requestedAt}: {diagnostic} {named}";
        return diagnostic.Replace(temp, $"the text of the expansion '{expansion}'", StringComparison.Ordinal);
    }

    /// <summary>The most positions of a line this writes (ISO §6.1 3) a) allows 255): room is left for a floating literal continuation
    /// indicator and its quotation symbol.</summary>
    private const int LineLimit = 200;

    /// <summary>The text as free-form lines of at most <see cref="LineLimit"/> positions that read as the text does. A line break stands
    /// where the text had a space BETWEEN words — a space inside an alphanumeric, boolean or national literal is the literal's content, so
    /// the text is split into words by the literals' delimiters, not by every space — and a literal too long for the rest of a line is
    /// continued by the floating literal continuation indicator (ISO §6.2.3.1: the quotation symbol and hyphen that end the line;
    /// §6.5 8): the continuation line begins with the matching quotation symbol and the literal goes on after it).
    /// <para>ISO §6.4.2: "At least one alphanumeric character, national character, or hexadecimal digit of the literal content shall be
    /// specified on the continued line and on each continuation line", and "A pair of quotation symbols indicating a single quotation symbol
    /// within a literal shall be specified on the same line". So a continuation indicator never follows the opening delimiter directly: a
    /// continuation line opens with the quotation symbol and then a whole content unit (one character, or a doubled quotation symbol, at
    /// most two positions, which always fits), and a literal whose FIRST content unit does not fit the line it opens on is moved, prefix
    /// letters and all, to a line of its own, where it does (kb/Work PB4456).</para></summary>
    internal static string FreeForm(string text)
    {
        var lines = new System.Text.StringBuilder();
        int column = 0;
        foreach (string word in WordsOf(text))
        {
            // A word that fits a line on its own never straddles two; a longer one (a long literal) starts where it is, which is at
            // most halfway along a line, and is continued.
            if (column > 0 && column + 1 + Math.Min(word.Length, LineLimit / 2) > LineLimit) { lines.Append('\n'); column = 0; }
            else if (column > 0) { lines.Append(' '); column++; }
            char quote = '\0';
            int literalStart = 0;   // where the open literal's prefix letters (else its opening delimiter) begin in lines
            int unitsOnLine = 0;    // content units of the open literal on the line being written
            for (int i = 0; i < word.Length; i++)
            {
                char ch = word[i];
                if (quote == '\0')
                {
                    if (ch is '"' or '\'')
                    {
                        quote = ch;
                        literalStart = lines.Length - PrefixLengthBefore(word, i);
                        unitsOnLine = 0;
                    }
                }
                else if (ch == quote && !(i + 1 < word.Length && word[i + 1] == quote)) quote = '\0';   // the closing symbol
                else
                {
                    // One content unit of the literal: a character, or a doubled quotation symbol (one character of the literal).
                    int unit = ch == quote ? 2 : 1;
                    if (column + unit > LineLimit - 2)
                    {
                        if (unitsOnLine == 0)
                        {
                            // The opening line would hold no content before the indicator: the literal starts a line of its own instead.
                            int written = lines.Length - literalStart;
                            if (literalStart > 0 && lines[literalStart - 1] == ' ') lines[literalStart - 1] = '\n';
                            else if (literalStart > 0 && lines[literalStart - 1] != '\n') lines.Insert(literalStart, '\n');
                            column = written;
                        }
                        else
                        {
                            lines.Append(quote).Append('-').Append('\n').Append(quote);
                            column = 1;
                            unitsOnLine = 0;
                        }
                    }
                    lines.Append(word, i, unit);
                    column += unit;
                    unitsOnLine++;
                    i += unit - 1;
                    continue;
                }
                lines.Append(ch);
                column++;
            }
        }
        return lines.ToString();
    }

    /// <summary>How many characters just before the opening delimiter at <paramref name="at"/> of <paramref name="word"/> are the
    /// literal's prefix letters — <c>N</c>, <c>B</c>, <c>X</c>, <c>NX</c> or <c>BX</c> (ISO §8.3.3.2, §8.3.3.4, §8.3.3.5) — and not the
    /// end of a longer word.</summary>
    private static int PrefixLengthBefore(string word, int at)
    {
        int length = 0;
        if (at >= 1 && "NBXnbx".Contains(word[at - 1])) length = 1;
        if (length == 1 && at >= 2 && word[at - 1] is 'X' or 'x' && word[at - 2] is 'N' or 'n' or 'B' or 'b') length = 2;
        return length < at && CobolCharacterRepertoire.IsWordCharacter(word[at - length - 1]) ? 0 : length;
    }

    /// <summary>The words of <paramref name="text"/>: the runs of characters between the spaces that lie outside a literal.</summary>
    private static IEnumerable<string> WordsOf(string text)
    {
        int start = 0;
        char quote = '\0';
        for (int i = 0; i < text.Length; i++)
        {
            char ch = text[i];
            if (quote != '\0') { if (ch == quote) quote = '\0'; }   // a doubled symbol closes and reopens, which leaves the state as it was
            else if (ch is '"' or '\'') quote = ch;
            else if (ch == ' ')
            {
                yield return text[start..i];
                start = i + 1;
            }
        }
        yield return text[start..];
    }
}
