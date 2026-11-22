// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ EVERY CLOSE THE PROGRAM EXECUTES, WRITTEN OR IMPLICIT, IS ONE EMITTED SEQUENCE (kb/Work PB5847, PB5261). The standard
/// makes each implicit close a CLOSE statement: the CANCEL's "implicit CLOSE statement without any optional phrases"
/// (§2023:14.9.5.4 GR9; §1985:X.5.3.4 GR10), which an INITIAL program's return also executes (§2023:14.9.18.4 GR2;
/// §1985:X.5.4.4 GR3), and the SORT/MERGE USING and GIVING closes (§2023:14.9.40.4 GR12c, GR15 c)). A CLOSE "causes the
/// value of the I-O status associated with file-name-1 to be updated" (§2023:14.9.6.4 GR4; §1985:VII.4.2.4 GR4). Before
/// PB5847 the CANCEL's close was a second, smaller close (one runtime call per file, no status store), and the FILE STATUS
/// item kept the last statement's '10' where '00' is due. So:
/// <list type="bullet">
/// <item>no code-generator source but <c>SequentialIoEmitter.EmitCloseStatement</c> renders a CLOSE's runtime entry
/// (<c>RuntimeApi.FileClose</c>) or its scope (<c>RuntimeApi.FileBeginClose</c>), and that member stores the status;</item>
/// <item>the program's implicit-close block reaches it through <c>EmitImplicitClose</c>, which alone renders the
/// "that is open" guard (<c>RuntimeApi.FileIsOpen</c>) and passes no USE hook (GR9 / GR10: no USE procedure is
/// executed).</item>
/// </list>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class CloseSequenceDriftTests
{
    private static IEnumerable<(string File, InvocationExpressionSyntax Call)> RuntimeApiCalls(string member) =>
        Directory.EnumerateFiles(TestRepo.Src("WiseOwl.Cobol.Compiler"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                        && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .SelectMany(f => CSharpSyntaxTree.ParseText(File.ReadAllText(f)).GetCompilationUnitRoot()
                .DescendantNodes().OfType<InvocationExpressionSyntax>()
                .Where(call => call.Expression is MemberAccessExpressionSyntax
                {
                    Expression: IdentifierNameSyntax { Identifier.ValueText: "RuntimeApi" },
                    Name.Identifier.ValueText: var name,
                } && name == member)
                .Select(call => (Path.GetFileName(f), call)));

    private readonly record struct Site(string File, string Member);

    private static List<Site> Sites(string member) =>
        RuntimeApiCalls(member)
            .Select(s => new Site(s.File,
                s.Call.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault()?.Identifier.ValueText ?? "<outside a method>"))
            .ToList();

    [Theory]
    [InlineData("FileClose")]
    [InlineData("FileBeginClose")]
    public void OnlyTheOneClose_RendersACloseEntryOrScope(string member)
    {
        var sites = Sites(member);
        Assert.True(sites.Count > 0, $"no code-generator source calls RuntimeApi.{member}; re-point this drift test.");
        var offenders = sites.Where(s => s is not { File: "SequentialIoEmitter.cs", Member: "EmitCloseStatement" }).ToList();
        Assert.True(offenders.Count == 0,
            $"RuntimeApi.{member} is rendered outside SequentialIoEmitter.EmitCloseStatement, THE ONE CLOSE, from: "
            + string.Join(", ", offenders.Select(o => $"{o.File}#{o.Member}"))
            + " — a CLOSE reached another way must go through EmitCloseStatement so it stores the I-O status "
            + "(§2023:14.9.6.4 GR4; kb/Work PB5847).");
    }

    [Fact]
    public void OnlyTheImplicitClose_RendersTheOpenGuard()
    {
        var sites = Sites("FileIsOpen");
        Assert.Equal([new Site("SequentialIoEmitter.cs", "EmitImplicitClose")], sites);
    }

    [Fact]
    public void TheOneClose_StoresTheStatus_AndTheImplicitCloseUsesIt_WithNoUseHook()
    {
        var root = CSharpSyntaxTree.ParseText(File.ReadAllText(
            TestRepo.Src("WiseOwl.Cobol.Compiler", "CodeGen", "Verbs", "SequentialIoEmitter.cs"))).GetCompilationUnitRoot();
        MethodDeclarationSyntax Method(string name) =>
            root.DescendantNodes().OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.ValueText == name);
        static IEnumerable<string> Called(SyntaxNode body) =>
            body.DescendantNodes().OfType<InvocationExpressionSyntax>().Select(c => c.Expression switch
            {
                IdentifierNameSyntax id => id.Identifier.ValueText,
                MemberAccessExpressionSyntax ma => ma.Name.Identifier.ValueText,
                _ => "",
            });

        Assert.Contains("EmitStoreFileStatus", Called(Method("EmitCloseStatement")));

        var implicitClose = Method("EmitImplicitClose");
        var reach = implicitClose.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Single(c => c.Expression is IdentifierNameSyntax { Identifier.ValueText: "EmitCloseStatement" });
        Assert.Equal("null", reach.ArgumentList.Arguments.Last().Expression.ToString());

        var program = CSharpSyntaxTree.ParseText(File.ReadAllText(
            TestRepo.Src("WiseOwl.Cobol.Compiler", "CodeGen", "ProgramEmitter.cs"))).GetCompilationUnitRoot();
        var emitProgramClass = program.DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Single(m => m.Identifier.ValueText == "EmitProgramClass");
        Assert.Contains("EmitImplicitClose", Called(emitProgramClass));
    }
}
