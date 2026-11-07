// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Runtime;

/// <summary>
/// The cause that triggered a debugging declarative for a <c>USE FOR DEBUGGING ON procedure-name / ALL PROCEDURES</c>
/// subject (X3.23-1985 debug module — the DEBUG-CONTENTS taxonomy, corroborated by the CCVS DB101A witness). The
/// facility was deleted by ISO/IEC 1989:2002 and is absent from ISO/IEC 1989:2023, so its authoritative behavior is
/// the 1985 standard; WiseOwl COBOL models it only at <c>--std 85</c> (VCR Table 7 row 7.17). Each enumerand renders one
/// DEBUG-CONTENTS string (<see cref="DebugItem.Populate"/>); the transfer-of-control kind that reaches the subject
/// procedure selects it (the PC dispatcher knows the transfer kind).
/// </summary>
public enum DebugCause
{
    /// <summary>A plain PERFORM / GO TO / altered GO TO transfer to the procedure — DEBUG-CONTENTS is SPACES
    /// (X3.23-1985; DB101A "DBCONT-HOLD EQUAL TO SPACE"). The default (0), so an unset field renders SPACES.</summary>
    Transfer = 0,

    /// <summary>First execution of the FIRST nondeclarative procedure — DEBUG-CONTENTS "START PROGRAM"
    /// (DB101A START-PROGRAM-TEST).</summary>
    StartProgram,

    /// <summary>Sequential fall-through into the procedure — DEBUG-CONTENTS "FALL THROUGH" (DB101A).</summary>
    FallThrough,

    /// <summary>The 2nd..nth iteration of a PERFORM … TIMES/UNTIL/VARYING whose range is the procedure —
    /// DEBUG-CONTENTS "PERFORM LOOP" (DB101A, "PERFORM LOOP-ROUTINE FIVE TIMES").</summary>
    PerformLoop,
}

/// <summary>
/// The X3.23-1985 <c>DEBUG-ITEM</c> special register (the '85 debug module). It is IMPLICITLY described — no DATA
/// DIVISION entry — and referenced only inside debugging declaratives. Its members are string-imaged fixed-width
/// fields; the compiler resolves a DEBUG-* reference to a read-only view over this object (a
/// <c>DebugRegisterPlace</c>), so MOVE / DISPLAY of a member "just work". The debug trigger the PC dispatcher
/// injects at a subject procedure's entry space-fills the whole register then sets the members for the triggering
/// occurrence (<see cref="Populate"/>) before it runs the declarative body.
/// <para>Layout (the X3.23-1985 implicit description — DEBUG-SUB-n is S9(4) SIGN LEADING SEPARATE, five
/// characters; general rule 10). The register holds each member's IMAGE: DEBUG-SUB-n as <c>+0004</c> (the sign, then four digits), so a
/// numeric use decodes it, and a MOVE to an alphanumeric item drops the separate sign (ISO MOVE GR6a, §2023:14.9.25.4,
/// specs/ISO_COBOL_2023.md:28921): the CCVS DB201A witness (<c>MOVE DEBUG-SUB-1 TO SUB-1-1</c>, <c>SUB-1-1 PIC X(5)</c>, then
/// <c>IF SUB-1-1 = "0004"</c>) sees <c>"0004 "</c>. DEBUG-SUB is SPACES when the triggering reference is not subscripted.</para>
/// <code>
/// 01  DEBUG-ITEM.
///     02  DEBUG-LINE     PIC X(6).
///     02  FILLER         PIC X   VALUE SPACE.
///     02  DEBUG-NAME     PIC X(30).
///     02  FILLER         PIC X   VALUE SPACE.
///     02  DEBUG-SUB-1    PIC S9(4) SIGN LEADING SEPARATE.   *> spaces when the reference is not subscripted
///     02  FILLER         PIC X   VALUE SPACE.
///     02  DEBUG-SUB-2    PIC S9(4) SIGN LEADING SEPARATE.
///     02  FILLER         PIC X   VALUE SPACE.
///     02  DEBUG-SUB-3    PIC S9(4) SIGN LEADING SEPARATE.
///     02  FILLER         PIC X   VALUE SPACE.
///     02  DEBUG-CONTENTS PIC X(n).   *> n: the largest of 30 and every data-name or cd-name target's image width
///                                    *> (X3.23-1985 USE FOR DEBUGGING general rule 15; docs/CONTROL_FLOW_DESIGN.md D20).
/// </code>
/// </summary>
public sealed class DebugItem(int contentsWidth = DebugItem.MinContentsWidth)
{
    /// <summary>DEBUG-LINE width — PIC X(6).</summary>
    public const int LineWidth = 6;
    /// <summary>DEBUG-NAME width — PIC X(30).</summary>
    public const int NameWidth = 30;
    /// <summary>DEBUG-SUB-1/2/3 width — S9(4) SIGN LEADING SEPARATE (a sign character + four digits = five).</summary>
    public const int SubWidth = 5;
    /// <summary>The smallest DEBUG-CONTENTS width — enough for every procedure-trigger token ("START PROGRAM" is the
    /// longest). A program whose data-name or cd-name targets are wider declares a wider register
    /// (<see cref="ContentsWidth"/>).</summary>
    public const int MinContentsWidth = 30;
    /// <summary>The most subscripted levels DEBUG-SUB-n can report: the 1985 implicit description has DEBUG-SUB-1 to DEBUG-SUB-3.</summary>
    public const int MaxSubscriptLevels = 3;

    /// <summary>The whole DEBUG-ITEM group image width for a register whose DEBUG-CONTENTS is
    /// <paramref name="contentsWidth"/> wide — the members plus the single-space FILLER between each
    /// (6 + 1 + 30 + 1 + 5 + 1 + 5 + 1 + 5 + 1 + n).</summary>
    public static int GroupWidthFor(int contentsWidth) =>
        LineWidth + 1 + NameWidth + 1 + SubWidth + 1 + SubWidth + 1 + SubWidth + 1 + contentsWidth;

    /// <summary>DEBUG-CONTENTS width — the declared register's, never less than <see cref="MinContentsWidth"/>.</summary>
    public int ContentsWidth { get; } = Math.Max(contentsWidth, MinContentsWidth);

    /// <summary>DEBUG-LINE — the source line of the CAUSING statement (the statement whose execution
    /// triggered the debugging declarative — the Wave F review fix, pinned by DB101A; implementor-defined
    /// format: WiseOwl COBOL right-justifies the decimal image in X(6)).</summary>
    public string DebugLine { get; private set; } = new string(' ', LineWidth);

    /// <summary>DEBUG-NAME — the leftmost 30 characters of the triggering name.</summary>
    public string DebugName { get; private set; } = new string(' ', NameWidth);

    /// <summary>DEBUG-SUB-1 — SPACES unless the triggering reference is subscripted (its outermost occurrence number).</summary>
    public string DebugSub1 { get; private set; } = new string(' ', SubWidth);
    /// <summary>DEBUG-SUB-2 — SPACES unless the triggering reference has a second subscript.</summary>
    public string DebugSub2 { get; private set; } = new string(' ', SubWidth);
    /// <summary>DEBUG-SUB-3 — SPACES unless the triggering reference has a third subscript.</summary>
    public string DebugSub3 { get; private set; } = new string(' ', SubWidth);

    /// <summary>DEBUG-CONTENTS — the DEBUG-CONTENTS taxonomy string for the triggering cause (X3.23-1985), or the
    /// referenced item's content.</summary>
    public string DebugContents { get; private set; } = new string(' ', MinContentsWidth);

    /// <summary>The whole DEBUG-ITEM group image (§2023:13.18 group MOVE reads this): the members concatenated with the
    /// single-space FILLER positions between them.</summary>
    public string Image =>
        DebugLine + " " + DebugName + " " + DebugSub1 + " " + DebugSub2 + " " + DebugSub3 + " " + DebugContents;

    /// <summary>Space-fill the whole register (X3.23-1985: DEBUG-ITEM is space-filled before each execution of a
    /// debugging declarative), then set DEBUG-LINE / DEBUG-NAME / DEBUG-CONTENTS for a procedure trigger. DEBUG-SUB-n
    /// stays SPACES (a procedure reference has no subscripts).</summary>
    /// <param name="line">The subject procedure's source line number.</param>
    /// <param name="name">The triggering procedure-name (left-justified/truncated to 30).</param>
    /// <param name="cause">The transfer-of-control cause selecting the DEBUG-CONTENTS token.</param>
    public void Populate(int line, string name, DebugCause cause) => Set(line, name, ContentsFor(cause), []);

    /// <summary>Space-fill the register, then set it for a data-name or cd-name trigger (X3.23-1985 USE FOR DEBUGGING
    /// general rules 14, 23 and 24; docs/CONTROL_FLOW_DESIGN.md D20): DEBUG-NAME the name as written, DEBUG-CONTENTS the
    /// referenced item's content (<paramref name="contents"/>, an alphanumeric image, left-justified and space-filled),
    /// DEBUG-SUB-n the occurrence numbers, outermost first, each as the S9(4) SIGN LEADING SEPARATE image the implicit
    /// description gives it; the members past the last occurrence number stay SPACES.</summary>
    /// <param name="line">The referencing statement's source line number.</param>
    /// <param name="name">The name as written, its qualifiers joined by OF or IN.</param>
    /// <param name="contents">The content of the referenced item.</param>
    /// <param name="occurrences">The occurrence number of each subscripted level of the reference, outermost first
    /// (at most three: the 1985 implicit description has DEBUG-SUB-1 to DEBUG-SUB-3).</param>
    public void PopulateReference(int line, string name, string contents, params long[] occurrences) =>
        Set(line, name, contents, occurrences);

    private void Set(int line, string name, string contents, ReadOnlySpan<long> occurrences)
    {
        DebugLine = Fit(line >= 0 ? line.ToString(System.Globalization.CultureInfo.InvariantCulture) : "", LineWidth, right: true);
        DebugName = Fit(name, NameWidth, right: false);
        DebugSub1 = SubImage(occurrences, 0);
        DebugSub2 = SubImage(occurrences, 1);
        DebugSub3 = SubImage(occurrences, 2);
        DebugContents = Fit(contents, ContentsWidth, right: false);
    }

    /// <summary>A program store into DEBUG-LINE (<c>MOVE ... TO DEBUG-LINE</c>): the image is fitted to the member's PICTURE X(6), left-justified.
    /// The standard names only the facility's updates (X3.23-1985 USE FOR DEBUGGING general rule 11) and states no rule against a program's own
    /// store, whose effect lasts until the facility updates the register again (the GnuCOBOL 3.2 behavior).</summary>
    public void StoreLine(string image) => DebugLine = Fit(image, LineWidth, right: false);

    /// <summary>A program store into DEBUG-NAME, as <see cref="StoreLine"/>.</summary>
    public void StoreName(string image) => DebugName = Fit(image, NameWidth, right: false);

    /// <summary>A program store into DEBUG-SUB-<paramref name="level"/> (1 to 3): the item's five-character image.</summary>
    public void StoreSub(int level, string image)
    {
        var fitted = Fit(image, SubWidth, right: false);
        switch (level)
        {
            case 1: DebugSub1 = fitted; break;
            case 2: DebugSub2 = fitted; break;
            default: DebugSub3 = fitted; break;
        }
    }

    /// <summary>A program store into DEBUG-CONTENTS, as <see cref="StoreLine"/>.</summary>
    public void StoreContents(string image) => DebugContents = Fit(image, ContentsWidth, right: false);

    /// <summary>A program store into the whole DEBUG-ITEM group: the group image is distributed over the members at their offsets in the
    /// implicit description (the FILLER positions are not stored).</summary>
    public void StoreImage(string image)
    {
        var whole = Fit(image, GroupWidthFor(ContentsWidth), right: false);
        int at = 0;
        string Take(int width) { var part = whole.Substring(at, width); at += width + 1; return part; }
        DebugLine = Take(LineWidth);
        DebugName = Take(NameWidth);
        DebugSub1 = Take(SubWidth);
        DebugSub2 = Take(SubWidth);
        DebugSub3 = Take(SubWidth);
        DebugContents = Take(ContentsWidth);
    }

    /// <summary>The image of occurrence number <paramref name="at"/>, or SPACES when the reference has no such level: the five characters of
    /// general rule 10's S9(4) SIGN LEADING SEPARATE item, the sign and then four digits. An occurrence number is positive; one past four
    /// digits keeps its low-order four, the size of the item.</summary>
    private static string SubImage(ReadOnlySpan<long> occurrences, int at)
    {
        if (at >= occurrences.Length) return new string(' ', SubWidth);
        return "+" + (Math.Abs(occurrences[at]) % 10000).ToString("0000", System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>The DEBUG-CONTENTS token for a procedure-trigger cause (X3.23-1985; the DB101A-witnessed taxonomy).
    /// The SORT INPUT/OUTPUT, MERGE OUTPUT and USE PROCEDURE causes are staged — a program combining an active
    /// debugging declarative with those is rejected COBOL1571 at bind, so they never reach here.</summary>
    private static string ContentsFor(DebugCause cause) => cause switch
    {
        DebugCause.StartProgram => "START PROGRAM",
        DebugCause.FallThrough => "FALL THROUGH",
        DebugCause.PerformLoop => "PERFORM LOOP",
        _ => "",   // Transfer — SPACES (a plain PERFORM / GO TO transfer)
    };

    /// <summary>Fit a string to an exact width: pad with spaces (left- or right-justified) or truncate.</summary>
    private static string Fit(string s, int width, bool right)
    {
        s ??= "";
        if (s.Length > width) return right ? s[^width..] : s[..width];
        return right ? s.PadLeft(width) : s.PadRight(width);
    }
}
