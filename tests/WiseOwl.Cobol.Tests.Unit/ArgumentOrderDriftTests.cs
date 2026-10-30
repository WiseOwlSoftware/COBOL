// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ EVERY FUNCTION-IDENTIFIER'S ARGUMENT LIST IS EVALUATED LEFT TO RIGHT THROUGH ONE WINDOW (kb/Work PB1423,
/// CLAUDE.md rule 5).
/// <para>ISO §8.4.3.2.4 GR2: "its arguments are evaluated individually in the order specified in the list of
/// arguments, from left to right". A user-defined function activation is hoisted as a statement pre-op, so an
/// earlier argument that merely READS state was rendered after a later argument's activation had changed it
/// (<c>FUNCTION SUM(A, FUNCTION BUMP(A))</c> summed the changed A). The repair is one mechanism —
/// <c>ArgumentOrder.Window</c> — opened around an argument list by the ONE intrinsic call point
/// (<c>IntrinsicBinder.BindIntrinsicCore</c>) and by the ONE user-defined activation (<c>UdfBinder.UdfActivate</c>),
/// fed by the ONE argument binder (<c>IntrinsicBinder.BindArgOperand</c>). A function kind added tomorrow that binds
/// its arguments through <c>BindArgOperand</c> inside either entry is ordered automatically; one that binds them
/// anywhere else is what this test refuses.</para>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class ArgumentOrderDriftTests
{
    private static readonly string CompilerDir = TestRepo.Src("WiseOwl.Cobol.Compiler");

    /// <summary>The file's code with every comment removed — prose naming a method is not a call to it.</summary>
    private static string CodeOf(string path)
    {
        string text = File.ReadAllText(path);
        text = Regex.Replace(text, @"/\*.*?\*/", "", RegexOptions.Singleline);
        return Regex.Replace(text, @"//[^\r\n]*", "");
    }

    private static string Verb(string file) =>
        CodeOf(Path.Combine(CompilerDir, "Binding", "Procedure", "Verbs", file));

    private static System.Collections.Generic.IEnumerable<string> CompilerSources() =>
        Directory.EnumerateFiles(CompilerDir, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar)
                        && !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar));

    [Fact]
    public void TheArgumentBinder_RecordsIntoTheOpenWindow()
    {
        string code = Verb("IntrinsicBinder.cs");
        Assert.True(Regex.IsMatch(code, @"BindArgOperand\([^)]*\)\s*\{[^}]*ArgOrder\.Record\("),
            "IntrinsicBinder.BindArgOperand no longer records the operand it bound into ArgumentOrder — an argument "
            + "would be invisible to the §8.4.3.2.4 GR2 left-to-right settlement.");
    }

    [Fact]
    public void TheIntrinsicCallPoint_OpensAndSettlesTheWindow()
    {
        string code = Verb("IntrinsicBinder.cs");
        Assert.Contains("ArgOrder.Open()", code);
        Assert.Contains(".Settle(", code);
    }

    [Fact]
    public void TheUserFunctionActivation_OpensAndFreezesTheWindow()
    {
        string code = Verb("UdfBinder.cs");
        Assert.Contains("ArgOrder.Open()", code);
        Assert.Contains(".Freeze(", code);
    }

    /// <summary>⛔ A BOUND CALL'S TYPE IS READ OFF THE NODE, NEVER RE-DERIVED FROM ITS ARGUMENTS (kb/Work PB4036).
    /// <c>Settle</c> swaps a frozen argument for the compiler temporary that holds it, and an arithmetic expression held
    /// in a scale-0 temporary is an integer data item where the expression was not, so a reader that re-resolves the
    /// type from <c>call.Args</c> after the swap answers a different type for the same call
    /// (<c>FUNCTION MAX (A * B, 5)</c> Numeric, <c>FUNCTION MAX (A * B, FUNCTION UDF (A))</c> Integer). The type is
    /// <c>BoundIntrinsicCall.ResultType</c>, asked once when the call is built from its arguments as written.</summary>
    [Fact]
    public void ACallsType_IsReadFromTheNode_NotResolvedAgainFromItsSettledArguments()
    {
        var rereaders = CompilerSources()
            .Where(f => Regex.IsMatch(CodeOf(f), @"\bResolve\(\s*\w+\.Sig\s*,\s*\w+\.Args\s*\)"))
            .Select(Path.GetFileName)
            .ToList();
        Assert.True(rereaders.Count == 0,
            $"{string.Join(", ", rereaders)} resolves a bound call's type from call.Args again — after ArgumentOrder.Settle "
            + "swapped a frozen argument for its temporary that is a different type from the call as written; read "
            + "BoundIntrinsicCall.ResultType (kb/Work PB4036).");

        string settle = CodeOf(Path.Combine(CompilerDir, "Binding", "Procedure", "ArgumentOrder.cs"));
        Assert.Matches(@"call with \{ Args = ", settle);
    }

    [Fact]
    public void NoOtherBinder_BindsFunctionArgumentsOrOpensAWindow()
    {
        var openers = CompilerSources()
            .Where(f => Path.GetFileName(f) is not ("IntrinsicBinder.cs" or "UdfBinder.cs" or "ArgumentOrder.cs"))
            .Where(f => Regex.IsMatch(CodeOf(f), @"\.BindArgOperand\(|ArgOrder\.Open\(\)"))
            .Select(Path.GetFileName)
            .ToList();
        Assert.True(openers.Count == 0,
            $"{string.Join(", ", openers)} binds function arguments or opens an ArgumentOrder window outside the two "
            + "entries that settle it — its arguments would evaluate in registration order, not left to right "
            + "(ISO §8.4.3.2.4 GR2; kb/Work PB1423).");
    }
}
