// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;

namespace CobolNet.Binding;

/// <summary>The ISO §8.5.2.1 Table-2 CLASSES.</summary>
/// <remarks>
/// Distinct from <see cref="PicCategory"/>, which is the CATEGORY axis — several categories map to one class, and
/// §15's argument rules are written against the CLASS. The two rows that matter here and are easy to read the
/// wrong way: <b>numeric-edited (usage display) is class ALPHANUMERIC</b>, not numeric; and a group item takes
/// its class from its own kind rather than from an (absent) PICTURE.
/// <para>
/// There is no <c>Alphabetic</c> member because <see cref="PicCategory"/> deliberately folds <c>PIC A</c> into
/// <see cref="PicCategory.Alphanumeric"/> — the distinction is not recoverable here, and inventing a class the
/// model cannot produce would make the §15.3 type-1 arm dead code that reads as coverage.
/// </para>
/// </remarks>
internal enum CobolClass
{
    Alphanumeric,

    /// <summary>
    /// NUMERIC-EDITED with usage display — Table 2 class ALPHANUMERIC, modelled apart so the STRING family can
    /// admit it as the alphanumeric item Table 2 says it is, without the screen having to pretend it is numeric.
    /// </summary>
    /// <remarks>
    /// ⚠ Anywhere a rule says "shall be of CLASS alphanumeric", this counts as alphanumeric — that is what
    /// Table 2 says and this member does not change it.
    /// <para>
    /// ⛔ THE NAME IS NOW HISTORICAL AND THE REASONING IT RECORDS WAS OVERTURNED (owner decision 2026-08-02).
    /// This member used to justify itself by "such an item DE-EDITS to a defined numeric value and is therefore a
    /// legal arithmetic operand" — which is FALSE. §8.8.1.1 admits "an identifier referencing a NUMERIC data
    /// item"; §8.5.2.13 calls this a "numeric-edited data item", a distinct defined term; and §15.43.3 r1 shows
    /// that when the standard means to admit one it says "category numeric OR NUMERIC-EDITED" explicitly, which
    /// §15.3's integer and numeric types do not. De-editing is granted by the MOVE rules (§14.9.25.4 GR5/GR6d1)
    /// and nowhere extended to arithmetic. The <c>'i'</c> and <c>'n'</c> arms therefore BOTH exclude it now;
    /// only the <c>'s'</c> string family admits it. See DEVLOG 1142 and <c>pb1-integer-arg-numeric-edited</c>.
    /// </para>
    /// </remarks>
    NumericEditedDeEditing,
    Boolean,
    National,
    Numeric,
    Object,
    Pointer,
}

/// <summary>
/// The ISO §15.3 ARGUMENT-TYPE screen — the one place a catalogued intrinsic's declared per-argument class is
/// checked against the operand actually written.
/// </summary>
/// <remarks>
/// <para>
/// ⛔ WHY THIS EXISTS. <c>IntrinsicSig.ArgKinds</c> has always declared each argument's required class on all 79
/// catalog rows, and <c>IntrinsicSig.ArgKind</c> was written to read it — but it had ZERO callers. The only
/// consumer of <c>ArgKinds</c> anywhere was <c>IntrinsicBinder</c>'s <c>== "p"</c> MAX/MIN polymorphism test, so
/// the table built to enforce §15's argument rules enforced nothing, and class checking survived only as
/// hand-written arms for a handful of functions (the repertoire pair, CONVERT, CHAR, the ALGEBRAIC family). The
/// Phase-B traceability review surfaced it as 11 separate DIVERGES rows over 11 functions; it is ONE defect
/// (fix-queue <c>PB1</c>). Two reproductions, both silently accepted before this screen existed:
/// <c>MOVE FUNCTION REVERSE(N) TO R</c> with <c>N PIC 9(4)</c> printed <c>4321</c>, and
/// <c>COMPUTE R = FUNCTION ABS(A)</c> with <c>A PIC X(4) VALUE "ABCD"</c> printed <c>0000000{</c>.
/// </para>
/// <para>
/// ⚠ <c>OperandContext.FunctionArgument</c> is NOT the bug and is unchanged. Its doc comment always said "the
/// function's own §15.x argument rule governs, so an alphanumeric operand may be perfectly legal here" — which is
/// correct, and is exactly why the §8.8.1.1 arithmetic screen must stay suppressed for arguments. The design was
/// right; the per-function rule it promised was the missing half. This is that half.
/// </para>
/// <para>
/// <b>ERROR, WITH THE LENIENCY DIALECT-GATED.</b> ISO §4.2.2 paragraph 3: "There are rules in standard COBOL that
/// are not identified as general formats or syntax rules, but nevertheless specify elements that are syntactically
/// distinguishable. This warning mechanism shall indicate violations of such rules."
/// <para>
/// ⚠ Read that paragraph to its END, because it is a DISCRETION clause wrapped around an obligation and the first
/// draft of this comment stated only the obligation: it closes "For elements not specified in general formats or
/// in explicit syntax rules, it is left to the implementor's discretion to determine what is syntactically
/// distinguishable." So the standard does not decree that an argument rule is compile-time checkable — WE
/// determine that, and having determined it, the mechanism "shall indicate violations of such rules". An
/// argument rule qualifies on its merits: it constrains the CLASS of an operand, which is a static property of
/// the source. The obligation is real; it is just downstream of a judgement that is ours to make and to defend.
/// (Caught by an adversarial reviewer reading the clause past the sentence this comment had quoted.)
/// </para>
/// <para>
/// The disposition follows DA6, which settled the
/// sibling §8.8.1.1 question (a wrong-class ARITHMETIC operand) one wave earlier: reject under strict conformance,
/// keep the leniency behind <c>--permissive</c>. Two mechanisms for one question would be the anti-pattern.
/// </para>
/// <para>
/// <b>CLASS, NOT CATEGORY.</b> §15's rules say "shall be of CLASS numeric", and §8.5.2.1 closes with "Use of the
/// name of a data class or data category in the rules of COBOL refers to the category unless class is
/// specifically indicated" — so they resolve through Table 2's CLASS column.
/// </para>
/// <para>
/// <b>FAIL-OPEN BY CONSTRUCTION.</b> An operand whose class cannot be determined statically is never rejected.
/// A false reject turns legal COBOL away, which CLAUDE.md rule 4 forbids outright; a missed one leaves an open
/// inventory row that a later pass revisits. The asymmetry is deliberate.
/// </para>
/// </remarks>
internal static class IntrinsicArgumentRules
{
    /// <summary>The ISO §8.5.2.1 Table-2 class of an operand, or <see langword="null"/> when it is not statically
    /// decidable (an index item and other PIC-less leaves included — those are simply not screened).</summary>
    public static CobolClass? ClassOf(BoundOperand op) => op switch
    {
        BoundStringLiteral => CobolClass.Alphanumeric,
        BoundNumericLiteral => CobolClass.Numeric,
        BoundFieldOperand f => ClassOfPlace(f.Place),
        // An arithmetic expression IS a numeric argument (§15.3 types 6 and 10 admit one outright); a nested
        // intrinsic contributes the class of ITS result category.
        BoundComputedOperand { Expr: BoundIntrinsicCall ic } => ClassOfCategory(ic.ResultCategory),
        BoundComputedOperand => CobolClass.Numeric,
        _ => null,
    };

    private static CobolClass? ClassOfPlace(Place p)
    {
        // ISO §8.4.3.3.4 GR6 — the unique data item reference modification creates has the SAME class and
        // category as identifier-1 except for the three lettered rewrites (PB20). This said "class alphanumeric,
        // always", on the authority of a clause number that DOES NOT EXIST (the old text cited "8.4.2.4";
        // cite.py --check reports no such clause). The rule now lives in ONE
        // place; a ref-modified boolean item is class BOOLEAN and a ref-modified national item class NATIONAL.
        if (p is RefModPlace rmp)
            return rmp.Inner.Item.Pic is { } innerPic ? ClassOfCategory(RefModPlace.CategoryOf(innerPic))
                 : rmp.Inner.Item.IsGroup ? ClassOfPlace(rmp.Inner)   // a ref-modded group takes the group's own class
                 : null;
        // §8.5.2.1 — an alphanumeric group item has class alphanumeric, a bit group boolean, a national group
        // national. A group has no PICTURE of its own, so it cannot fall through to the category table.
        if (p.Item.IsGroup)
        {
            return p.Item.Pic?.Category switch
            {
                PicCategory.National => CobolClass.National,
                PicCategory.Boolean => CobolClass.Boolean,
                _ => CobolClass.Alphanumeric,
            };
        }
        return p.Item.Pic is { } pic ? ClassOfCategory(pic.Category) : null;
    }

    /// <summary>ISO §8.5.2.1 Table 2, read as written.</summary>
    private static CobolClass? ClassOfCategory(PicCategory category) => category switch
    {
        PicCategory.Alphanumeric => CobolClass.Alphanumeric,
        // ⛔ NumericEdited sits under ALPHANUMERIC in Table 2 when its usage is display — which is why it is not
        // a legal argument to a rule demanding CLASS NUMERIC (ABS). It is reported as its own member so a rule
        // demanding §15.3's INTEGER TYPE can still admit it, since it de-edits to a numeric value.
        PicCategory.NumericEdited => CobolClass.NumericEditedDeEditing,
        PicCategory.National => CobolClass.National,
        PicCategory.Numeric => CobolClass.Numeric,
        PicCategory.Boolean => CobolClass.Boolean,
        PicCategory.ObjectReference => CobolClass.Object,
        PicCategory.Pointer or PicCategory.ProgramPointer => CobolClass.Pointer,
        _ => null,                                          // Group is handled by the caller
    };

    /// <summary>
    /// ⛔ THE SCREEN IS DRIVEN FROM HERE, NOT FROM <c>ArgKinds</c> — and the difference is the whole lesson.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The obvious fix for PB1 was "consult <c>sig.ArgKind(i)</c>". Implementing it proved the table is not
    /// merely UNREAD but UNVERIFIED: <c>BYTE-LENGTH</c> is declared <c>"s"</c> while §15.14.3 admits an argument
    /// "of any class", and the nine rows with an EMPTY <c>ArgKinds</c> default to <c>'n'</c>, which would have
    /// screened <c>LENGTH</c> and its family as numeric-only. Those declarations were written as dispatch hints
    /// and drifted freely for years for exactly the reason PB1 exists — nothing read them, so nothing could
    /// contradict them. The comprehensive gate caught 12 corpus programs; every one was legal COBOL.
    /// </para>
    /// <para>
    /// So a function is screened only when its §15 argument rule has been READ AND CITED. An entry here is a
    /// spec-derived fact carrying its clause; a function absent from this table behaves exactly as it did before
    /// PB1, which is why landing this cannot regress anything. The table grows as the Phase-B traceability review
    /// adjudicates each clause — the eleven below are §15.7 and §15.70–15.79, the review's first batch — and
    /// <c>IntrinsicArgumentClassDriftTests</c> holds the two halves in agreement.
    /// </para>
    /// <para>
    /// ⚠ This is deliberately NOT a wider guess. Asserting 68 more argument rules from an unaudited hint column
    /// would not be completeness, it would be fabrication — and every wrong entry rejects legal source, the one
    /// outcome CLAUDE.md rule 4 forbids outright.
    /// </para>
    /// </remarks>
    public static readonly IReadOnlyDictionary<string, (char Kind, string Clause)> Verified =
        new Dictionary<string, (char, string)>(StringComparer.OrdinalIgnoreCase)
        {
            ["ABS"] = ('n', "§15.7.3 r1"),                            // shall be of class numeric
            ["ORD"] = ('s', "§15.70.3 r1"),                           // category alphabetic/alphanumeric/national
            ["ORD-MAX"] = ('p', "§15.71.3 r1"),                       // NOT boolean/message-tag/object/pointer
            ["ORD-MIN"] = ('p', "§15.72.3 r1"),                       // NOT boolean/message-tag/object/pointer
            ["PRESENT-VALUE"] = ('n', "§15.74.3 r1"),                 // argument-1 and argument-2 class numeric
            ["RANDOM"] = ('n', "§15.75.3 r1"),                        // shall be of class numeric
            ["RANGE"] = ('n', "§15.76.3 r1"),                         // shall be of class numeric
            ["REM"] = ('n', "§15.77.3 r1"),                           // argument-1 and argument-2 class numeric
            ["REVERSE"] = ('s', "§15.78.3 r1"),                       // class alphabetic/alphanumeric/national
            // §15.79.3 r1/r3 — argument-1 is a national or alphanumeric LITERAL (its literal-ness is enforced by
            // the existing COBOLNET1517 arm); argument-2 "shall have the same type as argument-1", so both sit in
            // the string family for the purposes of a class screen.
            ["SECONDS-FROM-FORMATTED-TIME"] = ('s', "§15.79.3 r1/r3"),
            // PI takes no arguments (§15.73.2) — present so the drift test can hold this table and the Phase-B
            // batch's function list in agreement rather than silently tolerating a gap.
            ["PI"] = (' ', "§15.73.2 — no arguments"),

            // ── Phase-B batch 2 (§15.8–15.19). Eight of its twelve functions; the other four are listed under
            //    DELIBERATELY UNSCREENED below, because their argument rule is not a class constraint at all.
            ["ACOS"] = ('n', "§15.8.3 r1"),                           // shall be of class numeric
            ["ASIN"] = ('n', "§15.10.3 r1"),                          // shall be of class numeric
            ["ATAN"] = ('n', "§15.11.3 r1"),                          // shall be of class numeric
            ["ANNUITY"] = ('n', "§15.9.3 r1"),                        // argument-1 class numeric; r3 makes
                                                                      // argument-2 an integer, also class numeric
            ["CHAR"] = ('i', "§15.15.3 r1"),                          // shall be an integer
            ["CHAR-NATIONAL"] = ('i', "§15.16.3 r1"),                 // shall be an integer
            ["BOOLEAN-OF-INTEGER"] = ('i', "§15.13.3 r1/r2"),         // both arguments positive integers
            // §15.17.3 r1/r2 — argument-1 "in integer date form", argument-2 "in standard numeric time form".
            // Both are numeric FORMS, so the class screen is the same for each position even though the two
            // rules differ in what they additionally require of the VALUE.
            ["COMBINED-DATETIME"] = ('n', "§15.17.3 r1/r2"),

            // ── §15.32–15.44, the review's fourth batch (fix-queue PB12) ────────────────────────────────────
            // Each row is the function's OWN §15.x.3 argument rule, read and mechanically cited. Six of the
            // thirteen functions the batch adjudicated are NOT here, and the omissions are the point:
            //   · FIND-STRING and the four FORMATTED-* functions take MIXED argument classes (an alphanumeric or
            //     national format string plus integer operands), and this table carries ONE kind for the whole
            //     function. A row would screen every position by the first one's class and reject legal source.
            //     That needs a per-POSITION schema, not an entry — it is the half of PB12 that is a design change.
            //   · HIGHEST-ALGEBRAIC is the sharper omission: §15.43.3 r1 admits "a data item of category numeric
            //     OR NUMERIC-EDITED", and §8.5.2.1 Table 2 puts numeric-edited under class ALPHANUMERIC when its
            //     usage is display — so an 'n' row would REJECT an argument the standard admits. That is exactly
            //     the PB1 trap (12 legal corpus programs rejected by asserting an unaudited kind), so it stays
            //     unscreened until a kind exists that can say "category numeric or numeric-edited".
            ["EXP"] = ('n', "§15.34.3 r1"),                           // shall be of class numeric
            ["EXP10"] = ('n', "§15.35.3 r1"),                         // shall be of class numeric
            ["FRACTION-PART"] = ('n', "§15.42.3 r1"),                 // shall be of the class numeric
            ["INTEGER"] = ('n', "§15.44.3 r1"),                       // shall be of class numeric
            // ⚠ PARTIAL by construction: §15.36.3 r1 is "an integer GREATER THAN OR EQUAL TO ZERO". The 'i' kind
            // screens the CLASS half (§15.3 type 6, which admits an arithmetic expression and de-edits a
            // numeric-edited item); the VALUE half is a run-time property this compile-time screen cannot see, so
            // FACTORIAL(-1) still passes here and is EC-ARGUMENT-FUNCTION's business.
            ["FACTORIAL"] = ('i', "§15.36.3 r1"),                     // shall be an integer (>= 0 is a value rule)
            // Zero-argument functions: the general format admits no argument at all, so any argument is an arity
            // error (COBOLNET1504) before class screening is reached. Recorded so the review can close the row.
            ["EXCEPTION-STATEMENT"] = (' ', "§15.32.2 — no arguments"),
            ["EXCEPTION-STATUS"] = (' ', "§15.33.2 — no arguments"),
        };

    /// <summary>
    /// Functions whose §15 argument rule was READ during the Phase-B review and deliberately left UNSCREENED,
    /// with the reason. Not an oversight, and recorded so it cannot be mistaken for one.
    /// </summary>
    /// <remarks>
    /// A class screen can only express "the argument shall be of class X". These four rules are something else,
    /// and forcing them into the table would reject legal COBOL — which is exactly how the first attempt at PB1
    /// failed its gate on twelve corpus programs.
    /// <list type="bullet">
    ///   <item><b>BYTE-LENGTH</b> §15.14.3 r1 — "a data item of any class or category". There is nothing to
    ///   screen; the catalog's <c>"s"</c> hint is simply wrong, and screening from it rejected legal source.</item>
    ///   <item><b>BASECONVERT</b> §15.12.3 r1 — constrains USAGE ("a usage display or national data item or
    ///   literal"), not class. A usage screen is a different mechanism and does not exist here yet.</item>
    ///   <item><b>CONCAT</b> §15.18.3 r1 — admits "class alphabetic, alphanumeric, boolean, numeric or
    ///   national", i.e. everything except index/object/pointer. Expressible in principle, but its r2/r3 add
    ///   cross-argument USAGE agreement and an unsigned-integer condition, so a class-only screen would give a
    ///   false sense that the rule is enforced.</item>
    ///   <item><b>CONVERT</b> §15.19.3 — the admissible argument depends on the source-format KEYWORD (HEX /
    ///   ANUM / NAT / ANY), so there is no single class for argument-1. Its own arm in the binder owns it.</item>
    /// </list>
    /// </remarks>
    public static readonly IReadOnlyDictionary<string, string> DeliberatelyUnscreened =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["BYTE-LENGTH"] = "§15.14.3 r1 admits an argument of ANY class or category",
            ["BASECONVERT"] = "§15.12.3 r1 constrains USAGE (display or national), not class",
            ["CONCAT"] = "§15.18.3 r1 admits all classes but index/object/pointer; r2/r3 are cross-argument USAGE rules",
            ["CONVERT"] = "§15.19.3 makes the admissible argument depend on the source-format keyword",
        };

    /// <summary>The classes a verified class code admits, or <see langword="null"/> for "no general screen" —
    /// the function's rule is a NEGATIVE list and its own arm owns it.</summary>
    public static CobolClass[]? Admissible(char kind) => kind switch
    {
        // ⛔ 'n' is for a rule that says CLASS NUMERIC in those words — §15.7.3 r1 (ABS), §15.9.3 r1, §15.74.3 r1,
        // §15.75.3 r1, §15.76.3 r1, §15.77.3 r1 and the trig family. Table 2 puts NUMERIC-EDITED under class
        // ALPHANUMERIC, so such an operand is excluded, however numeric it looks.
        // ⚠ THIS WAS CHALLENGED AND THE CHALLENGE WAS WRONG — recorded so it is not re-argued a third time.
        // The argument for widening it to match 'i' runs: §15.3's type 10 "Numeric" admits "an ARITHMETIC
        // EXPRESSION or a numeric data item" (--check verified), which is verbatim what type 6 admits for 'i',
        // so a numeric-edited item should de-edit and be admissible here too. It does not follow, because
        // §8.8.1.1 defines what an arithmetic expression may BE: "an identifier referencing a NUMERIC DATA ITEM,
        // a numeric literal, the figurative constant ZERO …" (--check verified). A numeric-edited item is
        // category numeric-edited and class alphanumeric — not a numeric data item — so it is neither of type
        // 10's two alternatives. The exclusion is correct and `pb1-numeric-arg-numeric-edited` pins it.
        'n' => [CobolClass.Numeric],
        // ⛔ 'i' SCREENS EXACTLY AS 'n' DOES (owner decision 2026-08-02). The comment that stood here argued the
        // opposite — that §15.3 type 6 "admits an ARITHMETIC EXPRESSION — and a numeric-edited item is a legal
        // arithmetic operand, because it DE-EDITS to a defined numeric value" — and cited DA6's screen, a corpus
        // golden and an AssertSpec test as encoding it. ⚠ THAT WAS THE SAME READING THE 'n' ARM BELOW HAD
        // ALREADY REFUTED, so the two arms rested on readings of §8.8.1.1 that could not both be right; the
        // agreement of a golden and a test is not independent evidence when both were written from the same
        // wrong premise. Re-derived, all three citations `--check`ed:
        //   · §8.8.1.1 admits "an identifier referencing a NUMERIC DATA ITEM"; §8.5.2.13 says a numeric-edited
        //     item "is referred to as a NUMERIC-EDITED data item" — a distinct defined term — and §8.5.2.1
        //     Table 2 puts category numeric-edited in class ALPHANUMERIC or NATIONAL, never numeric;
        //   · type 6's only other alternative is "an INTEGER DATA ITEM", which it also is not;
        //   · de-editing is granted by the MOVE rules (§14.9.25.4 GR6d1) and nowhere extended to arithmetic — a
        //     grant that would be unnecessary if de-editing were generally available.
        // So `FUNCTION CHAR(WS-ED)` over a `PIC Z9` is NOT conforming. The distinction between a CLASS rule and
        // §15.3's integer TYPE is still real — it is just not a distinction about numeric-edited.
        'i' => [CobolClass.Numeric],
        // The string family — §15.3 type 1 Alphabetic, type 2 Alphanumeric (which explicitly treats a
        // strongly-typed group as alphanumeric) and type 9 National. Each catalogued 's' argument's own rule
        // names some subset of these; screening their UNION rejects the classes none of them admits (numeric,
        // boolean, object, pointer) without over-rejecting a function whose own rule is narrower. Narrowing
        // per-function is a later refinement, and it can only ADD rejections — never un-reject legal source.
        // Table 2 makes numeric-edited class ALPHANUMERIC, so the string family admits it as such — the
        // distinct member above changes what §15.3's integer type accepts, never what a class rule means.
        's' => [CobolClass.Alphanumeric, CobolClass.NumericEditedDeEditing, CobolClass.National],
        // 'p' — MAX/MIN/ORD-MAX/ORD-MIN, whose rule (§15.71.3 r1 and siblings) is a NEGATIVE list. An
        // admissible-set cannot express it without also excluding classes the rule permits.
        _ => null,
    };

    /// <summary>The classes §15.59.3 / §15.63.3 / §15.71.3 r1 / §15.72.3 r1 EXCLUDE from a polymorphic
    /// (MAX/MIN/ORD-MAX/ORD-MIN) argument: "shall not be of class boolean, message-tag, object, or pointer".</summary>
    public static readonly CobolClass[] PolymorphicExcluded =
        [CobolClass.Boolean, CobolClass.Object, CobolClass.Pointer];

    /// <summary>Why this operand is inadmissible for an argument declaring <paramref name="kind"/>, or
    /// <see langword="null"/> when it is admissible or not statically decidable.</summary>
    public static string? Violation(char kind, BoundOperand op)
    {
        if (ClassOf(op) is not { } actual) return null;

        if (kind == 'p')
        {
            return PolymorphicExcluded.Contains(actual)
                ? $"is of class {Name(actual)}, which ISO §15.71.3 excludes from a MAX/MIN-family argument list"
                : null;
        }

        if (Admissible(kind) is not { } ok || ok.Contains(actual)) return null;

        string wanted = ok.Length == 1
            ? Name(ok[0])
            : string.Join(", ", ok[..^1].Select(Name)) + " or " + Name(ok[^1]);
        return $"is of class {Name(actual)}; ISO §15.3 requires class {wanted}";
    }

    private static string Name(CobolClass c) => c switch
    {
        CobolClass.Alphanumeric => "alphanumeric",
        CobolClass.NumericEditedDeEditing => "alphanumeric (numeric-edited)",
        CobolClass.Boolean => "boolean",
        CobolClass.National => "national",
        CobolClass.Numeric => "numeric",
        CobolClass.Object => "object",
        CobolClass.Pointer => "pointer",
        _ => c.ToString().ToLowerInvariant(),
    };
}
