// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime.Exceptions;
namespace WiseOwl.Cobol.Runtime;

/// <summary>
/// Table-element access (ISO §2023:8.4.2.3 subscripting). Every emitted subscripted reference routes through
/// <see cref="At{T}"/> — the ONE occurrence-number→element mapping.
/// </summary>
public static class CobolTable
{
    /// <summary>The concatenated images of the first <paramref name="count"/> occurrences of an OCCURS DEPENDING
    /// <paramref name="table"/> whose elements are VARIABLE-LENGTH groups (kb/Work PB244; ISO §2023:14.9.11.4 GR7's
    /// documented format): each occurrence contributes its own current extent
    /// (<paramref name="image"/> = its <c>CurrentImage()</c>), in occurrence order, and the count is the table's
    /// current one (§2023:13.18.38.4 GR8 — only the part the DEPENDING item names is used). A count outside the table is
    /// clamped; the caller has already applied the EC-BOUND-ODO clamp (<c>TableOdoExtent</c>).</summary>
    public static string ConcatImages<T>(T[] table, int count, Func<T, string> image)
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < Math.Clamp(count, 0, table.Length); i++) sb.Append(image(table[i]));
        return sb.ToString();
    }

    /// <summary>The §2023:8.5.1.12 carrier of the first <paramref name="count"/> occurrences of an OCCURS DEPENDING
    /// <paramref name="table"/> whose elements are variable-length groups (kb/Work PB244): each occurrence's own
    /// carrier, concatenated in occurrence order (<see cref="CobolVarGroup.Concat"/>) - the flattening a fixed-OCCURS
    /// table of such elements gets, at the table's CURRENT count (§2023:13.18.38.4 GR8; the MAXIMUM at an activation
    /// boundary, which passes it). A count outside the table is clamped; the caller has already applied the
    /// EC-BOUND-ODO clamp (<c>TableOdoExtent</c>).</summary>
    public static CobolVarGroup ConcatVarImages<T>(T[] table, int count, Func<T, CobolVarGroup> carrier)
    {
        var occurrences = new CobolVarGroup[Math.Clamp(count, 0, table.Length)];
        for (int i = 0; i < occurrences.Length; i++) occurrences[i] = carrier(table[i]);
        return CobolVarGroup.Concat(occurrences);
    }

    /// <summary>
    /// The table element at a 1-based <paramref name="occurrence"/> number, as a writable reference. The parameter is
    /// an <see cref="Occurrence"/> (kb/Work PB2695) so a subscript that left <c>long</c>'s range reaches the
    /// diagnostic with its exact value; a native <c>long</c> or <c>int</c> converts implicitly, and a conversion to
    /// <c>long</c> exists too, so the table accessors have exactly ONE overload per job (a <c>long</c> overload beside
    /// this one would make every <c>int</c> argument ambiguous).
    /// <para><b>Out-of-range</b> (ISO §2023:8.4.2.3.4 GR2): with EC-BOUND-SUBSCRIPT checking ON the condition is raised
    /// (<see cref="OutOfRange{T}"/>); with it OFF — the COBOL-85 semantics, since COBOL-85 has no exception
    /// conditions — the reference continues benignly per the implementor-defined rule the NIST-85 golden requires:
    /// reads see a fresh default-valued element (spaces-equivalent for alphanumeric), writes are absorbed. The element
    /// is a per-type scratch slot, re-defaulted on every out-of-range access. Caveat: a GROUP element's scratch is a
    /// zeroed struct — its string members are null, so group-level use of an out-of-range element may still fail
    /// loudly. An occurrence that saturated sits at <c>long</c>'s extreme, which no table contains, so it falls
    /// into the out-of-range outcome with no test of its own.</para>
    /// </summary>
    public static ref T At<T>(T[] table, Occurrence occurrence)
    {
        long position = occurrence.Value;
        // A NULL table is itself an out-of-range chain: a multi-dimension reference whose OUTER subscript was
        // out of range continues through the zeroed scratch struct, whose nested OCCURS arrays are null
        // (NC401M's 5-deep FAIL-path read) — every further level resolves benignly too.
        int highest = table?.Length ?? 0;
        if (Permissible(position, highest)) return ref table![(int)(position - 1)];
        SubscriptOutOfRange(occurrence, highest);
        return ref OutOfRange<T>();
    }

    /// <summary>⛔ THE ONE §2023:8.4.2.3.4 GR2 RANGE TEST of a subscript, for every kind of table the runtime holds: a
    /// fixed or occurs-depending table here, a dynamic-capacity table's <see cref="CobolDynTable{T}"/> accessors and a
    /// report writer's repeating sum counter (kb/Work PB2648: each kind used to write the test itself). GR2: "If the
    /// value of the subscript is not a positive integer or is less than one or is greater than the highest permissible
    /// occurrence number, the EC-BOUND-SUBSCRIPT exception condition is set to exist." <paramref name="highest"/> is
    /// the highest permissible occurrence number of the table kind asking.</summary>
    internal static bool Permissible(long position, long highest) => position >= 1 && position <= highest;

    /// <summary>⛔ THE ONE §2023:8.4.2.3.4 GR2 RAISE (kb/Work PB2648): sets EC-BOUND-SUBSCRIPT with the one detail text,
    /// which names the subscript's value as the program holds it (<see cref="Occurrence.ToString"/>, even past
    /// <c>long</c>) and the range it left. Raised BEFORE any continuation, so a CHECKING-ON reference reports the
    /// condition; with checking off it returns and the caller continues with its own table kind's continuation (the
    /// fixed table's <see cref="OutOfRange{T}"/>, the dynamic table's seeded scratch, the sum counter's zero).</summary>
    /// <param name="occurrence">The subscript's value.</param>
    /// <param name="highest">The highest permissible occurrence number, or <see langword="null"/> when the reference
    /// has no upper bound (a dynamic table's RECEIVING reference grows to any positive occurrence, §2023:8.5.1.9.3);
    /// the detail then says the value "is not a positive integer".</param>
    /// <param name="subject">What the subscript selects, when it is not a data item's table (a report's sum counter).</param>
    /// <param name="range">What the range is, when it is not the table's size (a dynamic table's current capacity).</param>
    /// <param name="clause">The clause that defines the range, cited before GR2 (a dynamic table's 8.5.1.9.2).</param>
    internal static void SubscriptOutOfRange(Occurrence occurrence, long? highest, string? subject = null,
                                             string? range = null, string? clause = null)
    {
        string outside = highest is { } h ? $"is outside 1..{h}{(range is null ? "" : ", " + range)}" : "is not a positive integer";
        ExceptionState.SubscriptError(
            $"{(subject is null ? "" : subject + " ")}subscript {occurrence} {outside} (ISO {(clause is null ? "" : clause + ", ")}8.4.2.3.4 GR2)");
    }

    /// <summary>The checking-off continuation of a fixed or occurs-depending table's out-of-range reference, reached
    /// only after <see cref="SubscriptOutOfRange"/> returned (the policy <see cref="At{T}(T[], Occurrence)"/> states):
    /// the per-thread scratch cell, re-defaulted on every out-of-range access to the empty value
    /// (<c>string.Empty</c> for a string element, the zeroed value otherwise).</summary>
    private static ref T OutOfRange<T>()
    {
        Scratch<T>.Slot = typeof(T) == typeof(string) ? (T)(object)string.Empty : default!;
        return ref Scratch<T>.Slot;
    }

    /// <summary>⛔ THE ONE §2023:13.18.38.4 GR7 BOUND TEST of an OCCURS DEPENDING value (kb/Work PB2648: the element
    /// reference and the extent channel each wrote it): "the value of the data item referenced by data-name-1 shall
    /// fall within the bounds from integer-1 through integer-2. If the value of the data item does not fall within
    /// the specified bounds, the EC-BOUND-ODO exception condition is set to exist." <paramref name="where"/> names the
    /// use the value was tested at, when that is a reference to the table.</summary>
    private static void TestOdoBounds(Occurrence depending, int min, int max, string? where = null)
    {
        if (depending.Value < min || depending.Value > max)
            ExceptionState.OdoError(
                $"OCCURS DEPENDING value {depending} is outside {min}..{max}{(where is null ? "" : " " + where)} (ISO 13.18.38.4 GR7)");
    }

    /// <summary>An element of an OCCURS DEPENDING table, referenced where EC-BOUND-ODO checking can be enabled:
    /// ISO §2023:13.18.38.4 GR7 — "At the time the subject of entry is referenced or any data item subordinate or
    /// superordinate to the subject of entry is referenced, the value of the data item referenced by data-name-1 shall
    /// fall within the bounds from integer-1 through integer-2. If the value of the data item does not fall within the
    /// specified bounds, the EC-BOUND-ODO exception condition is set to exist." A reference to the subject or to an
    /// item subordinate to it passes through this level's subscript, so the test is made HERE, before the element is
    /// located (kb/Work PB1268 — only the superordinate group's extent, <see cref="OdoExtent"/>, used to test it).
    /// With checking off the condition is not raised and the element reference proceeds exactly as
    /// <see cref="At{T}(T[], Occurrence)"/>; the subscript's own range is still 1..integer-2 (§2023:8.4.2.3.4 GR2): "the highest
    /// permissible occurrence number … of an occurs-depending table is the maximum number of occurrences").</summary>
    public static ref T At<T>(T[] table, Occurrence occurrence, Occurrence depending, int min, int max)
    {
        TestOdoBounds(depending, min, max, "at a reference to the table");
        return ref At(table, occurrence);
    }

    /// <summary>⛔ THE SUBSCRIPT OF A TABLE REFERENCE THAT LOCATES ITS ELEMENT BY ARITHMETIC, not through <see cref="At{T}(T[], Occurrence)"/>
    /// — a Tier-B view's window over its class's one backing (kb/Work PB5762): the §2023:8.4.2.3.4 GR2 range test
    /// (<see cref="Permissible"/> / <see cref="SubscriptOutOfRange"/>, the same ONE test and raise) against
    /// <paramref name="highest"/>, the table's highest permissible occurrence number. Returns the subscript's value; with
    /// checking off the reference continues, as every out-of-range reference does.</summary>
    public static long Subscript(Occurrence occurrence, long highest)
    {
        if (!Permissible(occurrence.Value, highest)) SubscriptOutOfRange(occurrence, highest);
        return occurrence.Value;
    }

    /// <summary>The <see cref="Subscript(Occurrence, long)"/> of an element of an occurs-depending table: the same bound test of
    /// data-name-1 (§2023:13.18.38.4 GR7, kb/Work PB5605) the <see cref="At{T}(T[], Occurrence, Occurrence, int, int)"/> of an
    /// element path makes, before the subscript's own range (§2023:8.4.2.3.4 GR2 against integer-2).</summary>
    public static long Subscript(Occurrence occurrence, long highest, Occurrence depending, int min, int max)
    {
        TestOdoBounds(depending, min, max, "at a reference to the table");
        return Subscript(occurrence, highest);
    }

    /// <summary>The out-of-range reference's scratch cell (checking off: the reference reads the empty value and a
    /// store into it is discarded). <b>One per THREAD</b> (kb/Work PB1069): it is returned by <c>ref</c>, so a
    /// process-wide cell let two run units on two threads that both took an out-of-range subscript write one
    /// location between the reset above and the caller's use. A run unit executes on one thread at a time and the
    /// cell is overwritten before every use, so a per-thread cell carries nothing between run units.</summary>
    private static class Scratch<T>
    {
        [ThreadStatic] private static Cell? s_cell;

        /// <summary>This thread's cell (created on the thread's first out-of-range reference of type T).</summary>
        public static ref T Slot => ref (s_cell ??= new Cell()).Value;

        private sealed class Cell { public T Value = default!; }
    }

    /// <summary>The CURRENT character extent of an occurs-depending GROUP operand (ISO/IEC 1989:2023 §2023:13.18.38
    /// GR8): the fixed prefix plus data-name-1's value clamped to [0, max] occurrences, times the per-occurrence
    /// width. A count outside integer-1..integer-2 makes the excess content undefined (GR7); the benign clamp is
    /// the COBOL-85 policy (no exception conditions) and the 2002+ default until EC-BOUND-ODO checking lands.</summary>
    /// <param name="min">integer-1 of the OCCURS DEPENDING clause — the LOWER bound §2023:13.18.38.4 GR7 requires the
    /// control value to fall within. It was previously absent and the floor hardcoded to 0, so a below-minimum
    /// DEPENDING value silently clamped instead of raising.</param>
    public static int OdoExtent(Occurrence count, int min, int max, int fixedUnits, int elemUnits) =>
        fixedUnits + (int)CurrentCount(count, min, max) * elemUnits;

    /// <summary>The CURRENT extent, in positions, of an occurs-depending GROUP operand whose occurrences are
    /// <paramref name="strideUnits"/> apart but whose LAST occurrence ends where its own width does — a bit table's
    /// with an ALIGNED subject or member (§2023:13.18.1.4 GR2: "applies to each occurrence of that item", so the
    /// occurrences are a whole number of bytes apart, kb/Work PB3167; §2023:8.5.1.6.3 generates filler only "as needed to advance alignment to a required natural boundary for the
    /// next item", and the last occurrence has no next one). <paramref name="trailingUnits"/> is the filler that pads
    /// that last occurrence to the stride (<c>stride − width</c>); it is given back once at least one occurrence
    /// exists: the extent at <c>c</c> occurrences is <c>fixed + c × stride − trailing</c> for <c>c &gt; 0</c> and
    /// <c>fixed</c> for none, which is the extent the same table fixed at <c>c</c> occurrences has
    /// (<c>BitLayout.TableBits</c>), and what FUNCTION LENGTH of a bit group counts in boolean positions (§2023:15.50.4
    /// rule 1). With no trailing filler it is <see cref="OdoExtent"/>.</summary>
    public static int OdoExtentTrimmed(Occurrence count, int min, int max, int fixedUnits, int strideUnits, int trailingUnits) =>
        TrimmedExtent(CurrentCount(count, min, max), fixedUnits, strideUnits, trailingUnits);

    /// <summary>⛔ THE ONE RULE for the extent of <paramref name="count"/> occurrences <paramref name="strideUnits"/>
    /// apart after <paramref name="fixedUnits"/>, the last of them giving back its <paramref name="trailingUnits"/> of
    /// filler (<see cref="OdoExtentTrimmed"/>'s rule, over a count already tested and clamped). Read by the extent
    /// channels here and by <see cref="OdoTail"/>, the variable-length group codec's and a cell window's geometry of the
    /// same table (kb/Work PB3167 review R1-1: that third channel had restated the rule without the trailing term).</summary>
    internal static int TrimmedExtent(long count, int fixedUnits, int strideUnits, int trailingUnits) =>
        fixedUnits + (int)count * strideUnits - (count > 0 ? trailingUnits : 0);

    /// <summary>The occurrence count an extent is measured at: data-name-1's value, tested against
    /// integer-1..integer-2 and clamped to 0..integer-2.</summary>
    private static long CurrentCount(Occurrence count, int min, int max)
    {
        // §2023:13.18.38.4 GR7: both ends matter (TestOdoBounds); checking off keeps the clamp, whose result GR7's
        // closing sentence makes undefined content and therefore a conforming implementor choice.
        TestOdoBounds(count, min, max);
        long n = count.Value;
        return n < 0 ? 0 : n > max ? max : n;
    }

    /// <summary>The current CHARACTER extent of an occurs-depending GROUP operand whose positions are counted in
    /// <paramref name="positionsPerChar"/> units (1 for a character subtree, 8 for one holding USAGE BIT leaves —
    /// ISO/IEC 1989:2023 §2023:8.5.1.6.3, kb/Work PB173): <see cref="OdoExtentTrimmed"/> rounded UP to whole characters.
    /// The ceiling is where §2023:8.5.1.6.3's trailing-filler rule puts the group's own partial byte, and it is the same
    /// rounding §2023:15.50.4 RV9 requires of every other bit width. With <paramref name="positionsPerChar"/> = 1 it is
    /// the identity, so the character channel has ONE arm, never a units branch.
    /// <para>⛔ The LAST occurrence's <paramref name="trailingUnits"/> are given back BEFORE the ceiling, exactly as the
    /// position channel gives them back (kb/Work PB3167): a bit table whose occurrences stride whole bytes can begin in
    /// the middle of a byte, and then its stride-counted end lies in a later byte than its real end does —
    /// <c>05 BA PIC 1(2). 05 SUB GROUP-USAGE BIT OCCURS 1 TO 3 DEPENDING ON N. 10 SA PIC 1. 10 TA PIC 1(5) ALIGNED.</c>
    /// at N = 2 ends at bit 29 (4 characters), its stride count at bit 34 (5).</para></summary>
    public static int OdoExtentChars(Occurrence count, int min, int max, int fixedUnits, int strideUnits, int trailingUnits,
                                     int positionsPerChar)
    {
        int units = OdoExtentTrimmed(count, min, max, fixedUnits, strideUnits, trailingUnits);
        return positionsPerChar <= 1 ? units : (units + positionsPerChar - 1) / positionsPerChar;
    }

    // ── The table(ALL) intrinsic-argument enumeration (ISO §2023:15.3; kb/Work PB62) ─────────────────────────────

    /// <summary>
    /// The argument list a <c>table(… ALL …)</c> intrinsic argument stands for (ISO §2023:15.3 — "When ALL is specified
    /// as a subscript, the effect is as if each table element associated with that subscript position were
    /// specified. The order … is from left to right, with the first … specification being the identifier with
    /// each subscript specified by the word ALL replaced by one, the next … the rightmost subscript specified by
    /// the word ALL incremented by one …"): a row-major enumeration over the ALL levels' RANGES, outermost first.
    /// Each level's range is read when the level is ENTERED, as a function of the outer indices — a fixed OCCURS
    /// count, an OCCURS DEPENDING table's current count, or a dynamic-capacity table's current capacity ("from 1 to
    /// the current capacity of the table"), which for a table nested inside another depends on the outer occurrence.
    /// <para>"The evaluation of an ALL subscript shall result in at least one argument, otherwise the result of the
    /// reference to the function-identifier is undefined." WiseOwl COBOL defines the undefined case as
    /// EC-ARGUMENT-FUNCTION (set when checking is on) and terminates the reference with that name either way — a
    /// zero-argument list is never handed to a body whose result over nothing is itself undefined.</para>
    /// <para><paramref name="lead"/>, when given, renders the FIRST element enumerated in place of
    /// <paramref name="element"/>: the table(ALL) is then the function's leading argument, and its first implicit
    /// element is argument-1 (the enumeration order above), which a body such as PRESENT-VALUE screens against its
    /// argument-1 value domain on the element's EXACT carrier (kb/Work PB1000) before it joins the binary64 list.
    /// Which element is first is known only here — an inner ALL level may range over nothing for the first outer
    /// occurrence — so the choice is made by the walk, not by the caller's subscripts.</para>
    /// </summary>
    public static T[] AllArgs<T>(Func<long[], long>[] counts, Func<long[], T> element, Func<long[], T>? lead = null)
    {
        var list = new List<T>();
        EnumerateOccurrences(counts, idx => list.Add(list.Count == 0 && lead is not null ? lead(idx) : element(idx)));
        RequireOccurrences(list.Count);
        return list.ToArray();
    }

    /// <summary>
    /// The STORAGE IMAGE of a whole table a statement references without the subscripts of its own level (X3.23-1985 USE FOR
    /// DEBUGGING general rule 24 c: DEBUG-CONTENTS "contains the content of the data item referenced by identifier-1"; kb/Work
    /// PB4855): the images of its CURRENT occurrences in storage order, the same row-major walk as <see cref="AllArgs{T}"/>,
    /// each level's range read as that level is entered. A table whose current count is zero (an OCCURS DEPENDING table at
    /// integer-1 = 0, a dynamic-capacity table at capacity zero) has the empty image; it is not an error, because this is the
    /// content of a data item and not an argument list.
    /// </summary>
    public static string AllImage(Func<long[], long>[] counts, Func<long[], string> element)
    {
        var image = new System.Text.StringBuilder();
        EnumerateOccurrences(counts, idx => image.Append(element(idx)));
        return image.ToString();
    }

    /// <summary>The one row-major walk over a table's ranges, outermost first, the rightmost level varying fastest; each level's
    /// range is a function of the outer indices, read when the level is entered.</summary>
    private static void EnumerateOccurrences(Func<long[], long>[] counts, Action<long[]> visit)
    {
        var idx = new long[counts.Length];
        void Walk(int level)
        {
            if (level == counts.Length) { visit(idx); return; }
            long n = counts[level](idx);
            for (long i = 1; i <= n; i++) { idx[level] = i; Walk(level + 1); }
        }
        Walk(0);
    }

    /// <summary>"The evaluation of an ALL subscript shall result in at least one argument, otherwise the result of the reference to the
    /// function-identifier is undefined" (ISO §2023:15.3 AR14)), defined here as EC-ARGUMENT-FUNCTION (set when checking is on, fatal,
    /// USE-dispatchable) ending the reference either way - raised by the enumeration that ranged over nothing (<see cref="AllArgs{T}"/>)
    /// and by the HELD form of the argument (<c>BoundHoldTableAll</c>) when its copy found nothing, which is the same evaluation, at the
    /// argument's place in the list (§2023:8.4.3.2.4 GR2), before a later argument's activation.</summary>
    public static void RequireOccurrences(long count)
    {
        if (count != 0) return;
        string why = "a table(ALL) intrinsic argument ranged over no occurrences — the ALL subscript shall result in at least one argument ("
            + ExceptionState.EditionRule("§1989:A.2.2 p5", "ISO §2023:15.3") + ")";
        ExceptionState.ArgumentError(why);                          // EC-ARGUMENT-FUNCTION when checking is on (fatal, USE-dispatchable)
        throw new CobolFatalException("EC-ARGUMENT-FUNCTION", why);  // the undefined case is defined LOUD, checking or not
    }

    /// <summary>⛔ A FIXED TABLE'S INITIAL ELEMENTS, built at run time: <paramref name="element"/> composes occurrence
    /// <c>o</c> (1..<paramref name="count"/>) afresh for every element, so an element that holds a nested table gets
    /// its own array and no two occurrences share storage. It is what lets the compiler emit a table's initial
    /// state in text proportional to its DESCRIPTION rather than to its element count (kb/Work PB1722: an array
    /// literal of one hundred million element initializers is what a `10000 x 10000` table used to compile to).</summary>
    public static T[] Fill<T>(int count, Func<int, T> element)
    {
        var all = new T[count];
        for (int i = 0; i < all.Length; i++) all[i] = element(i + 1);
        return all;
    }

    /// <summary>Whether an occurrence's RANK lies in the run <paramref name="lo"/>..<paramref name="hi"/> of a
    /// Format-2 (table) VALUE phrase AND is the run position that takes literal number <paramref name="residue"/>
    /// of <paramref name="modulus"/> (ISO §2023:13.18.63.4 GR13: the literals "are reused, in the order specified") — the
    /// run-time occurrence test of INITIALIZE … TO VALUE (§2023:14.9.20.4 GR6a.3).</summary>
    public static bool InSpan(long rank, long lo, long hi, int modulus, int residue) =>
        rank >= lo && rank <= hi && (modulus == 1 || (rank - lo) % modulus == residue);

    /// <summary>The intrinsic argument list assembled from written operands and <see cref="AllArgs{T}"/> enumerations,
    /// in source order — the ONE array a <c>params T[]</c> body receives.</summary>
    public static T[] ArgConcat<T>(params T[][] parts)
    {
        int n = 0;
        foreach (var p in parts) n += p.Length;
        var all = new T[n];
        int at = 0;
        foreach (var p in parts) { Array.Copy(p, 0, all, at, p.Length); at += p.Length; }
        return all;
    }

    /// <summary>Bind an evaluated argument list to a name inside an EXPRESSION (the C# analogue of <c>let</c>): the
    /// intrinsic renderers use it when a body needs the SAME enumerated list twice — MEAN's sum and its count, a
    /// leading positional argument and the tail — so a runtime table(ALL) enumeration is evaluated exactly once.</summary>
    public static R With<T, R>(T value, Func<T, R> body) => body(value);

    /// <summary>
    /// ⛔ THE TABLE SORT (ISO §2023:14.9.40 Format 2), run so that a COBOL EXCEPTION CONDITION RAISED BY THE KEY
    /// COMPARISON STILL REACHES THE STATEMENT GUARD.
    /// </summary>
    /// <remarks>
    /// <para>The sort is <c>OrderBy</c> with a <see cref="Comparison{T}"/> — stable, which is what §2023:14.9.40.4
    /// GR19c/GR3c require of equal keys. But the framework's array sort CATCHES anything a comparer throws and
    /// re-throws it as <see cref="InvalidOperationException"/> "Failed to compare two elements in the array",
    /// on the assumption that a throwing comparer is an inconsistent one. A COBOL comparer is not: §2023:14.9.40.4
    /// GR8 makes a key comparison follow the relation-condition rules, and §2023:14.6.13.2 GR2 makes REFERENCING a
    /// numeric key whose content is not valid set the fatal EC-DATA-INCOMPATIBLE. Wrapped, that fatal never
    /// matched the emitted statement guard's <c>catch (CobolFatalException)</c>, so a program with a USE
    /// declarative for the condition died with an unhandled .NET exception instead of running its declarative.
    /// MEASURED, not deduced (kb/Work PB230): a group MOVE plants "AB1" in a key, and the run unit aborted with
    /// <c>System.InvalidOperationException: Failed to compare two elements in the array</c>.</para>
    /// <para>So the wrapper is undone here, at the ONE place a COBOL comparer is handed to the framework, and the
    /// original is re-thrown with its stack intact (<see cref="ExceptionDispatchInfo"/>, never a bare
    /// <c>throw f</c>, which would reset it). Any future comparer raise — a float key's EC-DATA-NOT-FINITE, a
    /// locale collation condition — is covered by construction rather than by remembering to add an arm.</para>
    /// </remarks>
    /// <param name="occurrences">The table's CURRENT occurrences, and only those (§2023:14.9.40.4 GR20 — "The number of
    /// occurrences of table elements referenced by data-name-2 is determined by the rules in the OCCURS clause";
    /// kb/Work PB1174): the first data-name-1 elements of an OCCURS DEPENDING table (§2023:13.18.38.4 GR7), a
    /// dynamic-capacity table's current capacity (<see cref="CobolDynTable{T}.CurrentOccurrences"/>), the whole
    /// array of a fixed one. Sorting the physical array pulled the stale content past the current count into the
    /// table and pushed real elements out of it. The sorted occurrences are placed back into the same span (GR24).</param>
    /// <param name="compare">The statement's key comparer (GR2 significance, GR19 direction).</param>
    public static void SortInPlace<T>(Span<T> occurrences, Comparison<T> compare)
    {
        T[] sorted;
        try
        {
            sorted = [.. occurrences.ToArray().OrderBy(e => e, Comparer<T>.Create(compare))];
        }
        catch (InvalidOperationException ex) when (ex.InnerException is CobolFatalException fatal)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(fatal).Throw();
            throw;   // unreachable — Throw() does not return; present because the compiler cannot know that
        }
        sorted.CopyTo(occurrences);
    }
}
