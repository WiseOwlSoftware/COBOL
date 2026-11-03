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
    /// written; the first failure stops the run and is returned as the group's backend error.</summary>
    public static (IReadOnlyList<string> Files, IReadOnlyList<string> Errors) Write(ExpansionPlacements expansions, CompilerDriver.Options options, string outputDll)
    {
        var files = new List<string>();
        var references = new List<string>(options.ReferencePaths ?? []);
        foreach (var (symbol, placement) in expansions.ToWrite)
        {
            var origin = symbol is OoClassSymbol c ? c.Expansion! : ((OoInterfaceSymbol)symbol).Expansion!;
            var state = origin.Skeleton.State;
            string source = state.CobolWordsMap + state.DirectiveFolds + state.CompilationVariables + FreeForm(origin.Text) + "\n";
            string temp = Path.Combine(Path.GetTempPath(), $"expansion-{Guid.NewGuid():N}.cob");
            File.WriteAllText(temp, source);
            try
            {
                string target = placement.From!.Path;
                var result = CompilerDriver.Compile(new CompilerDriver.Options(
                    temp, target, NistTestName: null, DialectLevel: state.Edition, CopyPaths: null, Permissive: state.Permissive,
                    CheckOnly: false, SignEncoding: state.SignEncoding, SourceFormat: Frontend.Preprocessor.InitialReferenceFormat.Free,
                    FlagExtensions: state.FlagExtensions, NumericDisplay: RepositoryVocabulary.FromPersisted(state.NumericDisplay),
                    RepositoryUpdate: options.RepositoryUpdate, RepositoryPaths: options.RepositoryPaths ?? [Path.GetDirectoryName(Path.GetFullPath(outputDll))!],
                    ReferencePaths: references, RepositoryCheck: false) { PublishAs = placement.Identity });
                if (!result.Success)
                    return (files, [$"the expansion assembly '{target}' (ISO §9.3.12; DESIGN-external-repository §10.5):", .. result.Errors]);
                files.AddRange(result.OutputFiles);
                references.Add(target);
            }
            finally
            {
                File.Delete(temp);
            }
        }
        return (files, []);
    }

    /// <summary>The text as free-form lines of at most 200 positions (ISO §6.1 3) a) allows 255): a line break stands where the text had
    /// a space, and a space is only ever one separator, so the lines read as the text does.</summary>
    private static string FreeForm(string text)
    {
        var lines = new System.Text.StringBuilder();
        int column = 0;
        foreach (string word in text.Split(' '))
        {
            if (column > 0 && column + 1 + word.Length > 200) { lines.Append('\n'); column = 0; }
            else if (column > 0) { lines.Append(' '); column++; }
            lines.Append(word);
            column += word.Length;
        }
        return lines.ToString();
    }
}
