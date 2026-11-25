// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Characterization;

/// <summary>
/// The P7 Step 4b guard, now FORBID-ALL (DESIGN-codegen-backend §3): every C# fragment naming a runtime member routes
/// through the <c>nameof</c>-anchored <c>RuntimeApi</c> façade, so a runtime rename breaks ONE file at compile
/// time. The Step-9-final sweep (DEVLOG 810) drove every Step-9 emitter to ZERO; Step 12 routed
/// <c>IntrinsicRenderer</c> (the static-channel deletion); kb/Work PB2302 (step 1 of the RuntimeApi extract) routed the
/// last three, <c>NumericRenderer</c>, <c>ConditionRenderer</c> and <c>OperandText</c>, and emptied the ratchet's table, so
/// no CodeGen file outside the façade and the anchored writers holds a bare runtime access.
/// <para><b>Counting rule (the Step-9-final recorded refinement):</b> comment lines (<c>//</c>-led) are
/// stripped before matching — a comment can never mis-emit text, and doc-comment <c>cref</c>s are already
/// compile-validated. <c>CobolRounding.</c> and <c>CobolPassMode.</c> member accesses are excluded — in
/// CodeGen those are always TYPED enum arguments (compile-checked); their EMITTED-text forms render
/// exclusively through the façade's <c>RoundingText</c>/<c>PassModeText</c> anchors, so a rename still breaks
/// <c>RuntimeApi.cs</c> at compile time. Reads files with <c>File.ReadAllText</c>, NOT a grep subprocess —
/// <c>ConditionRenderer.cs</c> historically carried a literal NUL that made it invisible to ripgrep
/// (DEVLOG 788/790).</para>
/// </summary>
public sealed class RuntimeApiGuardTests
{
    /// <summary>Matches a bare runtime member-access fragment. Excluded by construction: the <c>WiseOwl.Cobol.</c>
    /// namespace (a dot follows its <c>Cobol</c>, where the pattern needs a letter), the compiler-internal <c>CobolLiteral.</c> codec (not a runtime type), the two typed-enum
    /// runtime types (<c>CobolRounding.</c>/<c>CobolPassMode.</c> — see the counting rule above), and
    /// <c>CobolNames.</c>, the ONE Annex C word fold the emitters CALL at compile time to compare COBOL words (kb/Work
    /// PB1402) — never a fragment of emitted text.</summary>
    private static readonly Regex Bare = new(@"\bCobol(?!Literal\.|Rounding\.|PassMode\.|Names\.)[A-Za-z0-9]+\.", RegexOptions.Compiled);

    /// <summary>The files, besides <c>Roslyn/RuntimeApi.cs</c>, that write a runtime NAME into emitted text, each through
    /// <c>typeof</c>, <c>nameof</c> or a typed constant, so that a runtime rename still breaks that file at compile time —
    /// which is the ratchet's whole purpose. <c>RepositoryAttributeRenderer.cs</c> is the one place the repository
    /// attributes' syntax is written (DESIGN-external-repository §3.9, §12: it took <c>RuntimeApi.ModuleRecordAttribute</c>
    /// over), and the attribute types are not callable members the façade wraps. Adding a file here needs the same reason.</summary>
    private static readonly HashSet<string> AnchoredWriters = new(StringComparer.OrdinalIgnoreCase)
    {
        "RepositoryAttributeRenderer.cs",
    };

    [Fact]
    public void No_codegen_file_holds_a_bare_runtime_access()
    {
        string codeGen = TestRepo.Src("WiseOwl.Cobol.Compiler", "CodeGen");
        Assert.True(Directory.Exists(codeGen), codeGen);
        var over = new List<string>();
        foreach (string file in Directory.EnumerateFiles(codeGen, "*.cs", SearchOption.AllDirectories))
        {
            string name = Path.GetRelativePath(codeGen, file).Replace('\\', '/');
            if (name is "Roslyn/RuntimeApi.cs" || AnchoredWriters.Contains(name)) continue;   // the façade itself, and the anchored writers
            string code = string.Join("\n",
                File.ReadAllLines(file).Where(l => !l.TrimStart().StartsWith("//", StringComparison.Ordinal)));
            int count = Bare.Matches(code).Count;
            if (count > 0)
                over.Add($"{name}: {count} bare Cobol*. accesses — route the emission through RuntimeApi");
        }
        Assert.True(over.Count == 0, string.Join("\n", over));
    }
}
