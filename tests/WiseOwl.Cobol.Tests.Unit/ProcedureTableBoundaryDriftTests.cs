// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ THE PROCEDURE TABLE, THE USE SCOPE AND THE DEBUG SUBJECTS EACH HAVE ONE WRITER (kb/Work PB2345;
/// docs/rearchitecture/DESIGN-architecture-review.md §8.3, census R0-0050). <c>ProcedureTableBuilder</c> holds the pc space, the
/// sections and the procedure-name resolution over them, and records each declarative section's <c>BoundDeclarative</c> scope;
/// <c>UseStatementBinder</c> binds a USE statement's trigger scope with the per-procedure-division repeat screens of ISO
/// §2023:14.9.49.3 SR7, SR8, SR9 and SR14; <c>DebugDeclarativeResolver</c> resolves the X3.23-1985 USE FOR DEBUGGING operands to
/// trigger subjects and targets. The seam the rest of the binder reads, the table's pc index to range, is unchanged. This test
/// keeps a second writer of any of the three from growing back:
/// <list type="bullet">
/// <item>a <c>BoundDeclarative</c> is made only in <c>ProcedureTableBuilder.cs</c>;</item>
/// <item>the USE repeat registers are declared only in <c>UseStatementBinder.cs</c>;</item>
/// <item>a <c>BoundDebugSubject</c> or <c>BoundDebugTarget</c> is made only in <c>DebugDeclarativeResolver.cs</c>.</item>
/// </list>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class ProcedureTableBoundaryDriftTests
{
    private static IEnumerable<(string File, string Text)> CompilerSources() =>
        Directory.EnumerateFiles(TestRepo.Src("WiseOwl.Cobol.Compiler"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .Select(f => (Path.GetRelativePath(TestRepo.Root, f).Replace('\\', '/'), File.ReadAllText(f)));

    [Theory]
    [InlineData(@"\bnew BoundDeclarative\(", "/Binding/Procedure/ProcedureTableBuilder.cs")]
    [InlineData(@"ConstructOperandRegister<[^>]*(?:<[^>]*>)?[^>]*>\s+_use(?:Modes|Files|ReportGroups|EcPairs)\b", "/Binding/Procedure/UseStatementBinder.cs")]
    [InlineData(@"\bnew BoundDebugSubject\(", "/Binding/Procedure/DebugDeclarativeResolver.cs")]
    [InlineData(@"\bnew BoundDebugTarget\(", "/Binding/Procedure/DebugDeclarativeResolver.cs")]
    public void TheWriter_HasOneHome(string pattern, string home)
    {
        var rx = new Regex(pattern);
        var sites = CompilerSources().Where(s => rx.IsMatch(s.Text)).Select(s => s.File).ToList();
        Assert.True(sites.Count > 0, $"the scan found no `{pattern}` — the pattern no longer matches the code");
        Assert.True(sites.All(f => f.EndsWith(home, StringComparison.Ordinal)),
            $"`{pattern}` is written outside {home} (kb/Work PB2345): " + string.Join(", ", sites));
    }

    [Fact]
    public void TheTable_AsksItsTwoCollaborators()
    {
        string table = File.ReadAllText(TestRepo.Src("WiseOwl.Cobol.Compiler", "Binding", "Procedure", "ProcedureTableBuilder.cs"));
        Assert.Contains("scope = _use.DeclBindUse(use, name);", table, StringComparison.Ordinal);
        Assert.Contains("new DebugDeclarativeResolver(ctx, this, _debugSubjects, _debugTargets, _debugContentsWidth)", table, StringComparison.Ordinal);
    }
}
