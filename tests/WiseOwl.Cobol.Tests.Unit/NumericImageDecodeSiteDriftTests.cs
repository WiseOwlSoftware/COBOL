// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>⛔ ONLY THE NAMED CODEGEN SITES DECODE A NUMERIC IMAGE THROUGH A RUNTIME LANE (kb/Work PB186).
/// <c>NumericRenderer.WindowedNum</c> is THE reader of a windowed leaf's value, and <c>NumericRenderer.CarrierOfImage</c>
/// THE crossing decode of a native carrier from its item's storage image; the table-SORT comparer used to carry a private
/// SIGNED-only copy of the first (kb/Work PB186), so a lane added to the one reader never reached it. Each other file
/// listed decodes for a different job and says why at its call site: the codecs that BUILD the image (a record struct's,
/// and a .NET host's published record's, which decodes the image the activated program left for its host,
/// DESIGN-external-repository §4.3), the alphanumeric-rendering channel (whose own three-lane dispatch is
/// <c>OperandText.NonTextBytes</c>), two STORE-side re-derivations (INSPECT's sign, SET's INDEX augment) that are not
/// sending reads of a numeric value, and the OPTIONS INITIALIZE background (InitialStateBackground), which seeds a native
/// float carrier from its fill image (kb/Work PB1134). Any other emitter that needs a value or a carrier from an image
/// asks one of the two readers (an overlapping MOVE's in-place store asks <c>CarrierOfImage</c>, kb/Work PB1910).
/// <para>A DRIFT test by name, so the drift-rule index (<c>scripts/spec/drift_rules.py</c>) reads its CodeGen scope and
/// the gate runs it in leg 1 for every change to a CodeGen file (tier 0d). Kept inside <c>SortNumericKeyLaneTests</c> it
/// was in no index and no leg-1 tier, so a change it governs met it late: kb/Work PB2099 part 4 at a whole gate, PB1910
/// only in its landing's Linux legs.</para></summary>
public sealed class NumericImageDecodeSiteDriftTests
{
    /// <summary>The files allowed to call a runtime image-decode lane, each for the job the class summary names.</summary>
    private static readonly HashSet<string> Allowed = new(StringComparer.Ordinal)
    {
        "RuntimeApi.cs", "NumericRenderer.cs", "OperandText.cs", "GroupImageCodec.cs", "InspectEmitter.cs",
        "SetEmitter.cs", "InitialStateBackground.cs", "PublishedRecordEmitter.cs",
    };

    private static readonly Regex Decode = new(@"\bNumParseImage(U128|Float|U)?\(", RegexOptions.Compiled);

    [Fact]
    public void OnlyTheNamedSites_DecodeANumericImage()
    {
        string codegen = TestRepo.Src("WiseOwl.Cobol.Compiler", "CodeGen");
        Assert.True(Directory.Exists(codegen), $"CodeGen moved: {codegen} is not a directory.");
        var offenders = new List<string>();
        foreach (string file in Directory.EnumerateFiles(codegen, "*.cs", SearchOption.AllDirectories))
        {
            if (Allowed.Contains(Path.GetFileName(file))) continue;
            string[] lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
            {
                string code = lines[i].TrimStart();
                if (code.StartsWith("//", StringComparison.Ordinal)) continue;
                if (Decode.IsMatch(code)) offenders.Add($"{Path.GetRelativePath(codegen, file)}:{i + 1}: {code}");
            }
        }
        Assert.True(offenders.Count == 0,
            "A numeric image is decoded outside the named sites — read a windowed leaf's value through "
            + "NumericRenderer.WindowedNum, or a native carrier from its storage image through "
            + "NumericRenderer.CarrierOfImage (kb/Work PB186):\n" + string.Join("\n", offenders));
    }
}
