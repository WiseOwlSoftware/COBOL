// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit.Architecture;

/// <summary>
/// ⛔ THE PROGRAM COLLATING CARRIERS ARE SPELLED ONCE (kb/Work PB3215). A type with a non-native PROGRAM COLLATING SEQUENCE
/// (ISO §2023:12.3.6) declares <c>__COLLATE</c> / <c>__COLLATE_NAT</c> in <c>ObjectComputerEmit.EmitMembers</c>, and every
/// renderer that passes the sequence (relation conditions, CHAR/ORD/MAX/MIN, SORT/MERGE keys, indexed-file key collation, a
/// THROUGH range's IN phrase) names the field. Eleven string literals spelled it across five files, and nothing tied them to
/// the declaration: renaming it alone would emit C# that fails with CS0103. Every reader now names
/// <c>ObjectComputerEmit.ProgramCollation</c> / <c>NationalProgramCollation</c>; this test refuses a string literal that
/// spells the carrier anywhere else in the compiler.
/// </summary>
[Trait("Sweep", "tree")]
public sealed class CollationCarrierNameDriftTests
{
    private static readonly Regex Spelled = new(@"""[^""\r\n]*__COLLATE", RegexOptions.Compiled);

    [Fact]
    public void CollationCarrierNames_AreSpelledOnlyByObjectComputerEmit()
    {
        string root = TestRepo.Src("WiseOwl.Cobol.Compiler");
        string owner = Path.Combine("CodeGen", "Roslyn", "ObjectComputerEmit.cs");
        var offenders = new List<string>();
        foreach (string file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            string rel = Path.GetRelativePath(root, file);
            if (rel.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || rel.StartsWith($"obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                continue;
            string[] lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
            {
                string code = lines[i].TrimStart();
                if (code.StartsWith("//", StringComparison.Ordinal)) continue;
                if (!Spelled.IsMatch(code)) continue;
                if (rel == owner && code.StartsWith("public const string ", StringComparison.Ordinal)) continue;
                offenders.Add($"{rel}:{i + 1}: {code}");
            }
        }
        Assert.True(offenders.Count == 0,
            "a program collating carrier is spelled outside ObjectComputerEmit's constants (kb/Work PB3215):\n  "
            + string.Join("\n  ", offenders));
    }
}
