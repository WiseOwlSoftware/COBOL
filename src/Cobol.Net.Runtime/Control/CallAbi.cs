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
/// One CALL argument crossing the opaque ABI (design D2): the pass mode, the carrier, and the caller-side
/// numeric meta (digit count + scale) the callee-side adapters need to reinterpret a native-<c>long</c> carrier
/// through a differently-scaled or character-shaped formal (the D5-sanctioned category boundary).
/// </summary>
/// <param name="Mode">The pass mode (ISO §14.9.4.4 GR5 transitivity resolved at bind time).</param>
/// <param name="Carrier">The storage carrier (<see cref="ManagedPointer.Null"/> for OMITTED, GR11).</param>
/// <param name="Digits">Caller PICTURE digit count for a numeric argument; 0 for character storage.</param>
/// <param name="Scale">Caller PICTURE scale for a numeric argument; 0 for character storage.</param>
public readonly record struct CobolArg(CobolPassMode Mode, ManagedPointer Carrier, int Digits, int Scale);

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
}

/// <summary>
/// Callee-side positional argument adapters (design D2/D5): each maps <c>args[i]</c> onto a formal parameter's
/// carrier shape. Same-shape carriers pass through untouched (fully typed aliasing); a category mismatch (e.g. a
/// caller <c>PIC X(4)</c> viewed by the callee as <c>PIC 9(4)</c>) builds a CONVERTING view over the caller's
/// storage — the one sanctioned transient-character boundary (design D5; legal COBOL exercised by NIST), never a
/// persisted byte image. A missing / OMITTED argument yields a carrier that fails loud on first reference
/// (ISO §14.9.4.4 GR12 — EC-PROGRAM-ARG-OMITTED when the EC subsystem lands).
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
                    () => T.CreateTruncating(CobolNum.ParseDisplay(sp.Value, formal)),
                    v => sp.Value = CobolNum.FormatDisplay(Int128.CreateTruncating(v), formal));
            case { } np when ReadNumericCell(np) is not null:
            {
                // A native numeric cell of a DIFFERENT carrier or scale: a converting view over the caller's
                // storage. Same-scale cross-carrier reads/writes are bit-faithful through the Int128 lane.
                int callerScale = args[i].Scale;
                return ManagedPointer<T>.OverField(
                    () => T.CreateTruncating(CobolNum.Rescale(ReadNumericCell(np)!.Value, callerScale, formalScale, CobolRounding.Truncation)),
                    v => WriteNumericCell(np, CobolNum.Rescale(Int128.CreateTruncating(v), formalScale, callerScale, CobolRounding.Truncation)));
            }
            default:
                return Omitted<T>(i);
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
            case { } np when ReadNumericCell(np) is not null:
                // ANY LENGTH (width -1): the view width is the caller's digit-image width — n follows the
                // ARGUMENT's description (§13.18.2 GR1), never the formal's one-symbol picture. Generalized over
                // the four native carriers (kb/Work R12) — the same digit-image view, read/written through the
                // numeric-cell pair.
                int digits = args[i].Digits > 0 ? args[i].Digits : Math.Max(1, width);
                var prof = new NumProfile
                {
                    Digits = digits,
                    FractionDigits = Math.Max(0, args[i].Scale),
                    Signed = false,
                    Truncation = NumericTruncation.DigitCount,
                    ByteForm = NumericByteForm.Zoned,   // the CHARACTER view of the argument: one byte per digit
                };
                int viewWidth = width < 0 ? digits : width;   // ANY LENGTH: the argument's own image width (§13.18.2 GR1)
                return ManagedPointer<string>.OverField(
                    () => CobolString.Store(CobolNum.FormatDisplay(ReadNumericCell(np)!.Value, prof), viewWidth),
                    v => WriteNumericCell(np, CobolNum.ParseDisplay(v, prof)));
            default:
                return Omitted<string>(i);
        }
    }

    /// <summary>Adapt argument <paramref name="i"/> to a BY VALUE NUMERIC formal (ISO §14.2.3 GR10): the activated
    /// element operates on "the record in the linkage section … allocated by the activating runtime element" — a
    /// data item OF THE FORMAL'S OWN DESCRIPTION that does NOT alias the argument, filled as if by "a COMPUTE
    /// statement without the ROUNDED phrase" with the argument as the sending operand. Realized as a DETACHED
    /// cell: the argument's value is rescaled to the formal's scale (truncation — the un-ROUNDED COMPUTE) and
    /// conformed to the formal's digit capacity via <see cref="CobolNum.Store"/>; the callee's stores reach only
    /// the cell, never the caller's storage (contrast <see cref="Num"/>, the §14.2.3 GR8 aliasing view).</summary>
    public static ManagedPointer<T> NumValue<T>(CobolArg[] args, int i, NumProfile formal, int formalScale)
        where T : struct, System.Numerics.INumberBase<T>
    {
        if (!Present(args, i)) return Omitted<T>(i);
        Int128 v;
        switch (args[i].Carrier)
        {
            case { } np when ReadNumericCell(np) is { } nv:
                v = CobolNum.Rescale(nv, args[i].Scale, formalScale, CobolRounding.Truncation);
                break;
            case ManagedPointer<string> sp:
                v = CobolNum.ParseDisplay(sp.Value, formal);   // a character-carried argument decodes through the formal's profile
                break;
            default:
                return Omitted<T>(i);
        }
        // Store's 16-byte-unsigned result is container BITS (R10); CreateTruncating reinterprets them exactly.
        return ManagedPointer<T>.Cell(T.CreateTruncating(CobolNum.Store(v, formalScale, formal)));
    }

    /// <summary>Adapt argument <paramref name="i"/> to a BY VALUE formal whose callee-side storage is a CHARACTER
    /// image of <paramref name="width"/> positions (a REDEFINED fixed-point numeric formal — still class numeric,
    /// §14.2.2 SR2-legal, but image-carried): the same §14.2.3 GR10 detached copy as <see cref="NumValue"/>, in
    /// image form. Writes reach only the cell (contrast <see cref="Text"/>, the GR8 splice-through view).</summary>
    public static ManagedPointer<string> TextValue(CobolArg[] args, int i, int width)
    {
        if (!Present(args, i)) return Omitted<string>(i);
        switch (args[i].Carrier)
        {
            case ManagedPointer<string> sp:
                return ManagedPointer<string>.Cell(CobolString.Store(sp.Value, width));
            case { } np when ReadNumericCell(np) is { } nv:
                var prof = new NumProfile
                {
                    Digits = args[i].Digits > 0 ? args[i].Digits : Math.Max(1, width),
                    FractionDigits = Math.Max(0, args[i].Scale),
                    Signed = false,
                    Truncation = NumericTruncation.DigitCount,
                    ByteForm = NumericByteForm.Zoned,   // the CHARACTER image of the argument: one byte per digit
                };
                return ManagedPointer<string>.Cell(CobolString.Store(CobolNum.FormatDisplay(nv, prof), width));
            default:
                return Omitted<string>(i);
        }
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
        return args[i].Carrier is ManagedPointer<CobolVarGroup> vp ? vp : Omitted<CobolVarGroup>(i);
    }

    /// <summary>The BY VALUE / BY CONTENT twin of <see cref="VarGroup"/> (ISO §14.2.3 GR9/GR10 — a copy
    /// allocated by the activating element): a DETACHED cell holding the argument's carrier value, so the
    /// callee's stores never reach the caller's storage.</summary>
    public static ManagedPointer<CobolVarGroup> VarGroupValue(CobolArg[] args, int i)
    {
        if (!Present(args, i)) return Omitted<CobolVarGroup>(i);
        return args[i].Carrier is ManagedPointer<CobolVarGroup> vp
            ? ManagedPointer<CobolVarGroup>.Cell(vp.Value ?? CobolVarGroup.Empty)
            : Omitted<CobolVarGroup>(i);
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
        if (ret is ManagedPointer<string> sp) sp.Value = value.ToString();
    }

    /// <summary>String-shaped RETURNING delivery (see <see cref="StoreReturn(ManagedPointer?, long)"/>).</summary>
    public static void StoreReturn(ManagedPointer? ret, string value)
    {
        if (ret is ManagedPointer<string> sp) sp.Value = value;
        else if (ret is ManagedPointer<long> lp && long.TryParse(value.Trim(), out long v)) lp.Value = v;
    }

    /// <summary>Variable-length-group RETURNING delivery (ISO §14.8.3.2's compatibility sentence — the
    /// returning half of the same admission §14.8.2.2 grants arguments; kb/Work PB204).</summary>
    public static void StoreReturn(ManagedPointer? ret, CobolVarGroup value)
    {
        if (ret is ManagedPointer<CobolVarGroup> vp) vp.Value = value;
    }

    /// <summary>Data-pointer RETURNING delivery (kb/Work PB133 wave B — §14.2.3 GR7 over a USAGE POINTER
    /// item; the PB111 shape: legal source drew CS1503 because no overload matched the carrier type).</summary>
    public static void StoreReturn(ManagedPointer? ret, ManagedPointer value)
    {
        if (ret is ManagedPointer<ManagedPointer> pp) pp.Value = value;
    }

    /// <summary>Program-pointer RETURNING delivery (kb/Work PB133 wave B — §13.18.60 GR24's identity
    /// struct crosses by value; same CS1503 shape as the data pointer).</summary>
    public static void StoreReturn(ManagedPointer? ret, ProgramPointer value)
    {
        if (ret is ManagedPointer<ProgramPointer> pp) pp.Value = value;
    }

    /// <summary>Object-reference RETURNING delivery (kb/Work PB133 wave B). The CobolObject constraint keeps
    /// this overload away from every numeric/string carrier (a value type or string never derives it), so the
    /// specific lanes above stay untouched. An IDENTICALLY-described returning pair (the §14.8.3 conforming
    /// case a prototype-less CALL can realize today) matches the typed carrier exactly; the cross-class
    /// described relationship rides the §14.8.2/§14.8.3 conformance campaign (PB133 wave C).</summary>
    public static void StoreReturn<T>(ManagedPointer? ret, T? value) where T : CobolObject
    {
        if (ret is ManagedPointer<T?> tp) tp.Value = value;
        else if (ret is ManagedPointer<CobolObject?> op) op.Value = value;
    }

    /// <summary>The omitted/absent CALL argument's carrier (ISO §14.9.4.4 GR11–GR12; kb/Work PB133 wave C):
    /// IsNull answers true — what makes the §8.8.4.8 omitted-argument condition and GR1c's TRANSITIVE
    /// omission work through the ordinary Present test — and a reference raises EC-PROGRAM-ARG-OMITTED
    /// through the CA10 checked-raise gate IN THE CALLEE's engine. Checking OFF stays lenient (reads answer
    /// the type's benign empty value, stores are ignored — GR12 leaves the content undefined; the documented
    /// implementor choice). The old carrier threw CobolCallException unconditionally, which the CALL SITE's
    /// catch arm treated as an ACTIVATION failure — an in-execution raise unwound into the CALLER's
    /// ON EXCEPTION phrase, which GR3i forbids.</summary>
    private static ManagedPointer<T> Omitted<T>(int position) => ManagedPointer<T>.OmittedArgument(
        () =>
        {
            RunUnit.Current.Exceptions.ProgramArgOmittedError(
                $"reference to omitted/absent CALL argument #{position + 1} (ISO §14.9.4.4 GR12)");
            // The benign empty value per carrier shape — a REFERENCE carrier must not hand back null and
            // turn a documented GR12 leniency into an NRE (kb/Work PB204 added the var-group carrier).
            if (typeof(T) == typeof(string)) return (T)(object)"";
            if (typeof(T) == typeof(CobolVarGroup)) return (T)(object)CobolVarGroup.Empty;
            return default!;
        },
        _ => RunUnit.Current.Exceptions.ProgramArgOmittedError(
            $"store into omitted/absent CALL argument #{position + 1} (ISO §14.9.4.4 GR12)"));
}
