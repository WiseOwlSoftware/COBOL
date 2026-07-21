// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using CobolNet.Runtime.IO;

namespace CobolNet.CodeGen;

/// <summary>
/// The typed, <c>nameof</c>-anchored façade over the emitted runtime surface (P7 Step 4b;
/// DESIGN-codegen-backend §3): every C# fragment that names a runtime member routes through here, so a runtime
/// rename breaks THIS file at compile time instead of silently mis-emitting text. Migration is INCREMENTAL by
/// design (the doc's shrinking-whitelist plan): the ratchet guard test
/// (<c>tests/Cobol.Net.Tests.Characterization/RuntimeApiGuardTests.cs</c>) pins each CodeGen file's bare
/// <c>Cobol*.</c> count and fails on any INCREASE — Step 9's per-verb rewrites drive the counts to zero, at
/// which point the whitelist empties and the guard flips to forbid-all. Type-name anchors land first (a runtime
/// TYPE rename already breaks here); member anchors accrete per migrated file.
/// </summary>
internal static class RuntimeApi
{
    // ── Type-name anchors (each is a compile-time reference to the runtime type). ──
    public static string Bool => nameof(CobolBool);

    /// <summary>Boolean NOT — ISO §8.8.4.5 boolean expressions (the D-B1 '0'/'1' string substrate).</summary>
    public static string BoolNot(string operand) => $"{nameof(CobolBool)}.{nameof(CobolBool.Not)}({operand})";

    /// <summary>A boolean dyadic op (AND/OR/XOR/EXCLUSIVE-OR family) — <c>CobolBool.{method}(l, r)</c>.
    /// <paramref name="method"/> is the runtime method NAME (validated by the anchors below at compile time
    /// via <see cref="BoolOpName"/>).</summary>
    public static string BoolOp(string method, string l, string r) => $"{nameof(CobolBool)}.{method}({l}, {r})";

    /// <summary>The literal-optimized variant <c>CobolBool.{method}All(operand, bits)</c>.</summary>
    public static string BoolOpAll(string method, string operand, string bitsLiteral) =>
        $"{nameof(CobolBool)}.{method}All({operand}, {bitsLiteral})";

    /// <summary>Compile-time anchor for the boolean dyadic method names the binder selects: renaming
    /// <c>CobolBool.And/Or/Xor</c> breaks this member, not the emitted text.</summary>
    public static string BoolOpName(char op) => op switch
    {
        '|' => nameof(CobolBool.Or),
        '^' => nameof(CobolBool.Xor),
        _ => nameof(CobolBool.And),   // '&' and the (unreachable) default — the pre-4b table's shape
    };

    /// <summary>A boolean shift/rotate (ISO §8.8.2 rule 8, 2023) — <c>CobolBool.Shift{Left|Right}[Circular](v, k)</c>.</summary>
    public static string BoolShift(CobolNet.Binding.Bound.BoolShiftKind kind, string operand, string count) => kind switch
    {
        CobolNet.Binding.Bound.BoolShiftKind.Left => $"{nameof(CobolBool)}.{nameof(CobolBool.ShiftLeft)}({operand}, {count})",
        CobolNet.Binding.Bound.BoolShiftKind.Right => $"{nameof(CobolBool)}.{nameof(CobolBool.ShiftRight)}({operand}, {count})",
        CobolNet.Binding.Bound.BoolShiftKind.LeftCircular => $"{nameof(CobolBool)}.{nameof(CobolBool.ShiftLeftCircular)}({operand}, {count})",
        _ => $"{nameof(CobolBool)}.{nameof(CobolBool.ShiftRightCircular)}({operand}, {count})",
    };

    // ── Numeric (CobolNum) ──

    /// <summary>Decode a zoned/separate-sign DISPLAY image per the receiver's profile — <c>CobolNum.ParseDisplay</c>.</summary>
    public static string NumParseDisplay(string image, string profile) =>
        $"{nameof(CobolNum)}.{nameof(CobolNum.ParseDisplay)}({image}, {profile})";

    /// <summary>An unsigned integer's DISPLAY image zero-padded to a fixed digit width —
    /// <c>CobolNum.FormatUnsignedDisplay</c> (the ACCEPT temporal conceptual-item image, ISO §14.9.1.4 GR7–GR12).</summary>
    public static string NumFormatUnsignedDisplay(string value, int digits) =>
        $"{nameof(CobolNum)}.{nameof(CobolNum.FormatUnsignedDisplay)}({value}, {digits})";

    /// <summary>The numeric MOVE-rules store (decimal alignment, truncation/zero-fill) — <c>CobolNum.Store</c>.</summary>
    public static string NumStore(string value, string scale, string profile) =>
        $"{nameof(CobolNum)}.{nameof(CobolNum.Store)}({value}, {scale}, {profile})";

    /// <summary>Render an unscaled value as the receiver's DISPLAY image — <c>CobolNum.FormatDisplay</c>.</summary>
    public static string NumFormatDisplay(string value, string profile) =>
        $"{nameof(CobolNum)}.{nameof(CobolNum.FormatDisplay)}({value}, {profile})";

    // ── Strings (CobolString) ──

    /// <summary>The alphanumeric MOVE-rules store (left-justify, right space-fill/truncate) —
    /// <c>CobolString.Store</c>.</summary>
    public static string StrStore(string value, string width) =>
        $"{nameof(CobolString)}.{nameof(CobolString.Store)}({value}, {width})";

    /// <summary>The DYNAMIC LENGTH receiving store (ISO §8.5.1.10.4 — replace, truncate on the right to the LIMIT,
    /// NO padding) — <c>CobolDynString.Store</c>. <paramref name="limit"/> is the LIMIT character count, or "-1"
    /// for the implementor-defined maximum (no explicit LIMIT phrase).</summary>
    public static string DynStore(string value, string limit) =>
        $"{nameof(CobolDynString)}.{nameof(CobolDynString.Store)}({value}, {limit})";

    /// <summary>SET [SIZE OF] data-name TO n (ISO §14.9.39 Format 16) — set the current length of a dynamic-length
    /// item, space-filling grown positions (GR39) — <c>CobolDynString.SetSize(current, newLen, limit)</c>.</summary>
    public static string DynSetSize(string current, string newLen, string limit) =>
        $"{nameof(CobolDynString)}.{nameof(CobolDynString.SetSize)}({current}, {newLen}, {limit})";

    /// <summary>CONTINUE AFTER n SECONDS (ISO §14.9.9) — the timed pause; a negative interval sets the nonfatal
    /// EC-CONTINUE-LESS-THAN-ZERO when checking is enabled — <c>CobolTiming.ContinueAfter(seconds, check)</c>.</summary>
    public static string ContinueAfter(string seconds, string checkLessThanZero) =>
        $"{nameof(CobolTiming)}.{nameof(CobolTiming.ContinueAfter)}({seconds}, {checkLessThanZero})";

    /// <summary>Set the run-unit termination status passed to the OS as the process exit code (ISO §14.9.42.4 GR5 /
    /// §14.9.18.4 GR10) — <c>RunUnit.SetExitStatus(status)</c>.</summary>
    public static string SetExitStatus(string status) =>
        $"{nameof(RunUnit)}.{nameof(RunUnit.SetExitStatus)}({status})";

    // ── Editing (CobolEdit) ──

    /// <summary>Edit a numeric value into a PICTURE mask — <c>CobolEdit.Format</c>. <paramref name="cfgArgs"/> is
    /// the SPECIAL-NAMES suffix (<see cref="Emit.EmitContext.EditCfgArgs"/>), possibly empty.</summary>
    public static string EditFormat(string value, string scale, string maskLiteral, string cfgArgs) =>
        $"{nameof(CobolEdit)}.{nameof(CobolEdit.Format)}({value}, {scale}, {maskLiteral}{cfgArgs})";

    /// <summary>The trailing <c>edits:</c> named argument for a numeric-edited store carrying PICTURE EDITING
    /// phrases (ISO §13.18.40.2 Format 1) — the resolved single-character render rules serialized as a
    /// <c>CobolEdit.EditRule[]</c>. Empty for every non-editing item, so the generated code of an ordinary program
    /// is byte-identical. Appended AFTER <c>BwzFlag</c>/<c>EditCfgArgs</c> (all named args) at each edited store.</summary>
    public static string EditsArg(IReadOnlyList<CobolEdit.EditRule>? rules)
    {
        if (rules is null || rules.Count == 0) return "";
        static string Ch(char c) => Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(c, quote: true);
        string items = string.Join(", ", rules.Select(r =>
            $"new {nameof(CobolEdit)}.{nameof(CobolEdit.EditRule)}({Ch(r.Char1)}, {Ch(r.Neg)}, {Ch(r.Pos)})"));
        return $", edits: new {nameof(CobolEdit)}.{nameof(CobolEdit.EditRule)}[] {{ {items} }}";
    }

    /// <summary>Place sending characters into an alphanumeric-edited mask's positions (ISO §13.18.40 insertion) —
    /// <c>CobolEdit.FormatAlphanumeric</c>.</summary>
    public static string EditFormatAlphanumeric(string value, string maskLiteral) =>
        $"{nameof(CobolEdit)}.{nameof(CobolEdit.FormatAlphanumeric)}({value}, {maskLiteral})";

    /// <summary>Decode a digit image's magnitude (non-digits contribute no digit) — <c>CobolNum.FromAlphanumeric</c>.</summary>
    public static string NumFromAlphanumeric(string image) =>
        $"{nameof(CobolNum)}.{nameof(CobolNum.FromAlphanumeric)}({image})";

    /// <summary>Rescale an unscaled value between fraction scales under a rounding mode — <c>CobolNum.Rescale</c>,
    /// or the size-error-latching <c>CobolNum.RescaleChecked</c> when <paramref name="checkedPath"/>.</summary>
    public static string NumRescale(string value, string fromScale, string toScale, CobolRounding mode, bool checkedPath = false) =>
        $"{nameof(CobolNum)}.{(checkedPath ? nameof(CobolNum.RescaleChecked) : nameof(CobolNum.Rescale))}({value}, {fromScale}, {toScale}, {RoundingText(mode)})";

    /// <summary>The §14.9.12 GR6c/GR7 scaled division — <c>CobolNum.Divide</c>, or the size-error-throwing
    /// <c>CobolNum.DivideOrThrow</c> under a checked context.</summary>
    public static string NumDivide(bool orThrow, string a, string aScale, string b, string bScale, string resultScale, CobolRounding mode) =>
        $"{nameof(CobolNum)}.{(orThrow ? nameof(CobolNum.DivideOrThrow) : nameof(CobolNum.Divide))}({a}, {aScale}, {b}, {bScale}, {resultScale}, {RoundingText(mode)})";

    /// <summary>The checked numeric store — <c>CobolNum.TryStore</c> (false = capacity/PROHIBITED failure; the
    /// receiver stays unchanged, §14.7.5). <paramref name="argsFragment"/> is the pre-shaped value/scale/profile
    /// argument run (fixed, Real-landed, or SDIDI overload).</summary>
    public static string NumTryStore(string argsFragment, CobolRounding mode, string outVar) =>
        $"{nameof(CobolNum)}.{nameof(CobolNum.TryStore)}({argsFragment}, {RoundingText(mode)}, out var {outVar})";

    /// <summary>The unchecked rounded store — the <c>CobolNum.Store</c> overload taking a rounding mode.</summary>
    public static string NumStoreRounded(string argsFragment, CobolRounding mode) =>
        $"{nameof(CobolNum)}.{nameof(CobolNum.Store)}({argsFragment}, {RoundingText(mode)})";

    /// <summary>An SDIDI intermediate landed to an unscaled value — the instance <c>CobolDec.ToUnscaled</c>.</summary>
    public static string DecToUnscaled(string decExpr, string scale, CobolRounding mode) =>
        $"({decExpr}).{nameof(CobolDec.ToUnscaled)}({scale}, {RoundingText(mode)})";

    /// <summary>SDIDI exponentiation (ISO §8.8.1.5.4; P10 Step 12) — <c>CobolDec.Pow</c>. <paramref name="mode"/>
    /// is the pre-rendered INTERMEDIATE ROUNDING fragment (<c>CobolRounding.X</c>).</summary>
    public static string DecPow(string baseOperand, string expOperand, string mode) =>
        $"{nameof(CobolDec)}.{nameof(CobolDec.Pow)}({baseOperand}, {expOperand}, {mode})";

    /// <summary>The §8.8.1.5.1 implementor-defined float→SDIDI operand conversion — <c>CobolDec.FromDouble</c>
    /// (the shortest round-trip decimal identity of the IEEE value; P10 Step 12).</summary>
    public static string DecFromDouble(string doubleExpr) =>
        $"{nameof(CobolDec)}.{nameof(CobolDec.FromDouble)}({doubleExpr})";

    /// <summary>One SDIDI division of two exactly-lifted fixed-point values — MEAN's §15.60.4 equivalent-expression
    /// division under standard-decimal arithmetic (§15.4.1 r1; P10 Step 12).</summary>
    public static string DecDivLifted(string numerator, string numeratorScale, string denominator, string mode) =>
        $"{nameof(CobolDec)}.{nameof(CobolDec.Div)}({nameof(CobolDec)}.{nameof(CobolDec.From)}({numerator}, "
        + $"{numeratorScale}), {nameof(CobolDec)}.{nameof(CobolDec.From)}({denominator}, 0), {mode})";

    /// <summary>A float value's inexactness probe at a fraction scale (the ROUNDED PROHIBITED gate, §14.7.5 r7)
    /// — <c>CobolFloat.InexactAtScale</c>.</summary>
    public static string FloatInexactAtScale(string value, string scale) =>
        $"{nameof(CobolFloat)}.{nameof(CobolFloat.InexactAtScale)}({value}, {scale})";

    /// <summary>The capacity-checked edited format — <c>CobolEdit.TryFormat</c> (false = the aligned value's
    /// significant digits exceed the mask, §14.7.5 case 3).</summary>
    public static string EditTryFormat(string value, string scale, string maskLiteral, string imgVar, string cfgArgs) =>
        $"{nameof(CobolEdit)}.{nameof(CobolEdit.TryFormat)}({value}, {scale}, {maskLiteral}, out var {imgVar}{cfgArgs})";

    /// <summary>Resize a boolean value to the receiver's GR3 width — <c>CobolBool.Resize</c>.</summary>
    public static string BoolResize(string value, string width) =>
        $"{nameof(CobolBool)}.{nameof(CobolBool.Resize)}({value}, {width})";

    /// <summary>The emitted-text reference to a <see cref="CobolRounding"/> value — <c>nameof</c>-anchored so a
    /// member rename breaks HERE, never the generated text.</summary>
    public static string RoundingText(CobolRounding mode) => $"{nameof(CobolRounding)}.{mode}";

    /// <summary>A floating-point intermediate landed to a scaled integer at a target fraction scale —
    /// <c>CobolFloat.ToScaled</c> (MOVE truncates toward zero, §14.6.8.2).</summary>
    public static string FloatToScaled(string value, string scale, CobolRounding mode) =>
        $"{nameof(CobolFloat)}.{nameof(CobolFloat.ToScaled)}({value}, {scale}, {RoundingText(mode)})";

    /// <summary>The BOOLEAN-receiver store (§14.6.8.6 — boolean-ZERO pad, explicit justification) —
    /// <c>CobolString.Store</c> with <c>pad: '0'</c>.</summary>
    public static string StrStoreBoolean(string value, string width, bool justifiedRight) =>
        $"{nameof(CobolString)}.{nameof(CobolString.Store)}({value}, {width}, " +
        $"justifiedRight: {(justifiedRight ? "true" : "false")}, pad: '0')";

    // ── INSPECT (CobolInspect; ISO §14.9.22) ──

    /// <summary>The tallying pass — <c>CobolInspect.Tally</c>. Array-literal fragments are pre-rendered by the caller.</summary>
    public static string InspectTally(string image, string kinds, string pats, string befs, string afts, string backward) =>
        $"{nameof(CobolInspect)}.{nameof(CobolInspect.Tally)}({image}, new int[] {{ {kinds} }}, " +
        $"new string?[] {{ {pats} }}, new string?[] {{ {befs} }}, new string?[] {{ {afts} }}, {backward})";

    /// <summary>The replacing pass — <c>CobolInspect.Replace</c>.</summary>
    public static string InspectReplace(string image, string kinds, string pats, string reps, string befs, string afts, string backward) =>
        $"{nameof(CobolInspect)}.{nameof(CobolInspect.Replace)}({image}, new int[] {{ {kinds} }}, new string?[] {{ {pats} }}, " +
        $"new string?[] {{ {reps} }}, new string?[] {{ {befs} }}, new string?[] {{ {afts} }}, {backward})";

    /// <summary>CONVERTING — <c>CobolInspect.Convert</c>.</summary>
    public static string InspectConvert(string image, string from, string to, string before, string after, string backward) =>
        $"{nameof(CobolInspect)}.{nameof(CobolInspect.Convert)}({image}, {from}, {to}, {before}, {after}, {backward})";

    /// <summary>Compile-time anchor for the tally-kind discriminators the emitter selects.</summary>
    public static string InspectTallyKindText(Binding.Bound.InspectTallyKind k) => k switch
    {
        Binding.Bound.InspectTallyKind.All => $"{nameof(CobolInspect)}.{nameof(CobolInspect.TallyAll)}",
        Binding.Bound.InspectTallyKind.Leading => $"{nameof(CobolInspect)}.{nameof(CobolInspect.TallyLeading)}",
        _ => $"{nameof(CobolInspect)}.{nameof(CobolInspect.TallyCharacters)}",
    };

    /// <summary>Compile-time anchor for the replace-kind discriminators the emitter selects.</summary>
    public static string InspectReplaceKindText(Binding.Bound.InspectReplaceKind k) => k switch
    {
        Binding.Bound.InspectReplaceKind.All => $"{nameof(CobolInspect)}.{nameof(CobolInspect.ReplaceAll)}",
        Binding.Bound.InspectReplaceKind.First => $"{nameof(CobolInspect)}.{nameof(CobolInspect.ReplaceFirst)}",
        Binding.Bound.InspectReplaceKind.Leading => $"{nameof(CobolInspect)}.{nameof(CobolInspect.ReplaceLeading)}",
        _ => $"{nameof(CobolInspect)}.{nameof(CobolInspect.ReplaceCharacters)}",
    };

    // ── STRING / UNSTRING (CobolStringOps; ISO §14.9.43 / §14.9.48) ──

    /// <summary>One sending operand's transfer into the STRING working image — <c>CobolStringOps.StringTransfer</c>
    /// (advances the pointer, latches the overflow flag by ref).</summary>
    public static string StrTransfer(string acc, string src, string delim, string ptrVar, string ovfVar) =>
        $"{nameof(CobolStringOps)}.{nameof(CobolStringOps.StringTransfer)}({acc}, {src}, {delim}, ref {ptrVar}, ref {ovfVar})";

    /// <summary>One receiving area's extraction — <c>CobolStringOps.UnstringExtract</c> (out-vars for the examined
    /// field and the matched delimiter; −1 = not acted upon).</summary>
    public static string UnstringExtract(string src, string dels, string alls, string noDelimSize,
        string ptrVar, string fldVar, string dlmVar) =>
        $"{nameof(CobolStringOps)}.{nameof(CobolStringOps.UnstringExtract)}({src}, {dels}, {alls}, {noDelimSize}, " +
        $"ref {ptrVar}, out var {fldVar}, out var {dlmVar})";

    /// <summary>The JUSTIFIED-right alphanumeric store (§14.9.25.4 GR6c) — <c>CobolString.Store</c> with
    /// <c>justifiedRight: true</c>.</summary>
    public static string StrStoreJustified(string value, string width) =>
        $"{nameof(CobolString)}.{nameof(CobolString.Store)}({value}, {width}, justifiedRight: true)";

    // ── Pointers (CobolPtr; ISO §14.9.39 F7/F10, §14.9.3, §14.9.15) ──

    /// <summary>Displace a pointer by n character positions — <c>CobolPtr.UpBy</c> (GR18 null trap inside).</summary>
    public static string PtrUpBy(string ptr, string amount) =>
        $"{nameof(CobolPtr)}.{nameof(CobolPtr.UpBy)}({ptr}, {amount})";

    /// <summary>Displace by a SCALED amount — <c>CobolPtr.UpByScaled</c> (the GR19 divisibility test).</summary>
    public static string PtrUpByScaled(string ptr, string amount, string scale) =>
        $"{nameof(CobolPtr)}.{nameof(CobolPtr.UpByScaled)}({ptr}, {amount}, {scale})";

    /// <summary>ALLOCATE a fresh cell — <c>CobolPtr.Allocate</c> (GR1/GR2; GR6 zero fill).</summary>
    public static string PtrAllocate(string size, bool zeroFill = false) =>
        $"{nameof(CobolPtr)}.{nameof(CobolPtr.Allocate)}({size}{(zeroFill ? ", zeroFill: true" : "")})";

    /// <summary>FREE a pointer's cell — <c>CobolPtr.Free</c> (three-way per GR1; not-alloc out-flag).</summary>
    public static string PtrFree(string ptr, string notAllocVar) =>
        $"{nameof(CobolPtr)}.{nameof(CobolPtr.Free)}({ptr}, out {notAllocVar})";

    // ── More strings / tables ──

    /// <summary>A reference-modification slice — <c>CobolString.RefMod</c> (1-based start, length).
    /// <paramref name="allowZeroLength"/> (the REF-MOD-ZERO-LENGTH directive, §7.3.23) emits the named argument only
    /// when true, so every existing site stays byte-identical.</summary>
    public static string StrRefMod(string s, string start, string len, bool allowZeroLength = false) =>
        $"{nameof(CobolString)}.{nameof(CobolString.RefMod)}({s}, {start}, {len}{(allowZeroLength ? ", allowZeroLength: true" : "")})";

    /// <summary>Splice <paramref name="rhs"/> into <paramref name="s"/> at a 1-based start/length, preserving the
    /// rest of the width — <c>CobolString.SpliceInto</c>. <paramref name="pad"/> is the optional fill-char argument
    /// (a C# <c>char</c> literal, e.g. boolean-zero <c>'0'</c>); null emits the default space fill.</summary>
    public static string StrSpliceInto(string s, string start, string len, string rhs, string? pad = null,
        bool allowZeroLength = false) =>
        $"{nameof(CobolString)}.{nameof(CobolString.SpliceInto)}({s}, {start}, {len}, {rhs}"
        + $"{(pad is null ? "" : $", pad: {pad}")}{(allowZeroLength ? ", allowZeroLength: true" : "")})";

    /// <summary>The three-way alphanumeric comparison — <c>CobolString.Compare</c>. <paramref name="weightsArg"/>
    /// is the trailing collated-weights argument (", __COLLATE" / an inline table), possibly empty.</summary>
    public static string StrCompare(string a, string b, string weightsArg) =>
        $"{nameof(CobolString)}.{nameof(CobolString.Compare)}({a}, {b}{weightsArg})";

    /// <summary>An OCCURS-DEPENDING current count read — <c>CobolTable.Occ</c>.</summary>
    public static string TableOcc(string expr) => $"{nameof(CobolTable)}.{nameof(CobolTable.Occ)}({expr})";

    /// <summary>A FIXED OCCURS element access — the ref-returning <c>CobolTable.At(path, oneBasedIndex)</c>
    /// (ISO §8.4.2.3.4 GR2 — a benign out-of-range occurrence, subscript-checking off in COBOL-85).</summary>
    public static string TableAt(string path, string oneBasedIndex) =>
        $"{nameof(CobolTable)}.{nameof(CobolTable.At)}({path}, {oneBasedIndex})";

    /// <summary>The current CHARACTER extent of an occurs-depending GROUP operand (ISO §13.18.38 GR8) — the fixed
    /// prefix plus data-name-1's clamped value × the element width — <c>CobolTable.OdoExtent</c>.</summary>
    public static string TableOdoExtent(string occ, int maxOccurs, int fixedChars, int elemChars) =>
        $"{nameof(CobolTable)}.{nameof(CobolTable.OdoExtent)}({occ}, {maxOccurs}, {fixedChars}, {elemChars})";

    // ── Keyed file I/O (CobolFile; ISO §14.9.10/.30/.35/.41/.51) ──

    /// <summary>Register a RELATIVE connector — <c>CobolFile.RegisterRelative</c>. <paramref name="varyArgs"/> is
    /// the optional trailing ", min, max" record-bounds fragment (§13.18.43 GR9/GR10), possibly empty.</summary>
    public static string FileRegisterRelative(string name, string assign, int width, string optional, int access, int keyDigits, string varyArgs) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.RegisterRelative)}({name}, {assign}, {width}, {optional}, {access}, {keyDigits}{varyArgs})";

    /// <summary>Register an INDEXED connector — <c>CobolFile.RegisterIndexed</c> (prime-key window per §12.4.5.12,
    /// plus the optional §12.4.5.7 prime-key collating weights; <paramref name="weights"/> is "null" for native,
    /// emitted as a named argument so a no-clause file's registration is byte-identical to the pre-clause engine).</summary>
    public static string FileRegisterIndexed(string name, string assign, int width, string optional, int access, string pkOffset, int pkWidth, string varyArgs, string weights = "null") =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.RegisterIndexed)}({name}, {assign}, {width}, {optional}, {access}, {pkOffset}, {pkWidth}{varyArgs}{(weights == "null" ? "" : $", primeWeights: {weights}")})";

    /// <summary>Register one ALTERNATE RECORD KEY window (§12.4.5.6) — <c>CobolFile.AddAlternateKey</c>, with its
    /// optional §12.4.5.7 collating weights and §12.4.5.6.4 GR6 SUPPRESS WHEN value ("null" = absent, each emitted
    /// as a named argument so a plain alternate key's registration is unchanged).</summary>
    public static string FileAddAlternateKey(string name, string offset, int width, string dups, string weights = "null", string suppress = "null") =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.AddAlternateKey)}({name}, {offset}, {width}, {dups}{(weights == "null" ? "" : $", weights: {weights}")}{(suppress == "null" ? "" : $", suppress: {suppress}")})";

    /// <summary>Position a relative connector to the RELATIVE KEY item's RRN — <c>CobolFile.SetRelativeKey</c>.</summary>
    public static string FileSetRelativeKey(string name, string rrn) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.SetRelativeKey)}({name}, {rrn})";

    /// <summary>Sequential-forward keyed READ — <c>CobolFile.ReadKeyedNext</c> (status result, out image).</summary>
    public static string FileReadKeyedNext(string name, string imgVar) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.ReadKeyedNext)}({name}, out var {imgVar})";

    /// <summary>READ PREVIOUS — <c>CobolFile.ReadKeyedPrevious</c>.</summary>
    public static string FileReadKeyedPrevious(string name, string imgVar) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.ReadKeyedPrevious)}({name}, out var {imgVar})";

    /// <summary>Random keyed READ by key-of-reference — <c>CobolFile.ReadKeyed</c>.</summary>
    public static string FileReadKeyed(string name, int keyIndex, string keyImage, string imgVar) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.ReadKeyed)}({name}, {keyIndex}, {keyImage}, out var {imgVar})";

    /// <summary>The §9.1.16 record-lock governance adjustment of a just-read status — <c>CobolFile.ReadLockGovern</c>.</summary>
    public static string FileReadLockGovern(string name, string status, string lockRef, string retryKind, string retryAmount) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.ReadLockGovern)}({name}, {status}, {lockRef}, {retryKind}, {retryAmount})";

    /// <summary>Sequential-organization governed READ (§9.1.16 / §14.9.30 GR9–GR12/GR22) — <c>CobolFile.ReadShared</c>
    /// (bool result, out image — the same contract as the plain <c>FileRead</c>).</summary>
    public static string FileReadShared(string name, string lockRef, string advancingOnLock, string retryKind,
        string retryAmount, string imgVar) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.ReadShared)}({name}, {lockRef}, {advancingOnLock}, {retryKind}, {retryAmount}, out var {imgVar})";

    /// <summary>Governed WRITE for a sharing-active file, any organization (§14.9.51 GR10/GR11) — <c>CobolFile.WriteShared</c>.</summary>
    public static string FileWriteShared(string name, string image, string lenArg, string lockRef, string retryKind, string retryAmount) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.WriteShared)}({name}, {image}, {lenArg}, {lockRef}, {retryKind}, {retryAmount})";

    /// <summary>Governed REWRITE for a sharing-active file, any organization (§14.9.35 GR11/GR12) — <c>CobolFile.RewriteShared</c>.</summary>
    public static string FileRewriteShared(string name, string image, string lenArg, string lockRef, string retryKind, string retryAmount) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.RewriteShared)}({name}, {image}, {lenArg}, {lockRef}, {retryKind}, {retryAmount})";

    /// <summary>Governed DELETE RECORD for a sharing-active file (§14.9.10 GR6/GR7) — <c>CobolFile.DeleteShared</c>.</summary>
    public static string FileDeleteShared(string name, string areaImage, string retryKind, string retryAmount) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.DeleteShared)}({name}, {areaImage}, {retryKind}, {retryAmount})";

    /// <summary>DELETE FILE with a RETRY phrase (§14.9.10 GR15 — the '62' re-attempt) — <c>CobolFile.DeleteFile</c>.</summary>
    public static string FileDeleteFileRetry(string name, string retryKind, string retryAmount) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.DeleteFile)}({name}, {retryKind}, {retryAmount})";

    /// <summary>The connector's current relative slot number — <c>CobolFile.RelativeSlot</c>.</summary>
    public static string FileRelativeSlot(string name) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.RelativeSlot)}({name})";

    /// <summary>Keyed WRITE — <c>CobolFile.WriteKeyed</c>; <paramref name="lenArg"/> = the optional §13.18.43
    /// GR13a varying-length argument (null when fixed).</summary>
    public static string FileWriteKeyed(string name, string image, string? lenArg = null) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.WriteKeyed)}({name}, {image}{(lenArg is null ? "" : $", {lenArg}")})";

    /// <summary>Keyed REWRITE — <c>CobolFile.RewriteKeyed</c>.</summary>
    public static string FileRewriteKeyed(string name, string image, string? lenArg = null) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.RewriteKeyed)}({name}, {image}{(lenArg is null ? "" : $", {lenArg}")})";

    /// <summary>DELETE RECORD — <c>CobolFile.DeleteRecord</c> (key sliced from the record-area image, GR3/GR8).</summary>
    public static string FileDeleteRecord(string name, string areaImage) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.DeleteRecord)}({name}, {areaImage})";

    /// <summary>DELETE FILE (Format 2, every organization) — <c>CobolFile.DeleteFile</c>.</summary>
    public static string FileDeleteFile(string name) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.DeleteFile)}({name})";

    /// <summary>START FIRST/LAST — <c>CobolFile.StartFirstLast</c>.</summary>
    public static string FileStartFirstLast(string name, string last) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.StartFirstLast)}({name}, {last})";

    /// <summary>START on a relative file (§14.9.41 GR9/GR10 — numeric RRN comparison) — <c>CobolFile.StartRelative</c>.</summary>
    public static string FileStartRelative(string name, string opLiteral, string rrn) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.StartRelative)}({name}, {opLiteral}, {rrn})";

    /// <summary>START on an indexed file (§14.9.41 GR17 — leftmost-LENGTH key comparison) — <c>CobolFile.StartIndexed</c>.</summary>
    public static string FileStartIndexed(string name, int keyIndex, string opLiteral, string operandImage, string len) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.StartIndexed)}({name}, {keyIndex}, {opLiteral}, {operandImage}, {len})";

    /// <summary>Implicit OPEN INPUT (SORT GR12a / MERGE GR7a) — <c>CobolFile.OpenInput</c>.</summary>
    public static string FileOpenInput(string name) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.OpenInput)}({name})";

    /// <summary>Implicit OPEN OUTPUT (SORT GR15a) — <c>CobolFile.OpenOutput</c>.</summary>
    public static string FileOpenOutput(string name) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.OpenOutput)}({name})";

    /// <summary>CLOSE a connector — <c>CobolFile.Close</c>.</summary>
    public static string FileClose(string name) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.Close)}({name})";

    /// <summary>Sequential READ into an out-image (the implicit USING loop shape) — <c>CobolFile.Read</c>.</summary>
    public static string FileRead(string name, string imgVar) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.Read)}({name}, out var {imgVar})";

    /// <summary>Sequential WRITE without optional phrases (the implicit GIVING loop shape) — <c>CobolFile.Write</c>.</summary>
    public static string FileWrite(string name, string image, string? lenArg = null) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.Write)}({name}, {image}{(lenArg is null ? "" : $", {lenArg}")})";

    /// <summary>Register a SEQUENTIAL/LINE-SEQUENTIAL connector — <c>CobolFile.Register</c>.
    /// <paramref name="varyArgs"/> is the optional trailing ", min, max" bounds fragment.</summary>
    public static string FileRegister(string name, string assign, string width, string lineSeq, string optional, string varyArgs = "") =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.Register)}({name}, {assign}, {width}, {lineSeq}, {optional}{varyArgs})";

    /// <summary>Register a LINAGE file's logical-page evaluator closure (§13.18.34 GR6) — <c>CobolFile.SetLinage</c>.</summary>
    public static string FileSetLinage(string name, string closure) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.SetLinage)}({name}, {closure})";

    /// <summary>Mark a connector sharing-active (Phase 4d M2-FILE-1) — <c>CobolFile.RegisterSharing</c>.</summary>
    public static string FileRegisterSharing(string name, string argsFragment) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.RegisterSharing)}({name}, {argsFragment})";

    /// <summary>The sharing-governed OPEN (Table 19 → status 61) — <c>CobolFile.OpenShared</c>.</summary>
    public static string FileOpenShared(string name, string argsFragment) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.OpenShared)}({name}, {argsFragment})";

    /// <summary>The mode-specific plain OPEN — anchored over <c>CobolFile.Open{Input,Output,Extend,IO}</c>.</summary>
    public static string FileOpen(string name, Binding.Bound.BoundOpenMode mode) =>
        $"{nameof(CobolFile)}.{mode switch
        {
            Binding.Bound.BoundOpenMode.Output => nameof(CobolFile.OpenOutput),
            Binding.Bound.BoundOpenMode.Extend => nameof(CobolFile.OpenExtend),
            Binding.Bound.BoundOpenMode.IO => nameof(CobolFile.OpenIO),
            _ => nameof(CobolFile.OpenInput),
        }}({name})";

    /// <summary>The kind-specific CLOSE — anchored over <c>CobolFile.Close{,WithLock,ReelUnit}</c>.</summary>
    public static string FileClose(string name, Binding.Bound.BoundCloseKind kind) =>
        $"{nameof(CobolFile)}.{kind switch
        {
            Binding.Bound.BoundCloseKind.WithLock => nameof(CobolFile.CloseWithLock),
            Binding.Bound.BoundCloseKind.ReelUnit => nameof(CobolFile.CloseReelUnit),
            _ => nameof(CobolFile.Close),
        }}({name})";

    /// <summary>UNLOCK — <c>CobolFile.Unlock</c> (records flag = UNLOCK RECORDS).</summary>
    public static string FileUnlock(string name, string records) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.Unlock)}({name}, {records})";

    /// <summary>WRITE … ADVANCING — <c>CobolFile.WriteAdvancing</c>.</summary>
    public static string FileWriteAdvancing(string name, string image, string lines, string before) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.WriteAdvancing)}({name}, {image}, {lines}, {before})";

    /// <summary>WRITE … BEFORE ADVANCING n AFTER ADVANCING m (ISO §14.9.51, 2023) — <c>CobolFile.WriteBeforeAndAfter</c>.</summary>
    public static string FileWriteBeforeAndAfter(string name, string image, string beforeLines, string afterLines) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.WriteBeforeAndAfter)}({name}, {image}, {beforeLines}, {afterLines})";

    /// <summary>The LINAGE end-of-page probe (§13.18.34) — <c>CobolFile.EndOfPage</c>.</summary>
    public static string FileEndOfPage(string name) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.EndOfPage)}({name})";

    /// <summary>The connector's two-character I-O status — <c>CobolFile.Status</c>.</summary>
    public static string FileStatus(string name) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.Status)}({name})";

    /// <summary>The open-mode ordinal of a connector (the USE mode-scope switch) — <c>CobolFile.OpenModeOf</c>.</summary>
    public static string FileOpenModeOf(string name) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.OpenModeOf)}({name})";

    /// <summary>Sequential REWRITE — <c>CobolFile.Rewrite</c> (optional varying-length argument).</summary>
    public static string FileRewrite(string name, string image, string? lenArg = null) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.Rewrite)}({name}, {image}{(lenArg is null ? "" : $", {lenArg}")})";

    /// <summary>The just-read record's frame length — <c>CobolFile.LastReadLength</c>.</summary>
    public static string FileLastReadLength(string name) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.LastReadLength)}({name})";

    // ── SORT / MERGE (CobolSort; ISO §14.9.40 / §14.9.24) ──

    /// <summary>Initialize the per-SD image store — <c>CobolSort.Init</c>.</summary>
    public static string SortInit(string sd) => $"{nameof(CobolSort)}.{nameof(CobolSort.Init)}({sd})";

    /// <summary>RELEASE one record image — <c>CobolSort.Release</c>.</summary>
    public static string SortRelease(string sd, string image) =>
        $"{nameof(CobolSort)}.{nameof(CobolSort.Release)}({sd}, {image})";

    /// <summary>The sequence phase — <c>CobolSort.Sort</c> (stable; GR8).</summary>
    public static string SortSort(string sd, string keys, string weights, string dupsInOrder) =>
        $"{nameof(CobolSort)}.{nameof(CobolSort.Sort)}({sd}, {keys}, {weights}, {dupsInOrder})";

    /// <summary>The k-way merge — <c>CobolSort.Merge</c> (GR4 — file order breaks ties).</summary>
    public static string SortMerge(string sd, string keys, string weights) =>
        $"{nameof(CobolSort)}.{nameof(CobolSort.Merge)}({sd}, {keys}, {weights})";

    /// <summary>Open a new pre-sorted USING stream — <c>CobolSort.NextInput</c>.</summary>
    public static string SortNextInput(string sd) => $"{nameof(CobolSort)}.{nameof(CobolSort.NextInput)}({sd})";

    /// <summary>Close the SD store — <c>CobolSort.Close</c>.</summary>
    public static string SortClose(string sd) => $"{nameof(CobolSort)}.{nameof(CobolSort.Close)}({sd})";

    /// <summary>Rewind the return cursor (each GIVING file gets the FULL result, GR15) — <c>CobolSort.Rewind</c>.</summary>
    public static string SortRewind(string sd) => $"{nameof(CobolSort)}.{nameof(CobolSort.Rewind)}({sd})";

    /// <summary>Pull the next record in key order — <c>CobolSort.Return</c> (bool, out image).</summary>
    public static string SortReturn(string sd, string imgVar) =>
        $"{nameof(CobolSort)}.{nameof(CobolSort.Return)}({sd}, out var {imgVar})";

    /// <summary>The just-returned record's length (§13.18.43 GR15) — <c>CobolSort.LastReturnedLength</c>.</summary>
    public static string SortLastReturnedLength(string sd) =>
        $"{nameof(CobolSort)}.{nameof(CobolSort.LastReturnedLength)}({sd})";

    /// <summary>The <c>CobolSort.Key[]</c> array literal over per-key "new(…)" element fragments.</summary>
    public static string SortKeyArray(IEnumerable<string> keyElements) =>
        $"new {nameof(CobolSort)}.{nameof(CobolSort.Key)}[] {{ {string.Join(", ", keyElements)} }}";

    // ── Report Writer (CobolReport; ISO §13.14–§13.18) ──

    /// <summary>A space-filled report-line buffer — <c>CobolReport.NewLine</c>.</summary>
    public static string ReportNewLine(int width) =>
        $"{nameof(CobolReport)}.{nameof(CobolReport.NewLine)}({width})";

    /// <summary>Place a printable item's image at its COLUMN (§13.18.14) — <c>CobolReport.Place</c>.</summary>
    public static string ReportPlace(string lineVar, int column, string image) =>
        ReportPlace(lineVar, column.ToString(), image);

    /// <summary>The variable-column form of <see cref="ReportPlace(string,int,string)"/> — a relative (PLUS)
    /// COLUMN operand places against the line's horizontal counter (§13.18.14.4 GR8).</summary>
    public static string ReportPlace(string lineVar, string columnExpr, string image) =>
        $"{nameof(CobolReport)}.{nameof(CobolReport.Place)}({lineVar}, {columnExpr}, {image})";

    /// <summary>Decode a DISPLAY image back into a native numeric leaf, preserving unset positions from the
    /// current value — <c>CobolNum.StoreDisplay</c>.</summary>
    public static string NumStoreDisplay(string image, string profile, string current) =>
        $"{nameof(CobolNum)}.{nameof(CobolNum.StoreDisplay)}({image}, {profile}, {current})";

    // ── Inter-program ABI (CobolArgAdapt / CobolPassMode; interprogram design D1/D2) — Step 9-final sweep ──

    /// <summary>A LINKAGE formal's numeric carrier adoption — <c>CobolArgAdapt.Num</c>.</summary>
    public static string ArgAdaptNum(string args, int position, string profile, string scale) =>
        $"{nameof(CobolArgAdapt)}.{nameof(CobolArgAdapt.Num)}({args}, {position}, {profile}, {scale})";

    /// <summary>A LINKAGE formal's text carrier adoption — <c>CobolArgAdapt.Text</c>.</summary>
    public static string ArgAdaptText(string args, int position, string width) =>
        $"{nameof(CobolArgAdapt)}.{nameof(CobolArgAdapt.Text)}({args}, {position}, {width})";

    /// <summary>A BY VALUE numeric formal's DETACHED value-copy cell (ISO §14.2.3 GR10 — stores never reach
    /// the caller) — <c>CobolArgAdapt.NumValue</c>.</summary>
    public static string ArgAdaptNumValue(string args, int position, string profile, string scale) =>
        $"{nameof(CobolArgAdapt)}.{nameof(CobolArgAdapt.NumValue)}({args}, {position}, {profile}, {scale})";

    /// <summary>A BY VALUE image-carried formal's DETACHED value-copy cell (§14.2.3 GR10, image form) —
    /// <c>CobolArgAdapt.TextValue</c>.</summary>
    public static string ArgAdaptTextValue(string args, int position, string width) =>
        $"{nameof(CobolArgAdapt)}.{nameof(CobolArgAdapt.TextValue)}({args}, {position}, {width})";

    /// <summary>The argument-present probe (OMITTED handling, §14.2.3) — <c>CobolArgAdapt.Present</c>.</summary>
    public static string ArgAdaptPresent(string args, int position) =>
        $"{nameof(CobolArgAdapt)}.{nameof(CobolArgAdapt.Present)}({args}, {position})";

    /// <summary>RETURNING delivery into the caller's cell (§14.2.3 GR7) — <c>CobolArgAdapt.StoreReturn</c>.</summary>
    public static string ArgAdaptStoreReturn(string ret, string value) =>
        $"{nameof(CobolArgAdapt)}.{nameof(CobolArgAdapt.StoreReturn)}({ret}, {value})";

    /// <summary>The emitted-text reference to a <see cref="CobolPassMode"/> value — <c>nameof</c>-anchored like
    /// <see cref="RoundingText"/>, so a member rename breaks HERE, never the generated text.</summary>
    public static string PassModeText(CobolPassMode mode) => $"{nameof(CobolPassMode)}.{mode}";

    // ── Run-unit lifecycle (CobolFile) ──

    /// <summary>Run-unit file-subsystem init (the entry wrapper's Main) — <c>CobolFile.Init</c>.</summary>
    public static string FileInit() => $"{nameof(CobolFile)}.{nameof(CobolFile.Init)}()";

    /// <summary>The §14.6.11 run-unit-termination implicit CLOSE — <c>CobolFile.CloseAll</c>.</summary>
    public static string FileCloseAll() => $"{nameof(CobolFile)}.{nameof(CobolFile.CloseAll)}()";

    /// <summary>Mint a per-object instance-file connector key (§9.1.4) — <c>CobolFile.MintInstanceKey</c>.</summary>
    public static string FileMintInstanceKey(string baseKeyLiteral) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.MintInstanceKey)}({baseKeyLiteral})";

    // ── Pointers / objects ──

    /// <summary>Dereference a data-address pointer to its storage cell (GR3/GR4 loud) — <c>CobolPtr.Deref</c>.</summary>
    public static string PtrDeref(string ptr, string classWidth) =>
        $"{nameof(CobolPtr)}.{nameof(CobolPtr.Deref)}({ptr}, {classWidth})";

    /// <summary>The INVOKE null-receiver guard (EC-OO-NULL, §14.9.23.4 GR5) — <c>CobolObject.RequireNonNull</c>.</summary>
    public static string ObjRequireNonNull(string receiver) =>
        $"{nameof(CobolObject)}.{nameof(CobolObject.RequireNonNull)}({receiver})";

    /// <summary>Normalize a runtime method-name value for universal dispatch (D-U6) —
    /// <c>CobolObject.NormalizeMethodName</c>.</summary>
    public static string ObjNormalizeMethodName(string nameExpr) =>
        $"{nameof(CobolObject)}.{nameof(CobolObject.NormalizeMethodName)}({nameExpr})";

    /// <summary>Repeat an element image for a table initializer — <c>CobolString.Repeat</c>.</summary>
    public static string StrRepeat(string s, string n) =>
        $"{nameof(CobolString)}.{nameof(CobolString.Repeat)}({s}, {n})";

    // ── Intrinsic functions (CobolIntrinsics / CobolDate / EcFunctions / CobolModule; ISO §15 — P7 Step 12) ──

    /// <summary>A <c>CobolIntrinsics</c> call. <paramref name="method"/> is normally the catalog row's
    /// <c>RuntimeMethod</c> name — <c>IntrinsicCatalog</c> is the single name source, exercised end-to-end by
    /// the intrinsic conformance suite; the TYPE anchor breaks here on a rename.</summary>
    public static string Intrinsic(string method, string args) =>
        $"{nameof(CobolIntrinsics)}.{method}({args})";

    /// <summary>A <c>CobolDate</c> call (the §15 date/time family — same catalog-name discipline).</summary>
    public static string DateFn(string method, string args) =>
        $"{nameof(CobolDate)}.{method}({args})";

    /// <summary>A last-exception interrogation read (§15.28–15.33) — <c>EcFunctions.{method}(args)</c>.</summary>
    public static string EcFn(string method, string args = "") =>
        $"{nameof(Runtime.Exceptions.EcFunctions)}.{method}({args})";

    /// <summary>FUNCTION MODULE-NAME's runtime read (§15.65) — <c>CobolModule.Name(kind)</c>.</summary>
    public static string ModuleNameFn(int kind) =>
        $"{nameof(CobolModule)}.{nameof(CobolModule.Name)}({kind})";

    /// <summary>The COMPILE-TIME WHEN-COMPILED stamp format (a typed passthrough like <see cref="MaskScale"/>):
    /// the §15.99.3 r2 compilation timestamp is baked as a constant with the SAME runtime formatter the
    /// generated CURRENT-DATE call uses.</summary>
    public static string DateFormat21(DateTimeOffset t) => CobolDate.Format21(t);

    /// <summary>The COMPILE-TIME fractional-second count of a literal time format (§15.79 — the result scale
    /// is format-derived at compile time), through the ONE runtime format analyzer.</summary>
    public static int DateFormatFractionDigits(string format) => CobolDate.FormatFractionDigits(format);

    /// <summary>The COMPILE-TIME mask-scale computation (a typed passthrough, not a fragment): the emitters
    /// compute a numeric-edited receiver's fraction scale from its edit mask at compile time with the SAME
    /// runtime routine the generated code uses — one definition, anchored here.</summary>
    public static int MaskScale(string picture, char currency, bool commaMode) =>
        CobolEdit.MaskScale(picture, currency, commaMode);

    /// <summary>The COMPILE-TIME edited-image composition (a typed passthrough): a numeric literal VALUE on a
    /// numeric-edited item bakes its edited image as a constant (ISO §13.18.63 GR6) with the SAME runtime
    /// editor the generated code calls.</summary>
    public static string EditCompose(Int128 value, int valueScale, string picture, bool blankWhenZero, char currency,
        bool commaMode, IReadOnlyList<CobolEdit.EditRule>? edits = null) =>
        CobolEdit.Format(value, valueScale, picture, blankWhenZero, currency, commaMode, edits?.ToArray());
}
