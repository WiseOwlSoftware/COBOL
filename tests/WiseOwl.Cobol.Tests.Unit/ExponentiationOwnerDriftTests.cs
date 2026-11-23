// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using WiseOwl.Cobol.Runtime;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ THE OPERATOR <c>**</c> HAS ONE OWNER IN THE VALUE LIBRARY, AND §2023:8.8.1.2 RULE 6 IS SCREENED IN ONE PLACE (kb/Work PB2628).
/// The binary64 power was <c>CobolIntrinsics.PowNativeReal</c> although no intrinsic function is involved, and the rule-6 screen was
/// written twice (<c>CobolIntrinsics.CheckPowRule6</c> and inline in <c>CobolDec.Pow</c>), so <c>0 ** 0</c> raised
/// EC-SIZE-EXPONENTIATION with two texts depending on whether an ARITHMETIC clause was present. This class refuses a second raise of
/// the condition anywhere in the runtime and a power entry back on the intrinsic class.
/// </summary>
[Trait("Sweep", "tree")]
public sealed class ExponentiationOwnerDriftTests
{
    private static readonly TreeScope RuntimeSources = new(Under: ["src/WiseOwl.Cobol.Runtime/"], Kinds: [".cs"]);

    private const string Owner = "src/WiseOwl.Cobol.Runtime/Values/Numeric/CobolPower.cs";

    [Fact]
    public void EcSizeExponentiation_IsRaisedOnlyByCobolPower()
    {
        var offenders = new List<string>();
        int files = 0;
        foreach (string path in TrackedTree.Files(RuntimeSources))
        {
            files++;
            if (path == Owner) continue;
            string[] lines = File.ReadAllLines(TestRepo.Within("src/WiseOwl.Cobol.Runtime/", path));
            for (int i = 0; i < lines.Length; i++)
                if (ThrowsExponentiation(lines, i))
                    offenders.Add($"{path}:{i + 1}: {lines[i].Trim()}");
        }
        Assert.True(files >= 100, $"only {files} runtime source file(s) were scanned: the scope stopped matching the runtime");
        Assert.True(File.Exists(TestRepo.Within("src/WiseOwl.Cobol.Runtime/", Owner)), $"{Owner} is gone: the owner of ** moved, so this test must move with it");
        Assert.True(offenders.Count == 0,
            "EC-SIZE-EXPONENTIATION is raised outside CobolPower, a second copy of the §2023:8.8.1.2 rule-6 screen (ask "
            + "CobolPower.ScreenRule6; kb/Work PB2628):\n" + string.Join("\n", offenders));
    }

    /// <summary>A <c>new CobolSizeError(…)</c> whose condition argument, on this line or the next two, is EC-SIZE-EXPONENTIATION.</summary>
    private static bool ThrowsExponentiation(string[] lines, int i) =>
        lines[i].Contains("new CobolSizeError(", StringComparison.Ordinal)
        && lines.Skip(i).Take(3).Any(l => l.Contains("\"EC-SIZE-EXPONENTIATION\"", StringComparison.Ordinal));

    [Fact]
    public void NoPowerEntry_LivesOnTheIntrinsicClass()
    {
        var powers = typeof(CobolIntrinsics).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            .Where(m => m.Name.StartsWith("Pow", StringComparison.Ordinal) || m.Name.Contains("PowRule", StringComparison.Ordinal))
            .Select(m => m.Name).Distinct().ToList();
        Assert.True(powers.Count == 0,
            "the arithmetic operator ** is not an intrinsic function; its entries belong to CobolPower in the value library "
            + "(kb/Work PB2628, DESIGN-architecture-review §8.3 (3)): " + string.Join(", ", powers));
        Assert.NotNull(typeof(CobolPower).GetMethod(nameof(CobolPower.Binary64), [typeof(double), typeof(double)]));
    }
}
