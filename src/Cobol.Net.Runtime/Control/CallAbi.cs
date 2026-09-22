// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>How a CALL argument is passed (ISO §14.9.4 / §14.2.3 GR8–10).</summary>
public enum CobolPassMode
{
    /// <summary>BY REFERENCE — the callee operates as if the formal occupies the caller's storage (§14.2.3 GR8).</summary>
    Reference,
    /// <summary>BY CONTENT — a copy allocated at CALL initiation, then treated as if by reference (§14.2.3 GR9).</summary>
    Content,
    /// <summary>BY VALUE — a converted value copy (§14.2.3 GR10; COBOL-2002+).</summary>
    Value,
}

/// <summary>
/// One CALL argument crossing the opaque ABI (design D2): the pass mode, the carrier, and the DESCRIPTION of
/// the storage that carrier holds — the one fact the callee-side adapters need to reinterpret a native numeric
/// cell through a differently-scaled or character-shaped formal (the D5-sanctioned category boundary).
/// <para>⛔ THE DESCRIPTION IS A WHOLE <see cref="NumProfile"/>, NOT A (digits, scale) PAIR (kb/Work PB873). A
/// native cell holds a VALUE; the storage it stands for has a REPRESENTATION — the operational sign and where it
/// sits (§13.18.52), the usage's byte form (§13.18.60.4) — and a formal that sees the argument as characters
/// sees that representation: §14.2.3 GR8 "operates as if the formal parameter occupies the same storage area as
/// the argument", and GR9's first branch moves the argument into its record "without conversion". The pair this
/// replaced could spell only the digit run, so an image built from it was always UNSIGNED ZONED: <c>-12.34</c>
/// in a <c>PIC S9(4)V99</c> argument reached an image-carried formal as <c>00123D</c> (+12.34) where the storage
/// holds <c>00123M</c>, and a <c>COMP-5</c> argument reached a <c>PIC X(2)</c> formal as a zoned digit run
/// instead of its two binary bytes. Carrying the profile makes every attribute the image needs travel with it —
/// the next one (a national form, a new usage) is automatic.</para>
/// <para>When the activating element has performed GR9/GR10's COMPUTE into a record of the FORMAL's
/// description (<see cref="CobolArgAdapt.LandForFormal{T}"/>), the description it carries from then on IS the
/// formal's — GR11 resolves every reference to data-name-1 "in accordance with their description in the linkage
/// section" — so the one field answers both of GR9's branches.</para>
/// </summary>
/// <param name="Mode">The pass mode (ISO §14.9.4.4 GR5 transitivity resolved at bind time).</param>
/// <param name="Carrier">The storage carrier (<see cref="ManagedPointer.Null"/> for OMITTED, GR11).</param>
/// <param name="Num">The numeric description of the carried storage; null for character, group, pointer,
/// object-reference and index storage, whose carrier needs no numeric reinterpretation.</param>
public readonly record struct CobolArg(CobolPassMode Mode, ManagedPointer Carrier, NumProfile? Num)
{
    /// <summary>The carried storage's digit count; 0 when <see cref="Num"/> is null.</summary>
    public int Digits => Num?.Digits ?? 0;

    /// <summary>The carried storage's net scale (§13.18.40 — may be negative); 0 when <see cref="Num"/> is null.</summary>
    public int Scale => Num?.FractionScale ?? 0;
}

/// <summary>
/// The uniform program ABI every compiled program class implements (design D2 — the typed analog of the
/// rejected byte <c>Entry(ManagedPointer[])</c>). <see cref="Call"/> activates the program as a CALLed program
/// (positional formal mapping, §14.2.3 GR2); <see cref="Activate"/> runs it as the run-unit main program;
/// <see cref="CloseFiles"/> closes this program's file connectors (CANCEL §14.9.5 GR9 implicit CLOSE).
/// </summary>
public interface ICobolProgram
{
    /// <summary>Activate as a CALLed program: map <paramref name="args"/> positionally onto the LINKAGE formals
    /// (ISO §14.2.3 GR2 — correspondence is positional, never by name), run, and deliver the RETURNING value (if
    /// any) through <paramref name="returning"/> (§14.2.3 GR7).</summary>
    void Call(CobolArg[] args, ManagedPointer? returning);

    /// <summary>Activate as the run-unit's main program (no arguments; LINKAGE unbound, ISO §13.7.4 GR3).</summary>
    void Activate();

    /// <summary>Register this element's external descriptions and run the §14.8.4 conformance check
    /// (ISO §14.9.4.4 GR3e). It is an ACTIVATION-ATTEMPT step, not part of the activated element's execution:
    /// GR3e precedes GR3g's "control is transferred to the called program", and a violation makes "the program
    /// call ... not successful" — so the activation boundary calls it BEFORE <see cref="Call"/>, which is what
    /// lets the boundary mark everything escaping <see cref="Call"/> as post-transfer (GR3i). A unit with no
    /// external record or file connector, or with no enabling EC-EXTERNAL &gt;&gt;TURN in the group, emits no
    /// override and takes this no-op (zero scaffolding).</summary>
    void DescribeExternals() { }

    /// <summary>Close every file connector this program owns (CANCEL GR9 / run-unit termination §14.6.11).</summary>
    void CloseFiles();

    /// <summary>Select and execute this element's USE declarative for a NONFATAL exception condition raised at a
    /// RUNTIME site (ISO §14.6.13.1.4 #3 — "If there is an applicable USE statement in the source unit that
    /// specifies the exception-name associated with the exception condition … the associated declarative is
    /// executed"), returning the dispatch result protocol: <c>-1</c> completed normally, <c>-2</c> RESUME AT NEXT
    /// STATEMENT, <c>-3</c> no qualifying declarative, <c>≥ 0</c> RESUME AT that pc.
    /// <para>This is the §14.9.49.4 GR3 selection seen from OUTSIDE the generated code. A raise the emitter can
    /// place a dispatch AT (RAISE, an I-O status, an ON OVERFLOW-less STRING) never needs it; a condition detected
    /// INSIDE the runtime — an untranslatable code unit deep in an expression, a dynamic table growing under a
    /// receiving subscript, an inverted THRU range, a dynamic-length resize — has no statement-level node to hang
    /// a dispatch on, and before this channel existed it set the last exception status and returned, so its
    /// declarative never ran (kb/Work PB367b).</para>
    /// <para>The DEFAULT is "no qualifying declarative": an element with no Format-3 selection machinery answers
    /// it without emitting anything, which is what keeps the zero-scaffolding invariant true for every program
    /// that declares no USE AFTER EXCEPTION CONDITION declarative and every non-COBOL implementation of this
    /// interface.</para></summary>
    int NonfatalDispatch(string ec) => -3;
}

/// <summary>
/// Callee-side positional argument adapters (design D2/D5): each maps <c>args[i]</c> onto a formal parameter's
/// carrier shape. Same-shape carriers pass through untouched (fully typed aliasing); a category mismatch (e.g. a
/// caller <c>PIC X(4)</c> viewed by the callee as <c>PIC 9(4)</c>) builds a CONVERTING view over the caller's
/// storage — the one sanctioned transient-character boundary (design D5; legal COBOL exercised by NIST), never a
/// persisted byte image.
/// <para>TWO DIFFERENT FAILURES, TWO DIFFERENT ANSWERS (kb/Work PB615). A missing / OMITTED argument yields the
/// omitted carrier (<see cref="Omitted{T}"/>): §14.9.4.4 GR11 makes the omitted-argument condition true, and a
/// reference raises EC-PROGRAM-ARG-OMITTED under checking (GR12) and reads the type's benign empty value
/// without it. A SUPPLIED argument whose carrier the formal's adapter cannot read is NOT omitted — it is a
/// violation of the §14.8.2 conformance rules, and §14.9.4.4 GR3 d) answers it with "the program call is not successful"
/// (EC-PROGRAM-ARG-MISMATCH): <see cref="Unreadable{T}"/> fails the activation LOUD, never a silent zero
/// indistinguishable from an omitted argument.</para>
/// </summary>
public static class CobolArgAdapt
{
    /// <summary>True when argument <paramref name="i"/> was supplied and is not OMITTED (ISO §14.9.4.4 GR11 —
    /// the omitted-argument condition is the negation of this).</summary>
    public static bool Present(CobolArg[] args, int i) => i < args.Length && !args[i].Carrier.IsNull;

    /// <summary>Read any NATIVE NUMERIC carrier cell as its Int128-lane unscaled value, or null when the cell is
    /// not a native numeric (kb/Work R12 — the carrier set is the four <c>PicInfo.ClrType</c> integer carriers;
    /// a <c>UInt128</c> value beyond <see cref="Int128.MaxValue"/> passes as its container BITS, the same
    /// contract the R10 store path uses, so the typed write below reinterprets it exactly).</summary>
    private static Int128? ReadNumericCell(ManagedPointer p) => p switch
    {
        ManagedPointer<long> x => x.Value,
        ManagedPointer<ulong> x => (Int128)x.Value,
        ManagedPointer<Int128> x => x.Value,
        ManagedPointer<UInt128> x => unchecked((Int128)x.Value),
        _ => null,
    };

    /// <summary>Read any NATIVE FLOATING-POINT carrier cell as its binary64 value, or null when the cell is not
    /// one (kb/Work PB238 — the float lane joined this ABI's carrier vocabulary because a BY VALUE argument
    /// stopped being narrowed to the exact <c>Int128</c> lane at the CALLER; §14.2.3 GR10 makes the crossing
    /// "a COMPUTE statement without the ROUNDED phrase", and a caller-side integer cast performed a truncation
    /// the callee's own COMPUTE was supposed to perform at the FORMAL's scale — <c>1.5</c> reached a
    /// <c>PIC S9(3)V99</c> formal as <c>1.00</c>). It is a SECOND lane rather than a widening of
    /// <see cref="ReadNumericCell"/>: no <c>Int128</c> holds a fractional binary64 value, so the conversion has
    /// to happen where the destination scale is known.</summary>
    private static double? ReadRealCell(ManagedPointer p) => p switch
    {
        ManagedPointer<double> x => x.Value,
        ManagedPointer<float> x => x.Value,
        _ => null,
    };

    /// <summary>The write half of <see cref="ReadRealCell"/>; false when the cell is not a native float.</summary>
    private static bool WriteRealCell(ManagedPointer p, double v)
    {
        switch (p)
        {
            case ManagedPointer<double> x: x.Value = v; return true;
            case ManagedPointer<float> x: x.Value = (float)v; return true;
            default: return false;
        }
    }

    /// <summary>⛔ THE ONE NUMERIC LANDING OF THIS ABI (ISO §14.2.3 GR9/GR10; kb/Work PB288). Every numeric arm
    /// below reaches its receiving side through this: the crossing IS "a COMPUTE statement without the ROUNDED
    /// phrase" whose receiving operand is a data item of the FORMAL's description (GR9's prototyped/NESTED
    /// branch, GR10's allocated record), and GR11 resolves every reference to the formal through that same
    /// linkage description — so the GR8 aliasing view owes the identical landing, one value at a time, rather
    /// than a raw reinterpretation of the caller's bits.
    /// <para>The landing has TWO halves and both are load-bearing. (1) The scale alignment widens with STORE
    /// semantics (<see cref="CobolNum.RescaleStoreCap"/>): §14.7.5 case 3's overflow "after radix point
    /// alignment" is a receiver overflow, and with no SIZE ERROR phrase and checking off its documented
    /// disposition is the result's LOW-ORDER digits (CONFORMANCE.md DOC-A.1-70) — never the binary two's
    /// complement of an intermediate, which is not any rule. <c>BY CONTENT 1000000000000000000000000000000</c>
    /// into a <c>PIC S9(9)V9(9)</c> formal used to arrive as 873995514.006732800 because the unchecked
    /// <c>Rescale</c> formed 10^39 and wrapped. (2) The digit-capacity conformance
    /// (<see cref="CobolNum.Store"/>) then reduces to the formal's picture — WITHOUT it the
    /// <c>T.CreateTruncating</c> below is itself a BINARY truncation to the carrier, so an 18-digit argument
    /// viewed through a <c>PIC S9(4)V99</c> formal arrived as −9838.16 where the low-order digits are 5678.00.
    /// The float lane's <see cref="CobolNum.Store"/> composition was already right in <see cref="NumValue"/>
    /// and missing in <see cref="Num"/> — the two-arm shape this helper exists to make unrepeatable.</para></summary>
    /// <remarks><paramref name="toScale"/> is the emitted formal's <c>PicInfo.Scale</c> and
    /// <paramref name="receiver"/> is the profile emitted from the SAME <c>PicInfo</c>, whose
    /// <c>FractionDigits</c> is that same <c>Scale</c> (<c>PicInfo.ProfileInitializer</c>) — so
    /// <see cref="CobolNum.Store"/>'s own internal rescale is the identity here and cannot re-introduce an
    /// unchecked widening behind this one. Keep them emitted from one <c>PicInfo</c>.</remarks>
    private static Int128 Land(Int128 unscaled, int fromScale, int toScale, in NumProfile receiver) =>
        CobolNum.Store(CobolNum.RescaleStoreCap(unscaled, fromScale, toScale, CobolRounding.Truncation),
                       toScale, receiver, CobolRounding.Truncation);

    /// <summary>The binary64 lane of <see cref="Land(Int128, int, int, in NumProfile)"/>: the quantization to
    /// the receiving description's scale happens HERE (kb/Work PB238), and past the <c>Int128</c> carrier
    /// <see cref="CobolFloat.ToScaledUnchecked"/> keeps supplying the exact expansion's low-order digits
    /// (kb/Work PB77) for the same <see cref="CobolNum.Store"/> to reduce to the picture.</summary>
    private static Int128 Land(double value, int toScale, in NumProfile receiver) =>
        CobolNum.Store(CobolFloat.ToScaledUnchecked(value, toScale, CobolRounding.Truncation),
                       toScale, receiver, CobolRounding.Truncation);

    /// <summary>The EC-SIZE-CHECKED twin of <see cref="Land(Int128, int, int, in NumProfile)"/> — the SAME
    /// §14.2.3 GR9/GR10 COMPUTE, with §14.7.5's no-phrase rule 4 in force instead of DOC-A.1-70's low-order
    /// digits (kb/Work PB640). It exists only on the ACTIVATING side (<see cref="LandForFormal"/>): the
    /// crossing's COMPUTE is performed by the activating runtime element — GR9/GR10 say the allocated record is
    /// "allocated by the activating runtime element during the process of initiating the activation" — so the
    /// checking state that decides between the two dispositions is the ACTIVATING element's, and so are the
    /// declaratives that see the raise. <c>toScale</c> is not a parameter here because it can only ever be
    /// <c>receiver.FractionScale</c> (the remark on the unchecked overload), and
    /// <see cref="CobolNum.StoreOrRaise(Int128, int, in NumProfile, CobolRounding)"/> reads it from the
    /// profile — so the checked landing cannot be handed a scale the capacity check disagrees with.</summary>
    private static Int128 LandChecked(Int128 unscaled, int fromScale, in NumProfile receiver) =>
        CobolNum.StoreOrRaise(unscaled, fromScale, receiver, CobolRounding.Truncation);

    /// <summary>The binary64 lane of <see cref="LandChecked(Int128, int, in NumProfile)"/>. The quantizer is
    /// <see cref="CobolFloat.ToScaled"/>, NOT the unchecked twin: past the <c>Int128</c> carrier
    /// <c>ToScaledUnchecked</c> hands back the exact expansion's LOW-ORDER digits (kb/Work PB77), which is the
    /// checking-off disposition and would pass the capacity test as a small value — the §14.7.5 case-3 fact is
    /// about the ALGEBRAIC result, so the checked lane takes the saturating landing the capacity test then
    /// rejects.</summary>
    private static Int128 LandChecked(double value, int toScale, in NumProfile receiver) =>
        CobolNum.StoreOrRaise(CobolFloat.ToScaled(value, toScale, CobolRounding.Truncation),
                              toScale, receiver, CobolRounding.Truncation);

    /// <summary>The value of one argument landed into a record of the FORMAL's description (ISO §14.2.3
    /// GR9/GR10's "COMPUTE statement without the ROUNDED phrase"), or null when the carrier is outside this
    /// ABI's numeric vocabulary — the drift boundary <c>CallAbiNumericCarrierDriftTests</c> pins, which the
    /// callers turn into the §14.9.4.4 GR12 omitted carrier rather than a reinterpretation of storage.
    /// <para>⛔ ONE SCALAR LANDING FOR BOTH SIDES OF THE BOUNDARY (kb/Work PB640). The activating side
    /// (<see cref="LandForFormal"/>, which is where GR9/GR10 put the COMPUTE whenever the formal's description
    /// is knowable there) and the activated side (<see cref="NumValue"/>, the residual landing for a crossing
    /// whose formal the caller could not know) are the same three carrier arms and the same
    /// <see cref="Land(Int128, int, int, in NumProfile)"/>; only <paramref name="checking"/> differs.</para></summary>
    private static Int128? LandScalar(in CobolArg a, in NumProfile formal, int formalScale, bool checking) =>
        a.Carrier switch
        {
            // §14.2.3 GR10's COMPUTE from the FLOAT lane (kb/Work PB238): the quantization to the formal's
            // scale happens at the RECEIVER and truncates — exactly what an un-ROUNDED COMPUTE does.
            { } rp when ReadRealCell(rp) is { } rv =>
                checking ? LandChecked(rv, formalScale, formal) : Land(rv, formalScale, formal),
            { } np when ReadNumericCell(np) is { } nv =>
                checking ? LandChecked(nv, a.Scale, formal) : Land(nv, a.Scale, formalScale, formal),
            // An IMAGE-carried FIXED-POINT argument (a redefined item, a Tier-B window) is the COMPUTE's
            // sending operand, so its VALUE is its carrier text read through ITS OWN description (kb/Work PB873)
            // — decoding it through the formal's profile read the argument's sign and scale as if they were the
            // formal's. The carrier text is the item's operand text (`CallEmitter.CallStringRead` →
            // `OperandText.FieldImage`), which is what ParseDisplay reads.
            ManagedPointer<string> sp when a.Num is { ByteForm: not (NumericByteForm.None or NumericByteForm.Ieee32 or NumericByteForm.Ieee64) } d =>
                checking ? LandChecked(CobolNum.ParseDisplay(sp.Value, d), d.FractionScale, formal)
                         : Land(CobolNum.ParseDisplay(sp.Value, d), d.FractionScale, formalScale, formal),
            // A CHARACTER argument has no numeric description of its own: it decodes through the formal's
            // profile; the rescale is then the identity and only the capacity conformance remains.
            ManagedPointer<string> sp =>
                checking ? LandChecked(CobolNum.ParseDisplay(sp.Value, formal), formalScale, formal)
                         : Land(CobolNum.ParseDisplay(sp.Value, formal), formalScale, formalScale, formal),
            _ => null,
        };

    /// <summary>The CHARACTER IMAGE of a native numeric cell under the description <paramref name="d"/> it
    /// carries — the bytes the storage holds (§14.2.3 GR8: the formal "occupies the same storage area as the
    /// argument"), through THE record-image codec, so the operational sign, its position and the usage's byte
    /// form all appear exactly as they do in storage (kb/Work PB873). Null when the cell is not of the lane
    /// <paramref name="d"/> describes.</summary>
    private static string? CellImage(ManagedPointer cell, in NumProfile d) => d.ByteForm switch
    {
        NumericByteForm.Ieee32 or NumericByteForm.Ieee64 =>
            ReadRealCell(cell) is { } r ? CobolNum.FormatImageFloat(r, d) : null,
        NumericByteForm.None => null,
        _ => ReadNumericCell(cell) is { } n ? CobolNum.FormatImage(n, d) : null,
    };

    /// <summary>The write half of <see cref="CellImage"/>: decode <paramref name="image"/> through the SAME
    /// description and store the value into the cell.</summary>
    private static void WriteCellImage(ManagedPointer cell, string image, in NumProfile d)
    {
        if (d.ByteForm is NumericByteForm.Ieee32 or NumericByteForm.Ieee64)
            WriteRealCell(cell, CobolNum.ParseImageFloat(image, d));
        else
            WriteNumericCell(cell, CobolNum.ParseImage(image, d));
    }

    /// <summary>The argument's value as binary64 — the sending operand of GR10's COMPUTE when the receiving
    /// description is a FLOATING-POINT one (§14.6.8.3 GR1: the IEEE receiver takes the algebraic value, so
    /// there is no scale to quantize to). Null when the carrier is outside the numeric vocabulary.</summary>
    private static double? ArgDouble(in CobolArg a) => a.Carrier switch
    {
        { } rp when ReadRealCell(rp) is { } rv => rv,
        { } np when ReadNumericCell(np) is { } nv => CobolFloat.ScaledToDouble(nv, a.Scale),
        ManagedPointer<string> sp when a.Num is { ByteForm: not (NumericByteForm.None or NumericByteForm.Ieee32 or NumericByteForm.Ieee64) } d =>
            CobolFloat.ScaledToDouble(CobolNum.ParseDisplay(sp.Value, d), d.FractionScale),
        _ => null,
    };

    /// <summary>⛔ THE ACTIVATING ELEMENT'S §14.2.3 GR9/GR10 CROSSING (kb/Work PB640) — the caller-side half of
    /// this ABI's numeric landing, emitted by <c>CallEmitter.ArgText</c> around the argument carrier it just
    /// built, for every BY CONTENT / BY VALUE argument whose corresponding formal parameter is a fixed-point
    /// numeric item KNOWN AT THE CALL SITE.
    /// <para>WHY THE CALLER AND NOT THE CALLEE. GR9 and GR10 both say the allocated record is "allocated by the
    /// activating runtime element during the process of initiating the activation", and it is the ACTIVATING
    /// element that supplies the COMPUTE's sending operand — so every consequence of that COMPUTE is the
    /// activating element's: its <c>&gt;&gt;TURN EC-SIZE CHECKING</c> state decides whether a §14.7.5 case-3
    /// overflow raises (enablement is a property of a compilation group, §14.6.13.1.1), its USE declaratives
    /// are the ones §14.6.13.1.3 selects over, and §14.9.4.4 GR3g transfers control to the called program only
    /// "if a fatal exception condition has not been raised" — which a landing performed after the transfer can
    /// no longer honour. Before PB640 the whole landing ran callee-side, so an argument that overflowed the
    /// formal's description under checking arrived as DOC-A.1-70's low-order digits with no raise at all.</para>
    /// <para>WHEN THE CALLER CANNOT. GR9's FIRST branch — a program with no program-specifier in the activating
    /// element's REPOSITORY paragraph and no NESTED phrase — allocates a record "of the same length as the
    /// argument" and moves it "without conversion", so there is no COMPUTE to perform and no formal description
    /// to perform it against. The set of crossings that ARE a COMPUTE (a prototyped program, a NESTED CALL, a
    /// method, a function) is exactly the set whose formal is knowable at the call site; §14.8.2.3.3 draws the
    /// same partition for conformance. <see cref="NumValue"/> and <see cref="Num"/> therefore keep their own
    /// landing for the residue, and for an already-landed argument it is the identity: the carrier IS the
    /// formal's, at the formal's scale, within the formal's capacity.</para>
    /// <para><paramref name="checking"/> is decided at COMPILE time from the CALL statement's own TURN state —
    /// the same kernel selection the arithmetic store makes — so a unit with checking off emits the landing it
    /// always had.</para></summary>
    /// <typeparam name="T">The formal's carrier (<c>PicInfo.ClrType</c>), so the callee's own adapter sees a
    /// same-carrier same-scale argument and aliases it (§14.2.3 GR9's "treated … as if it were passed by
    /// reference") rather than converting it a second time.</typeparam>
    public static CobolArg LandForFormal<T>(CobolArg arg, NumProfile formal, int formalScale, bool checking)
        where T : struct, System.Numerics.INumberBase<T>
    {
        // OMITTED (§14.9.4.4 GR11): there is no argument to be the COMPUTE's sending operand.
        if (arg.Carrier.IsNull) return arg;
        // A carrier outside the numeric vocabulary (a variable-length group, an object/pointer handle) is not a
        // numeric crossing to land — it reaches the callee unchanged and takes that side's own arm.
        if (LandScalar(arg, formal, formalScale, checking) is not { } v) return arg;
        T landed = T.CreateTruncating(v);
        // The allocated record IS the argument from here on (GR9's last sentence), so the meta the ABI carries
        // becomes the record's own description — which is what makes the callee-side landing the identity.
        // ⚡ The CONFORMING case — same carrier, same scale, inside the formal's capacity — keeps the cell the
        // activating element has ALREADY allocated (the BY CONTENT / BY VALUE snapshot this CALL site built),
        // because the COMPUTE is the identity over it and a second cell would be pure garbage on the call path.
        // CobolArg is a readonly record struct, so the meta update itself allocates nothing either way.
        // The formal's WHOLE description, sign and byte form included (kb/Work PB873): an image-carried formal
        // reads the landed record through it, and the pair this replaced carried only (Digits, Scale).
        if (arg.Carrier is ManagedPointer<T> same && arg.Scale == formalScale && same.Value == landed)
            return arg with { Num = formal };
        return arg with { Carrier = ManagedPointer<T>.Cell(landed), Num = formal };
    }

    /// <summary>The write half of <see cref="ReadNumericCell"/>; false when the cell is not a native numeric.</summary>
    private static bool WriteNumericCell(ManagedPointer p, Int128 v)
    {
        switch (p)
        {
            case ManagedPointer<long> x: x.Value = unchecked((long)v); return true;
            case ManagedPointer<ulong> x: x.Value = unchecked((ulong)v); return true;
            case ManagedPointer<Int128> x: x.Value = v; return true;
            case ManagedPointer<UInt128> x: x.Value = unchecked((UInt128)v); return true;
            default: return false;
        }
    }

    /// <summary>Adapt argument <paramref name="i"/> to a NUMERIC formal described by <paramref name="formal"/>
    /// (the callee's profile) at <paramref name="formalScale"/>. GENERIC over the formal's CARRIER
    /// (<c>long</c> / <c>ulong</c> / <c>Int128</c> / <c>UInt128</c> — <c>PicInfo.ClrType</c>'s integer set;
    /// kb/Work R12: the wide and unsigned tiers used to be routed onto a STRING crossing whose write half was
    /// never implemented — the generated C# did not compile — while the callee side hardcoded a
    /// <c>ManagedPointer&lt;long&gt;</c> cell its own carrier-typed reads could not use). A same-carrier
    /// same-scale argument aliases directly; a different scale or different carrier gets a converting view over
    /// the SAME storage (§14.2.3 GR8); a character carrier gets the zoned decode/encode view through the
    /// callee's profile — the D5 boundary. Conversions ride the Int128 lane with the R10 bits contract at the
    /// UInt128 ends, so a full-container value crosses losslessly between same-carrier cells.</summary>
    public static ManagedPointer<T> Num<T>(CobolArg[] args, int i, NumProfile formal, int formalScale)
        where T : struct, System.Numerics.INumberBase<T>
    {
        if (!Present(args, i)) return Omitted<T>(i);
        switch (args[i].Carrier)
        {
            case ManagedPointer<T> tp when args[i].Scale == formalScale:
                return tp;   // same carrier, same scale — pure typed aliasing (the common conforming case)
            case ManagedPointer<string> sp:
                // The D5 boundary: the caller's CHARACTER storage viewed as the callee's zoned numeric — decode
                // and re-encode through the callee's profile on each access (same storage area, §14.2.3 GR8).
                return ManagedPointer<T>.OverField(
                    // Through the shared Land, like every other arm: the decode is already at the formal's
                    // scale so the rescale is the identity, but the capacity conformance must not be the one
                    // arm that skips it (§14.2.3 GR11 — every reference resolves through the SAME description).
                    () => T.CreateTruncating(Land(CobolNum.ParseDisplay(sp.Value, formal), formalScale, formalScale, formal)),
                    v => sp.Value = CobolNum.FormatDisplay(Int128.CreateTruncating(v), formal));
            case { } rp when ReadRealCell(rp) is not null:
                // A native FLOAT cell viewed through a fixed-point formal (kb/Work PB238): the same §14.2.3 GR8
                // converting view, on the float lane, because no Int128 holds the fractional value. The scale
                // conversion is the receiver's, in BOTH directions — read lands through the formal's own
                // description (§14.2.3 GR9/GR10 + GR11, the shared Land), write un-scales back into the caller's
                // float.
                return ManagedPointer<T>.OverField(
                    () => T.CreateTruncating(Land(ReadRealCell(rp)!.Value, formalScale, formal)),
                    v => WriteRealCell(rp, CobolFloat.ScaledToDouble(Int128.CreateTruncating(v), formalScale)));
            case { } np when ReadNumericCell(np) is not null:
            {
                // A native numeric cell of a DIFFERENT carrier or scale: a converting view over the caller's
                // storage, landed through the formal's description on every read (§14.2.3 GR11 — the shared
                // Land; before kb/Work PB288 this read an UNCHECKED Rescale straight into T.CreateTruncating,
                // so both the scale alignment and the carrier narrowing were binary wraps).
                // Same-scale cross-carrier reads/writes are bit-faithful through the Int128 lane.
                int callerScale = args[i].Scale;
                return ManagedPointer<T>.OverField(
                    () => T.CreateTruncating(Land(ReadNumericCell(np)!.Value, callerScale, formalScale, formal)),
                    // The write-back's receiving side is the CALLER's item, whose capacity discipline is its own
                    // carrier's (WriteNumericCell); only the scale alignment is this ABI's, and it takes the same
                    // store-semantics widening rather than the wrapping one.
                    v => WriteNumericCell(np, CobolNum.RescaleStoreCap(Int128.CreateTruncating(v), formalScale, callerScale, CobolRounding.Truncation)));
            }
            default:
                return Unreadable<T>(args, i, "a numeric formal");
        }
    }

    /// <summary>Adapt argument <paramref name="i"/> to a CHARACTER formal of <paramref name="width"/> characters.
    /// A character carrier gets a width-window view: reads are the first <paramref name="width"/> positions
    /// (space-padded when the caller's storage is shorter); writes SPLICE into the caller's storage, preserving
    /// the caller's own width invariant (§14.2.3 GR8 — the callee touches only its formal's character positions).
    /// A native-<c>long</c> carrier gets a digit-image view via the caller's digit meta (D5 boundary).
    /// <para><paramref name="width"/> = <c>-1</c> is the ANY LENGTH mode (ISO §13.18.2 GR1): the formal's length
    /// IS the caller's argument length, so the callee sees the caller's FULL string (a zero-length argument
    /// yields the zero-length item, GR1a) and every write re-fits to the argument's CURRENT length (GR1b — the
    /// item behaves as n repetitions of its picture symbol, n fixed by the activation).</para></summary>
    public static ManagedPointer<string> Text(CobolArg[] args, int i, int width)
    {
        if (!Present(args, i)) return Omitted<string>(i);
        switch (args[i].Carrier)
        {
            case ManagedPointer<string> sp when width < 0:   // ANY LENGTH (§13.18.2 GR1) — the full-string view
                return ManagedPointer<string>.OverField(
                    () => sp.Value ?? "",
                    v => sp.Value = CobolString.Store(v, sp.Value?.Length ?? 0));
            case ManagedPointer<string> sp:
                return ManagedPointer<string>.OverField(
                    () => CobolString.Store(sp.Value, width),
                    v => sp.Value = CobolString.SpliceInto(sp.Value, 1, Math.Min(width, sp.Value?.Length ?? width), v));
            case { } np when args[i].Num is { } d && CellImage(np, d) is { } image:
            {
                // ⛔ THE STORAGE'S OWN IMAGE (kb/Work PB873; §14.2.3 GR8 — "operates as if the formal parameter
                // occupies the same storage area as the argument"; GR9's first branch moves it "without
                // conversion"). The image is the carried description's record image — sign, sign position and
                // byte form included — so a signed argument keeps its operational sign, a SIGN SEPARATE one its
                // extra position, and a binary/packed one its bytes. After GR9/GR10's COMPUTE the carried
                // description IS the formal's (LandForFormal), so the same arm serves both branches.
                // ANY LENGTH (width -1): the view width is the argument's own image width — n follows the
                // ARGUMENT's description (§13.18.2 GR1), never the formal's one-symbol picture.
                int viewWidth = width < 0 ? image.Length : width;
                return ManagedPointer<string>.OverField(
                    () => CobolString.Store(CellImage(np, d), viewWidth),
                    // A store touches only the formal's character positions (GR8) — splice into the CURRENT
                    // image, exactly as the character arm above splices into the caller's string.
                    v =>
                    {
                        string current = CellImage(np, d)!;
                        WriteCellImage(np, CobolString.SpliceInto(current, 1, Math.Min(viewWidth, current.Length), v), d);
                    });
            }
            case { } np when args[i].Num is null && ReadNumericCell(np) is not null:
            {
                // A native cell that carries NO numeric description — a USAGE INDEX item, whose storage
                // description has no digit positions for a profile to state: its image stays the unsigned digit
                // run of the view width, the representation this ABI has always given it.
                int w = Math.Max(1, width);
                var prof = new NumProfile
                {
                    Digits = w, FractionDigits = 0, Signed = false,
                    Truncation = NumericTruncation.DigitCount, ByteForm = NumericByteForm.Zoned,
                };
                return ManagedPointer<string>.OverField(
                    () => CobolNum.FormatDisplay(ReadNumericCell(np)!.Value, prof),
                    v => WriteNumericCell(np, CobolNum.ParseDisplay(v, prof)));
            }
            default:
                return Unreadable<string>(args, i, $"a character formal of {width} position(s)");
        }
    }

    /// <summary>Adapt argument <paramref name="i"/> to a BY VALUE NUMERIC formal (ISO §14.2.3 GR10): the activated
    /// element operates on "the record in the linkage section … allocated by the activating runtime element" — a
    /// data item OF THE FORMAL'S OWN DESCRIPTION that does NOT alias the argument, filled as if by "a COMPUTE
    /// statement without the ROUNDED phrase" with the argument as the sending operand. Realized as a DETACHED
    /// cell: the argument's value is rescaled to the formal's scale (truncation — the un-ROUNDED COMPUTE) and
    /// conformed to the formal's digit capacity via <see cref="CobolNum.Store"/>; the callee's stores reach only
    /// the cell, never the caller's storage (contrast <see cref="Num"/>, the §14.2.3 GR8 aliasing view).</summary>
    /// <remarks>⛔ THE RESIDUAL LANDING, NOT THE ONLY ONE (kb/Work PB640). When the activating element could
    /// know the formal's description it has ALREADY performed this COMPUTE — <see cref="LandForFormal"/>, which
    /// is where GR9/GR10 put it — and the argument arrives on the formal's own carrier at the formal's own
    /// scale, within its capacity, so the three arms below are the identity. This side stays because GR9's
    /// first branch (a non-prototyped, non-NESTED program CALL) gives the caller no formal to land against and
    /// because a non-COBOL activator supplies whatever it supplies; it shares
    /// <see cref="LandScalar"/> with the activating side so the two cannot answer differently.</remarks>
    public static ManagedPointer<T> NumValue<T>(CobolArg[] args, int i, NumProfile formal, int formalScale)
        where T : struct, System.Numerics.INumberBase<T> =>
        // Land's 16-byte-unsigned result is container BITS (R10); CreateTruncating reinterprets them exactly.
        !Present(args, i) ? Omitted<T>(i)
        : LandScalar(args[i], formal, formalScale, checking: false) is { } v
            ? ManagedPointer<T>.Cell(T.CreateTruncating(v))
            : Unreadable<T>(args, i, "a BY VALUE numeric formal");

    /// <summary>Adapt argument <paramref name="i"/> to a BY VALUE formal whose callee-side storage is a CHARACTER
    /// image of <paramref name="width"/> positions (a REDEFINED numeric formal — still class numeric, §14.2.2
    /// SR2-legal, but image-carried): the same §14.2.3 GR10 detached copy as <see cref="NumValue"/>, in image
    /// form. Writes reach only the cell (contrast <see cref="Text"/>, the GR8 splice-through view).
    /// <para>⛔ THE RECORD IS OF THE FORMAL'S DESCRIPTION (kb/Work PB873). GR10's record is "allocated by the
    /// activating runtime element" and filled by "a COMPUTE statement without the ROUNDED phrase" whose receiving
    /// operand is that record — so its image is the LANDED value in the formal's own representation
    /// (<paramref name="formal"/>), never the argument's digit run under a hard-coded unsigned profile, which
    /// is what dropped the sign of <c>-12.34</c>. <paramref name="formal"/> is null only for a formal with no
    /// numeric description, which takes the argument's own image.</para></summary>
    public static ManagedPointer<string> TextValue(CobolArg[] args, int i, int width, NumProfile? formal, int formalScale)
    {
        if (!Present(args, i)) return Omitted<string>(i);
        if (formal is { } f)
        {
            string? image = f.ByteForm is NumericByteForm.Ieee32 or NumericByteForm.Ieee64
                ? ArgDouble(args[i]) is { } dv ? CobolNum.FormatImageFloat(dv, f) : null
                : LandScalar(args[i], f, formalScale, checking: false) is { } v ? CobolNum.FormatImage(v, f) : null;
            return image is null
                ? Unreadable<string>(args, i, "a BY VALUE numeric formal")
                : ManagedPointer<string>.Cell(CobolString.Store(image, width));
        }
        return args[i].Carrier switch
        {
            ManagedPointer<string> sp => ManagedPointer<string>.Cell(CobolString.Store(sp.Value, width)),
            { } np when args[i].Num is { } d && CellImage(np, d) is { } img =>
                ManagedPointer<string>.Cell(CobolString.Store(img, width)),
            _ => Unreadable<string>(args, i, $"a BY VALUE character formal of {width} position(s)"),
        };
    }

    /// <summary>Adapt argument <paramref name="i"/> to a VARIABLE-LENGTH GROUP formal (ISO §14.8.2.2's
    /// compatibility sentence via §14.9.4.3 SR25; kb/Work PB204). The carrier IS the
    /// <see cref="CobolVarGroup"/> the caller built, aliased whole: unlike <see cref="Text"/> there is no width
    /// window to apply here, because the fixed run and the component list are BOTH re-fitted by the receiving
    /// group's own emitted distributor — which knows its own geometry and is the only thing that can. A
    /// non-var-group carrier means the sides disagreed about the crossing shape, which §8.5.1.12
    /// compatibility is checked at bind precisely to prevent; it degrades to the omitted carrier rather than
    /// silently reinterpreting storage.</summary>
    public static ManagedPointer<CobolVarGroup> VarGroup(CobolArg[] args, int i)
    {
        if (!Present(args, i)) return Omitted<CobolVarGroup>(i);
        return args[i].Carrier is ManagedPointer<CobolVarGroup> vp ? vp : Unreadable<CobolVarGroup>(args, i, "a variable-length group formal");
    }

    /// <summary>Adapt argument <paramref name="i"/> to a DYNAMIC LENGTH formal (ISO §13.18.19; kb/Work PB165).
    /// The THIRD length regime beside the fixed window and ANY LENGTH's activation-fixed one, and it had no arm
    /// at all: a dynamic-length formal took <see cref="Text"/> at its PICTURE length, which §13.18.19.3 SR1
    /// pins at exactly ONE symbol — so every such crossing delivered one character (measured: a 7-character
    /// argument arrived as <c>LEN=1</c>, and the callee's store spliced one character back into the caller's
    /// seven).
    /// <para>The view is the caller's FULL string, and a store carries §8.5.1.10.4's dynamic-length semantics
    /// through the ONE store helper: the new content replaces the old, the new length IS the sending length —
    /// never padded, minimum zero (§13.18.19.4 GR1) — truncated on the right at <paramref name="limit"/>
    /// (the item's §8.5.1.10.1 maximum size — GR2's LIMIT phrase bounded by the implementor maximum).</para>
    /// <para>BY REFERENCE this is §14.2.3 GR8's shared storage area, so the varying length is the CALLER's item
    /// varying. BY CONTENT / BY VALUE the caller already snapshotted the argument into a detached cell, and
    /// §14.2.3 GR9's second regime describes exactly this record — "a dynamic-length elementary item of the
    /// same category and described with the same dynamic-length-structure-name as the formal parameter" — with
    /// the argument moved into it, which is what the full-string view plus the dynamic store performs.</para>
    /// </summary>
    public static ManagedPointer<string> DynText(CobolArg[] args, int i, int limit)
    {
        if (!Present(args, i)) return Omitted<string>(i);
        return args[i].Carrier switch
        {
            ManagedPointer<string> sp => ManagedPointer<string>.OverField(
                () => sp.Value ?? "",
                v => sp.Value = CobolDynString.Store(v, limit)),
            // A numeric carrier reaching a dynamic-length formal is not a crossing to invent: §14.8.2.3.2
            // rule 2 requires the same DYNAMIC LENGTH and PICTURE clauses BY REFERENCE, and §14.8.2.3.3's MOVE
            // rules give a numeric sender an alphanumeric receiver only through its digit image — which is a
            // FIXED width and so contradicts the receiver's varying one. It takes the loud omitted carrier.
            _ => Unreadable<string>(args, i, "a dynamic-length formal"),
        };
    }

    /// <summary>The BY VALUE / BY CONTENT twin of <see cref="VarGroup"/> (ISO §14.2.3 GR9/GR10 — a copy
    /// allocated by the activating element): a DETACHED cell holding the argument's carrier value, so the
    /// callee's stores never reach the caller's storage.</summary>
    public static ManagedPointer<CobolVarGroup> VarGroupValue(CobolArg[] args, int i)
    {
        if (!Present(args, i)) return Omitted<CobolVarGroup>(i);
        return args[i].Carrier is ManagedPointer<CobolVarGroup> vp
            ? ManagedPointer<CobolVarGroup>.Cell(vp.Value ?? CobolVarGroup.Empty)
            : Unreadable<CobolVarGroup>(args, i, "a BY VALUE variable-length group formal");
    }

    /// <summary>Adapt argument <paramref name="i"/> to a MANAGED-SLOT formal — a formal of class pointer or
    /// class object-reference, whose value is a managed reference and has no byte image at all (kb/Work PB663;
    /// the <c>SlotWindow.CarriedBySlot</c> population, which is where the compiler decides the same thing about
    /// storage). It is the FOURTH crossing form beside the native cell, the character image and the
    /// variable-length carrier, and the one that had no arm: a pointer formal took <see cref="Text"/> and
    /// arrived as a <c>ManagedPointer&lt;string&gt;</c> space image, which is not merely the wrong value — the
    /// generated C# does not compile (CS1503 on the first reference).
    /// <para>The adaptation is pure ALIASING and admits exactly the same carrier type, because §14.8.2.3.2
    /// leaves no crossing to invent here: <i>"If either the argument or the formal parameter is of class
    /// pointer, the corresponding formal parameter or argument shall be of class pointer and the corresponding
    /// items shall be of the same category"</i>, and its object-reference rules 1–3 likewise force the same
    /// universal/interface-name/object-class-name on both sides. Same category plus same class means the same
    /// <c>PicInfo.ClrType</c>, so a conforming pairing IS a same-<c>T</c> carrier (§14.2.3 GR8's "same storage
    /// area", realized with zero indirection). A carrier of any other shape means the two sides disagreed about
    /// the crossing — it degrades to the loud omitted carrier rather than reinterpreting storage, the same way
    /// <see cref="VarGroup"/> does.</para></summary>
    public static ManagedPointer<T> Slot<T>(CobolArg[] args, int i)
    {
        if (!Present(args, i)) return Omitted<T>(i);
        return args[i].Carrier is ManagedPointer<T> mp ? mp : Unreadable<T>(args, i, "a pointer or object-reference formal");
    }

    /// <summary>The BY VALUE / BY CONTENT twin of <see cref="Slot{T}"/> (ISO §14.2.3 GR10 — a record "allocated
    /// by the activating runtime element", the argument its sending operand in <i>"a SET statement"</i> when the
    /// formal is of class object or pointer): a DETACHED cell holding the argument's reference value, so the
    /// callee's stores never reach the caller's storage. That SET of one pointer (or object reference) from
    /// another of the same category IS the reference copy this cell performs — there is no conversion for it
    /// to apply, which is why the numeric lane's landing machinery has no counterpart here.</summary>
    public static ManagedPointer<T> SlotValue<T>(CobolArg[] args, int i)
    {
        if (!Present(args, i)) return Omitted<T>(i);
        return args[i].Carrier is ManagedPointer<T> mp ? ManagedPointer<T>.Cell(mp.Value) : Unreadable<T>(args, i, "a BY VALUE pointer or object-reference formal");
    }

    /// <summary>Deliver a RETURNING value to the caller's RETURNING carrier (ISO §14.2.3 GR7 — at termination the
    /// returning item's value transfers to the activating element's RETURNING identifier). Null-tolerant: a CALL
    /// without RETURNING discards the value (deep-dive edge case). The overload set spans the four native
    /// carriers (kb/Work R12); a cross-carrier delivery rides the numeric-cell pair (Int128 lane, bits at the
    /// UInt128 ends).</summary>
    public static void StoreReturn(ManagedPointer? ret, long value) => StoreReturnNum(ret, value);

    /// <inheritdoc cref="StoreReturn(ManagedPointer?, long)"/>
    public static void StoreReturn(ManagedPointer? ret, ulong value) => StoreReturnNum(ret, (Int128)value);

    /// <inheritdoc cref="StoreReturn(ManagedPointer?, long)"/>
    public static void StoreReturn(ManagedPointer? ret, Int128 value) => StoreReturnNum(ret, value);

    /// <inheritdoc cref="StoreReturn(ManagedPointer?, long)"/>
    /// <remarks>The string leg renders the UNSIGNED value's own text BEFORE the bits reinterpretation — the
    /// Int128 lane carries a top-half UInt128 as negative bits, which is exactly right for a typed cell and
    /// exactly wrong for a character image.</remarks>
    public static void StoreReturn(ManagedPointer? ret, UInt128 value)
    {
        if (ret is ManagedPointer<string> sp) { sp.Value = value.ToString(); return; }
        StoreReturnNum(ret, unchecked((Int128)value));
    }

    private static void StoreReturnNum(ManagedPointer? ret, Int128 value)
    {
        if (ret is null) return;
        if (WriteNumericCell(ret, value)) return;
        if (WriteRealCell(ret, (double)value)) return;
        if (ret is ManagedPointer<string> sp) { sp.Value = value.ToString(); return; }
        Undeliverable(ret, $"the numeric result {value}");
    }

    /// <summary>String-shaped RETURNING delivery (see <see cref="StoreReturn(ManagedPointer?, long)"/>).
    /// <para>⛔ TOTAL OVER THE CARRIERS THIS COMPILER EMITS, AND NEVER A SILENT NO-OP (kb/Work PB165 closing
    /// GR-14.9.4.4-4). This used to be <c>string</c>-carrier or a <c>long</c>-carrier <c>long.TryParse</c> and
    /// nothing else, so a character result delivered into a <c>ulong</c>/<c>Int128</c>/<c>UInt128</c>
    /// identifier-3 — every PIC 9(19)+ receiver — was DISCARDED with no store and no diagnostic (measured: a
    /// callee returning "000123" left a <c>PIC 9(30)</c> identifier-3 at its previous value 7). §14.9.4.4 GR4
    /// says "the result of the activated program is placed into identifier-3"; nothing happening is the one
    /// outcome that rule excludes.</para>
    /// <para>A cross-CARRIER pair can only arise from source §14.8.3.3 already declares non-conforming (it
    /// requires the same PICTURE and USAGE clauses), which §14.9.4.4 GR3d makes EC-PROGRAM-ARG-MISMATCH when
    /// checking is enabled in both elements. The delivery therefore reads the result's DIGIT IMAGE — the
    /// characters the receiver would have seen had the pair conformed by length — and a shape with no delivery
    /// at all is loud rather than lost.</para></summary>
    public static void StoreReturn(ManagedPointer? ret, string value)
    {
        if (ret is null) return;                                   // a CALL without RETURNING discards (GR4 has no receiver)
        if (ret is ManagedPointer<string> sp) { sp.Value = value; return; }
        string t = value.Trim();
        if (Int128.TryParse(t, out Int128 v) && WriteNumericCell(ret, v)) return;
        if (double.TryParse(t, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out double d) && WriteRealCell(ret, d)) return;
        Undeliverable(ret, $"the character result \"{value}\"");
    }

    /// <summary>The RETURNING delivery has no leg for this (result shape, carrier) pair — §14.9.4.4 GR4's
    /// "placed into identifier-3" cannot be honoured, and §14.8.3.3's conformance rules are what such a pair
    /// violates (§14.9.4.4 GR3d ⇒ EC-PROGRAM-ARG-MISMATCH). Raised through the same
    /// <see cref="CobolCallException"/> the activation-time GR3d count check uses, so a run unit sees ONE
    /// EC-PROGRAM-ARG-MISMATCH mechanism — never a silent discard, which is what this replaced.</summary>
    private static void Undeliverable(ManagedPointer ret, string what) =>
        throw new CobolCallException(
            $"RETURNING delivery: {what} has no conforming store into the activating element's RETURNING item "
            + $"(carrier {ret.GetType().Name}) — ISO §14.8.3.3 requires the same PICTURE and USAGE clauses; "
            + "§14.9.4.4 GR3d — EC-PROGRAM-ARG-MISMATCH",
            "EC-PROGRAM-ARG-MISMATCH");

    /// <summary>Variable-length-group RETURNING delivery (ISO §14.8.3.2's compatibility sentence — the
    /// returning half of the same admission §14.8.2.2 grants arguments; kb/Work PB204).</summary>
    public static void StoreReturn(ManagedPointer? ret, CobolVarGroup value)
    {
        if (ret is null) return;
        if (ret is ManagedPointer<CobolVarGroup> vp) { vp.Value = value; return; }
        Undeliverable(ret, "the variable-length-group result");
    }

    /// <summary>Data-pointer RETURNING delivery (kb/Work PB133 wave B — §14.2.3 GR7 over a USAGE POINTER
    /// item; the PB111 shape: legal source drew CS1503 because no overload matched the carrier type).</summary>
    public static void StoreReturn(ManagedPointer? ret, ManagedPointer value)
    {
        if (ret is null) return;
        if (ret is ManagedPointer<ManagedPointer> pp) { pp.Value = value; return; }
        Undeliverable(ret, "the data-pointer result");
    }

    /// <summary>Program-pointer RETURNING delivery (kb/Work PB133 wave B — §13.18.60 GR24's identity
    /// struct crosses by value; same CS1503 shape as the data pointer).</summary>
    public static void StoreReturn(ManagedPointer? ret, ProgramPointer value)
    {
        if (ret is null) return;
        if (ret is ManagedPointer<ProgramPointer> pp) { pp.Value = value; return; }
        Undeliverable(ret, "the program-pointer result");
    }

    /// <summary>Object-reference RETURNING delivery (kb/Work PB133 wave B). The CobolObject constraint keeps
    /// this overload away from every numeric/string carrier (a value type or string never derives it), so the
    /// specific lanes above stay untouched. An IDENTICALLY-described returning pair (the §14.8.3 conforming
    /// case a prototype-less CALL can realize today) matches the typed carrier exactly; the cross-class
    /// described relationship rides the §14.8.2/§14.8.3 conformance campaign (PB133 wave C).</summary>
    public static void StoreReturn<T>(ManagedPointer? ret, T? value) where T : CobolObject
    {
        if (ret is null) return;
        if (ret is ManagedPointer<T?> tp) { tp.Value = value; return; }
        if (ret is ManagedPointer<CobolObject?> op) { op.Value = value; return; }
        Undeliverable(ret, "the object-reference result");
    }

    /// <summary>The omitted/absent CALL argument's carrier (ISO §14.9.4.4 GR11–GR12; kb/Work PB133 wave C):
    /// IsNull answers true — what makes the §8.8.4.8 omitted-argument condition and GR1c's TRANSITIVE
    /// omission work through the ordinary Present test — and a reference raises EC-PROGRAM-ARG-OMITTED
    /// through the CA10 checked-raise gate IN THE CALLEE's engine. Checking OFF stays lenient (reads answer
    /// the type's benign empty value, stores are ignored — GR12 leaves the content undefined; the documented
    /// implementor choice). The old carrier threw CobolCallException unconditionally, which the CALL SITE's
    /// catch arm treated as an ACTIVATION failure — an in-execution raise unwound into the CALLER's
    /// ON EXCEPTION phrase, which GR3i forbids.</summary>
    /// <summary>⛔ A SUPPLIED ARGUMENT THE FORMAL CANNOT READ (kb/Work PB615) — never the omitted carrier. Every
    /// adapter's type switch ends here once its readable arms are exhausted: the argument's carrier is outside the
    /// shapes the formal's crossing form can take, which only a pairing that violates §14.8.2's conformance rules
    /// (or a compiler defect) produces. §14.9.4.4 GR3d: "If a violation of these rules is detected, the
    /// EC-PROGRAM-ARG-MISMATCH exception condition is set to exist if checking for it is enabled in both the
    /// activated program and activating runtime element, the program call is not successful" — so this is an
    /// ACTIVATION failure, raised while the activated element adopts its formals and before any of its
    /// statements runs. It rides the same <see cref="CobolCallException"/> the GR3d argument-count check and
    /// the RETURNING delivery (<see cref="Undeliverable"/>) use — one EC-PROGRAM-ARG-MISMATCH mechanism — marked
    /// <see cref="CobolCallException.RaisedAtAdoption"/> so the activation boundary keeps it attributable to THIS
    /// CALL's GR3h rather than marking it as a condition propagated from the called program (GR3i).
    /// <para>Before PB615 each switch fell to <see cref="Omitted{T}"/>, whose read raises only under
    /// EC-PROGRAM-ARG-OMITTED checking and otherwise answers <c>default</c> — a supplied argument read as zero,
    /// silently, and a wrong exception-name when checking was on.</para></summary>
    private static ManagedPointer<T> Unreadable<T>(CobolArg[] args, int position, string formal) =>
        throw new CobolCallException(
            $"CALL argument #{position + 1}: its carrier ({args[position].Carrier.GetType().Name}) cannot be read "
            + $"through {formal} — the argument and the formal parameter do not conform (ISO §14.8.2 via "
            + "§14.9.4.4 GR3d — EC-PROGRAM-ARG-MISMATCH)",
            "EC-PROGRAM-ARG-MISMATCH") { RaisedAtAdoption = true };

    private static ManagedPointer<T> Omitted<T>(int position) => ManagedPointer<T>.OmittedArgument(
        () =>
        {
            RunUnit.Current.Exceptions.ProgramArgOmittedError(
                $"reference to omitted/absent CALL argument #{position + 1} (ISO §14.9.4.4 GR12)");
            // The benign empty value per carrier shape — a REFERENCE carrier must not hand back null and
            // turn a documented GR12 leniency into an NRE (kb/Work PB204 added the var-group carrier).
            if (typeof(T) == typeof(string)) return (T)(object)"";
            if (typeof(T) == typeof(CobolVarGroup)) return (T)(object)CobolVarGroup.Empty;
            // The DATA-POINTER carrier's benign empty value is the predefined NULL data pointer, not a CLR
            // null (ISO §8.4.3.10.4 GR1 — "the predefined address NULL references a data item of category
            // data-pointer that contains the null address"; kb/Work PB663). `default` would hand back null
            // and make the documented GR12 leniency an NRE the first time the callee referenced the formal —
            // the same trap the string and var-group arms exist for. ProgramPointer / FunctionPointer need no
            // arm: each is a readonly struct whose `default` IS its own null address (GR3 / GR2), and an
            // object reference's CLR null IS its initial state (§13.18.63).
            if (typeof(T) == typeof(ManagedPointer)) return (T)(object)ManagedPointer.Null;
            return default!;
        },
        _ => RunUnit.Current.Exceptions.ProgramArgOmittedError(
            $"store into omitted/absent CALL argument #{position + 1} (ISO §14.9.4.4 GR12)"));
}
