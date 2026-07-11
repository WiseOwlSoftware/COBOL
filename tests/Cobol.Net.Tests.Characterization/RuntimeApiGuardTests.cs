// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using Xunit;

namespace CobolNet.Tests.Characterization;

/// <summary>
/// The P7 Step 4b RATCHET (DESIGN-codegen-backend §3): every C# fragment naming a runtime member must route
/// through the <c>nameof</c>-anchored <c>RuntimeApi</c> façade, so a runtime rename breaks ONE file at compile
/// time. Migration is incremental (the phase doc's shrinking-whitelist plan): this test pins each CodeGen
/// file's count of bare <c>Cobol*.</c> member-accesses and FAILS on any INCREASE — new emission goes through
/// <c>RuntimeApi</c>, and Step 9's per-verb rewrites drive the baselines to zero (delete a file's entry once it
/// reaches 0; when the table empties, rewrite this test to forbid-all). Reads files with
/// <c>File.ReadAllText</c>, NOT a grep subprocess — <c>ConditionRenderer.cs</c> historically carried a literal
/// NUL that made it invisible to ripgrep (DEVLOG 788/790).
/// </summary>
public sealed class RuntimeApiGuardTests
{
    /// <summary>Matches a bare runtime member-access fragment. Excluded by construction: the <c>CobolNet.</c>
    /// namespace and the compiler-internal <c>CobolLiteral.</c> codec (not runtime types). Typed compile-time
    /// uses (e.g. <c>CobolRounding</c> enum values) still count — the ratchet tolerates them inside a file's
    /// baseline; the migration decides per file whether they become façade passthroughs.</summary>
    private static readonly Regex Bare = new(@"\bCobol(?!Net\.|Literal\.)[A-Za-z0-9]+\.", RegexOptions.Compiled);

    /// <summary>The baseline: bare-count per CodeGen file at the Step-4b landing (audit census wf_8ace7f29-a1d,
    /// re-counted at commit time). A file NOT listed here must have ZERO bare accesses.</summary>
    private static readonly Dictionary<string, int> Baseline = new(StringComparer.OrdinalIgnoreCase)
    {
        ["CSharpEmitter.cs"] = 98,
        ["Emit/IntrinsicRenderer.cs"] = 52,
        ["CSharpEmitter.Sort.cs"] = 35,
        ["CSharpEmitter.Call.cs"] = 27,
        ["CSharpEmitter.KeyedIo.cs"] = 25,
        ["Emit/NumericRenderer.cs"] = 25,
        ["Emit/FieldEmitter.cs"] = 24,
        ["Emit/ConditionRenderer.cs"] = 21,
        ["CSharpEmitter.Oo.cs"] = 17,
        ["CSharpEmitter.StringUnstring.cs"] = 17,
        ["CSharpEmitter.Inspect.cs"] = 16,
        ["CSharpEmitter.Ptr.cs"] = 9,
        ["Emit/OperandText.cs"] = 9,
        ["Verbs/AcceptDisplayEmitter.cs"] = 9,
        ["CSharpEmitter.ReportWriter.cs"] = 5,
        ["CSharpEmitter.Exceptions.cs"] = 2,
        ["CSharpEmitter.Corresponding.cs"] = 1,
        ["Emit/EmitCore.cs"] = 1,
        ["Roslyn/ReceiverContext.cs"] = 1,
    };

    [Fact]
    public void Bare_runtime_accesses_do_not_increase()
    {
        string codeGen = Path.Combine(RepoRoot(), "src", "Cobol.Net.Compiler", "CodeGen");
        Assert.True(Directory.Exists(codeGen), codeGen);
        var over = new List<string>();
        foreach (string file in Directory.EnumerateFiles(codeGen, "*.cs", SearchOption.AllDirectories))
        {
            string name = Path.GetRelativePath(codeGen, file).Replace('\\', '/');
            if (name is "Roslyn/RuntimeApi.cs") continue;   // the façade itself
            int count = Bare.Matches(File.ReadAllText(file)).Count;
            int allowed = Baseline.GetValueOrDefault(name, 0);
            if (count > allowed)
                over.Add($"{name}: {count} bare Cobol*. accesses (baseline {allowed}) — route new emission through RuntimeApi");
        }
        Assert.True(over.Count == 0, string.Join("\n", over));
    }

    private static string RepoRoot()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !Directory.Exists(Path.Combine(d.FullName, "src", "Cobol.Net.Compiler")))
            d = d.Parent!;
        return d?.FullName ?? throw new InvalidOperationException("repo root not found");
    }
}
