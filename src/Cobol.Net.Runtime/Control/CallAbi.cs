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

    /// <summary>Adapt argument <paramref name="i"/> to a NUMERIC formal described by <paramref name="formal"/>
    /// (the callee's profile) at <paramref name="formalScale"/>. A native-<c>long</c> carrier at the same scale
    /// aliases directly; a different scale gets a rescaling view; a character carrier gets a zoned decode/encode
    /// view through the CALLEE's profile — the same storage characters reinterpreted (§14.2.3 GR8; design D5).</summary>
    public static ManagedPointer<long> Num(CobolArg[] args, int i, NumProfile formal, int formalScale)
    {
        if (!Present(args, i)) return Omitted<long>(i);
        switch (args[i].Carrier)
        {
            case ManagedPointer<long> lp when args[i].Scale == formalScale:
                return lp;   // same shape, same scale — pure typed aliasing (the common conforming case)
            case ManagedPointer<long> lp:
                int callerScale = args[i].Scale;
                return ManagedPointer<long>.OverField(
                    () => (long)CobolNum.Rescale(lp.Value, callerScale, formalScale, CobolRounding.Truncation),
                    v => lp.Value = (long)CobolNum.Rescale(v, formalScale, callerScale, CobolRounding.Truncation));
            case ManagedPointer<string> sp:
                // The D5 boundary: the caller's CHARACTER storage viewed as the callee's zoned numeric — decode
                // and re-encode through the callee's profile on each access (same storage area, §14.2.3 GR8).
                return ManagedPointer<long>.OverField(
                    () => (long)CobolNum.ParseDisplay(sp.Value, formal),
                    v => sp.Value = CobolNum.FormatDisplay(v, formal));
            default:
                return Omitted<long>(i);
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
            case ManagedPointer<long> lp:
                // ANY LENGTH (width -1): the view width is the caller's digit-image width — n follows the
                // ARGUMENT's description (§13.18.2 GR1), never the formal's one-symbol picture.
                int digits = args[i].Digits > 0 ? args[i].Digits : Math.Max(1, width);
                var prof = new NumProfile
                {
                    Digits = digits,
                    FractionDigits = Math.Max(0, args[i].Scale),
                    Signed = false,
                    Truncation = NumericTruncation.DigitCount,
                    StorageForm = NumericStorageForm.Zoned,   // the CHARACTER view of the argument: one byte per digit
                };
                int viewWidth = width < 0 ? digits : width;   // ANY LENGTH: the argument's own image width (§13.18.2 GR1)
                return ManagedPointer<string>.OverField(
                    () => CobolString.Store(CobolNum.FormatDisplay(lp.Value, prof), viewWidth),
                    v => lp.Value = (long)CobolNum.ParseDisplay(v, prof));
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
    public static ManagedPointer<long> NumValue(CobolArg[] args, int i, NumProfile formal, int formalScale)
    {
        if (!Present(args, i)) return Omitted<long>(i);
        Int128 v;
        switch (args[i].Carrier)
        {
            case ManagedPointer<long> lp:
                v = CobolNum.Rescale(lp.Value, args[i].Scale, formalScale, CobolRounding.Truncation);
                break;
            case ManagedPointer<string> sp:
                v = CobolNum.ParseDisplay(sp.Value, formal);   // a character-carried argument decodes through the formal's profile
                break;
            default:
                return Omitted<long>(i);
        }
        return ManagedPointer<long>.Cell((long)CobolNum.Store(v, formalScale, formal));
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
            case ManagedPointer<long> lp:
                var prof = new NumProfile
                {
                    Digits = args[i].Digits > 0 ? args[i].Digits : Math.Max(1, width),
                    FractionDigits = Math.Max(0, args[i].Scale),
                    Signed = false,
                    Truncation = NumericTruncation.DigitCount,
                    StorageForm = NumericStorageForm.Zoned,   // the CHARACTER image of the argument: one byte per digit
                };
                return ManagedPointer<string>.Cell(CobolString.Store(CobolNum.FormatDisplay(lp.Value, prof), width));
            default:
                return Omitted<string>(i);
        }
    }

    /// <summary>Deliver a RETURNING value to the caller's RETURNING carrier (ISO §14.2.3 GR7 — at termination the
    /// returning item's value transfers to the activating element's RETURNING identifier). Null-tolerant: a CALL
    /// without RETURNING discards the value (deep-dive edge case).</summary>
    public static void StoreReturn(ManagedPointer? ret, long value)
    {
        if (ret is ManagedPointer<long> lp) lp.Value = value;
        else if (ret is ManagedPointer<string> sp) sp.Value = value.ToString();
    }

    /// <summary>String-shaped RETURNING delivery (see <see cref="StoreReturn(ManagedPointer?, long)"/>).</summary>
    public static void StoreReturn(ManagedPointer? ret, string value)
    {
        if (ret is ManagedPointer<string> sp) sp.Value = value;
        else if (ret is ManagedPointer<long> lp && long.TryParse(value.Trim(), out long v)) lp.Value = v;
    }

    /// <summary>A carrier whose first reference fails loud: the formal's argument was omitted or absent
    /// (ISO §14.9.4.4 GR12 — referencing an omitted parameter is the EC-PROGRAM-ARG-OMITTED condition).</summary>
    private static ManagedPointer<T> Omitted<T>(int position) => ManagedPointer<T>.OverField(
        () => throw new CobolCallException(
            $"reference to omitted/absent CALL argument #{position + 1} (ISO §14.9.4.4 GR12 — EC-PROGRAM-ARG-OMITTED)", "EC-PROGRAM-ARG-OMITTED"),
        _ => throw new CobolCallException(
            $"store into omitted/absent CALL argument #{position + 1} (ISO §14.9.4.4 GR12 — EC-PROGRAM-ARG-OMITTED)", "EC-PROGRAM-ARG-OMITTED"));
}
