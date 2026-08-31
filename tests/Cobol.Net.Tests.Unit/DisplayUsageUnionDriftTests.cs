// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE HAND-ROLLED DISPLAY-ONLY UNION INVENTORY (kb/Work PB164, the Step D review).
/// <para>
/// <c>{ Category: PicCategory.Numeric, IsFloat: false, Usage: Usage.Display }</c> is the shape a predicate takes
/// when it was written before the byte forms existed. Sometimes it is exactly right — the question really IS
/// "is this a zoned USAGE DISPLAY item?" — and sometimes it is DRIFT: the question is "does this leaf have a
/// character image?", which <see cref="CobolNet.Binding.Model.DataItem.ElementImageCapable"/> and
/// <c>PicInfo.HasImageByteForm</c> answer for every consumer, and which every numeric usage now satisfies
/// (V59 pinned BINARY/PACKED, PB164 wave 1 COMP-5/BINARY-*, wave 2 the IEEE float family, R40 USAGE INDEX).
/// </para>
/// <para>
/// The drift instance that motivated this test: <c>UdfBinder.UdfReturningResidue</c> screened a GROUP RETURNING
/// item's leaves with this union and rejected a COMP-5 leaf as having "no shared character image across the
/// activation boundary" — years after the group codec started emitting exactly that image. It rejected
/// conforming source (ISO §14.2.2 SR5 places NO category restriction on a RETURNING item), and nothing failed,
/// because a hand-rolled union has no drift lock. <c>V59ImagePredicateDriftTests</c> watches
/// <c>IsCharacterImage</c>; NOTHING watched this shape, and it is the more common one.
/// </para>
/// <para>
/// ⛔ THESE SITES ARE NOT AUTOMATICALLY BUGS — most are load-bearing, several are SPEC-REQUIRED, and the test
/// deliberately does not prejudge them. Its job is to make the inventory EXPLICIT so a new copy cannot appear
/// silently and a removed one records a deliberate decision. Each entry's classification is in the comment
/// beside it; the reasons were MEASURED, not assumed (a probe per claim — see the kb note).
/// </para>
/// </summary>
public sealed class DisplayUsageUnionDriftTests
{
    /// <summary>Count occurrences in CODE only — these predicates are discussed at length in comments, so a raw
    /// text count is a false positive. Strips <c>///</c> doc comments, whole-line <c>//</c> comments, and
    /// trailing <c>//</c> tails, exactly as <see cref="V59ImagePredicateDriftTests"/> does.</summary>
    private static int CodeOccurrences(string src, string needle)
    {
        var code = new List<string>();
        foreach (string raw in src.Split('\n'))
        {
            string line = raw.TrimEnd('\r');
            string t = line.TrimStart();
            if (t.StartsWith("///") || t.StartsWith("//")) continue;
            int i = line.IndexOf("//", StringComparison.Ordinal);
            code.Add(i >= 0 ? line[..i] : line);
        }
        return Regex.Matches(string.Join("\n", code), Regex.Escape(needle)).Count;
    }

    [Fact]
    public void HandRolledDisplayUnion_IsTheKnownInventory()
    {
        var expected = new Dictionary<string, int>
        {
            // KEEPER — the REPORT SECTION composes a printable CHARACTER image by nature (§13.18.x report
            // items); the question really is "is this a zoned DISPLAY field?".
            ["Binding/DataBinder.Reports.cs"] = 1,

            // KEEPERS (7):
            //  :521  MarkImageLeaves — which leaves must be STORED AS their image. A byte-form leaf keeps its
            //        native carrier and encodes on demand; only a zoned leaf's storage IS characters.
            //  :1923, :3262  USAGE INHERITANCE — genuinely asks "is this item still at the default DISPLAY
            //        usage, so an inherited BINARY/PACKED/COMP-5 applies?". Usage.Display as a VALUE.
            //  :3584  the Tier-B StringCanonical split: the DISPLAY arm marks through the CHARACTER pipeline,
            //        and the very next arm marks HasImageByteForm leaves through the BYTE pipeline. Both arms
            //        present — this is the deliberate two-lane split, not a missing lane.
            //  (+ the remaining occurrences of the same three shapes on those lines.)
            ["Binding/DataBinder.cs"] = 7,

            // KEEPERS — SPEC-REQUIRED. §8.4.3.3.3 SR1 admits reference modification of "a numeric data item of
            // usage display or national" ONLY, so the RENAMES/partial-cell and ref-mod view sites are enforcing
            // the standard, not a stale image predicate. MEASURED: `MOVE NB(1:1) TO X` over a COMP-5 item draws
            // COBOLNET1647 citing that rule.
            ["Binding/ReferenceResolver.cs"] = 4,

            // :76  KEEPER — the whole-group promotion to CharImage storage; same question as DataBinder:521.
            // :123 KEEPER, OUT-OF-WAVE — UnifyCrossing's windowed-float vs native-float OO crossing is
            //      registered separately as kb/Work PB187; do not "migrate" it here.
            ["Binding/Passes/StorageFormPass.cs"] = 2,

            // :136 KEEPER — a NON-DIGIT figurative fill deposits fill CHARACTERS into the receiver's cells
            //      ("MOVE QUOTE TO PIC 9(3) leaves three quotation marks"); a 2-byte binary carrier has no
            //      character cells to hold them.
            // :274 KEEPER, SPEC-REQUIRED — MarkRefModStoreImage, gated by §8.4.3.3.3 SR1 as above.
            ["Binding/Procedure/Verbs/MoveBinder.cs"] = 2,

            // KEEPER — SPEC-REQUIRED. §14.9.48.3 SR4 restricts an UNSTRING numeric receiver to usage display
            // or national; a COMP/packed/COMP-5/index/float receiver is rejected BY THE STANDARD.
            ["Binding/Procedure/Verbs/StringUnstringBinder.cs"] = 1,

            // ⚠ OPEN QUESTION, not a keeper and not this wave's drift — registered as kb/Work PB184. §13.18.63.4
            // GR5: "the group area is initialized without consideration for the individual elementary or group
            // items contained within this group" (cite.py --check OK), which would deposit the literal's BYTES
            // into byte-form leaves; today they are undistributable and take the member-wise default.
            // MEASURED: `01 GV VALUE "40537". 05 GB PIC 9 COMP-5 OCCURS 5.` leaves every GB zero.
            // That is a separate GR5 derivation, deliberately NOT changed under a predicate-drift sweep.
            ["CodeGen/DataDivision/GroupValueSlicer.cs"] = 2,

            // KEEPER, OUT-OF-WAVE — D-U6a's UNIVERSAL box bridge, the same OO crossing as kb/Work PB187.
            ["CodeGen/Verbs/OoEmitter.cs"] = 1,

            // KEEPER, OUT-OF-WAVE — WriteImage's guard is kb/Work PB177 leg D (§14.9.43.3 SR1: the screen is
            // SKIPPED for a windowed / image-stored receiver, so illegal source is accepted silently).
            ["CodeGen/Verbs/StringEmitter.cs"] = 1,
        };

        var actual = new Dictionary<string, int>();
        string root = TestRepo.Src("Cobol.Net.Compiler");
        foreach (string f in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            string rel = Path.GetRelativePath(root, f).Replace('\\', '/');
            int n = CodeOccurrences(File.ReadAllText(f), "Usage: Usage.Display");
            if (n > 0) actual[rel] = n;
        }

        Assert.Equal(
            expected.OrderBy(k => k.Key).Select(k => $"{k.Key}={k.Value}").ToArray(),
            actual.OrderBy(k => k.Key).Select(k => $"{k.Key}={k.Value}").ToArray());
    }

    /// <summary>The drift instance itself, pinned so it cannot come back: the UDF group-RETURNING screen must
    /// ask the DERIVED image predicate. Its elementary arms keep their own spec-required rejections
    /// (§13.18.60.3 SR10 permits an index item in a USING phrase but not as a RETURNING item; the
    /// pointer/object refusals), which is why this pins the GROUP screen specifically.</summary>
    [Fact]
    public void UdfReturningResidue_GroupScreen_UsesTheDerivedImagePredicate()
    {
        string src = File.ReadAllText(TestRepo.At("src", "Cobol.Net.Compiler", "Binding", "Procedure", "Verbs",
                                                  "UdfBinder.cs"));
        Assert.Equal(0, CodeOccurrences(src, "Usage: Usage.Display"));
        Assert.True(CodeOccurrences(src, "ElementImageCapable") >= 1,
            "UdfReturningResidue's group-leaf screen must ask DataItem.ElementImageCapable — a group RETURNING "
            + "item whose leaves all have a byte image crosses the activation boundary through the group codec "
            + "(ISO §14.2.2 SR5 places no category restriction on a RETURNING item). A hand-rolled usage union "
            + "here rejected conforming source once already.");
    }
}
