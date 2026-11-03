// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Binding.Repository;
using WiseOwl.Cobol.Compiler.Oo;

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
    /// <summary>Compile every expansion <paramref name="expansions"/> places in an assembly of its own, dependency order, and return the files
    /// written; the first failure stops the run and is returned as the group's backend error, with the generated C# of the compilation that
    /// failed. A diagnostic of such a compilation names where the expansion was asked for (the EXPANDS entry's line, as an expansion made
    /// in the group is positioned) and never the throw-away file its text was compiled from. Its warnings are those of the skeleton's
    /// text, which the group that wrote the skeleton already reported, so they are not repeated for every group that expands it.</summary>
    public static (IReadOnlyList<string> Files, IReadOnlyList<string> Errors, string? GeneratedCsPath) Write(ExpansionPlacements expansions,
        CompilerDriver.Options options, string outputDll)
    {
        var files = new List<string>();
        var references = new List<string>(options.ReferencePaths ?? []);
        foreach (var (text, state, placement, requestedAtLine) in expansions.ToWrite)
        {
            string source = state.CobolWordsMap + state.DirectiveFolds + state.CompilationVariables + FreeForm(text) + "\n";
            string temp = Path.Combine(Path.GetTempPath(), $"expansion-{Guid.NewGuid():N}.cob");
            File.WriteAllText(temp, source);
            try
            {
                string target = placement.HoldingFile!;
                var result = CompilerDriver.Compile(new CompilerDriver.Options(
                    temp, target, NistTestName: null, DialectLevel: state.Edition, CopyPaths: null, Permissive: state.Permissive,
                    CheckOnly: false, SignEncoding: state.SignEncoding, SourceFormat: Frontend.Preprocessor.InitialReferenceFormat.Free,
                    FlagExtensions: state.FlagExtensions, NumericDisplay: RepositoryVocabulary.FromPersisted(state.NumericDisplay),
                    RepositoryUpdate: options.RepositoryUpdate, RepositoryPaths: options.RepositoryPaths ?? [Path.GetDirectoryName(Path.GetFullPath(outputDll))!],
                    ReferencePaths: references, RepositoryCheck: false) { PublishAs = placement.Identity });
                if (!result.Success)
                {
                    string where = $"the expansion '{Path.GetFileNameWithoutExtension(target)}' asked for at {Path.GetFileName(options.SourcePath)} line {requestedAtLine}, expansion text at ";
                    return (files, [$"the expansion assembly '{target}' (ISO §9.3.12; DESIGN-external-repository §10.5):",
                        .. result.Errors.Select(e => e.Replace(temp, where, StringComparison.Ordinal))], result.GeneratedCsPath);
                }
                files.AddRange(result.OutputFiles);
                references.Add(target);
            }
            finally
            {
                File.Delete(temp);
            }
        }
        return (files, [], null);
    }

    /// <summary>The most positions of a line this writes (ISO §6.1 3) a) allows 255): room is left for a floating literal continuation
    /// indicator and its quotation symbol.</summary>
    private const int LineLimit = 200;

    /// <summary>The text as free-form lines of at most <see cref="LineLimit"/> positions that read as the text does. A line break stands
    /// where the text had a space BETWEEN words — a space inside an alphanumeric, boolean or national literal is the literal's content, so
    /// the text is split into words by the literals' delimiters, not by every space — and a literal too long for the rest of a line is
    /// continued by the floating literal continuation indicator (ISO §6.2.3.1: the quotation symbol and hyphen that end the line;
    /// §6.5 8): the continuation line begins with the matching quotation symbol and the literal goes on after it).</summary>
    internal static string FreeForm(string text)
    {
        var lines = new System.Text.StringBuilder();
        int column = 0;
        foreach (string word in WordsOf(text))
        {
            // A word that fits a line on its own never straddles two; a longer one (a long literal) starts where it is and is continued.
            if (column > 0 && column + 1 + Math.Min(word.Length, LineLimit / 2) > LineLimit) { lines.Append('\n'); column = 0; }
            else if (column > 0) { lines.Append(' '); column++; }
            char quote = '\0';
            for (int i = 0; i < word.Length; i++)
            {
                char ch = word[i];
                if (quote == '\0') { if (ch is '"' or '\'') quote = ch; }
                else if (ch == quote && !(i + 1 < word.Length && word[i + 1] == quote)) quote = '\0';   // the closing symbol
                else
                {
                    // One content unit of the literal: a character, or a doubled quotation symbol (one character of the literal).
                    int unit = ch == quote ? 2 : 1;
                    if (column + unit > LineLimit - 2)
                    {
                        lines.Append(quote).Append('-').Append('\n').Append(quote);
                        column = 1;
                    }
                    lines.Append(word, i, unit);
                    column += unit;
                    i += unit - 1;
                    continue;
                }
                lines.Append(ch);
                column++;
            }
        }
        return lines.ToString();
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
