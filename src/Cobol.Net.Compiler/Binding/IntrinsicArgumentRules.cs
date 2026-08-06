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

/// <summary>One argument POSITION's requirement — the §15.x.3 rule for that ordinal, with the clause it was read
/// from. <paramref name="Kind"/> is the code <see cref="IntrinsicArgumentRules.Admissible"/> resolves.</summary>
internal readonly record struct ArgRule(char Kind, string Clause);

/// <summary>A §15.x.3 rule about the argument list AS A WHOLE, which no per-position code can express.</summary>
/// <remarks>
/// ⛔ THIS EXISTS BECAUSE THE RULES DO, AND A PER-POSITION SCREEN WAS SILENTLY IGNORING THEM (fix-queue PB31,
/// and the residue PB12/PB52 recorded three separate times before it was modelled). §15.59.3 r2 and §15.63.3 r2
/// are not decoration: without them <c>FUNCTION MAX(1 "A")</c> is accepted, and every argument passes its own
/// position's check while the LIST is illegal.
/// </remarks>
internal enum CrossArgRule
{
    /// <summary>No cross-argument constraint beyond the per-position kinds.</summary>
    None,

    /// <summary>§15.59.3 r2 / §15.63.3 r2 — "All arguments shall be of the same class with the exception that
    /// mixing of arguments of alphabetic and alphanumeric classes is allowed." (The exception is free here:
    /// <see cref="PicCategory"/> folds alphabetic into alphanumeric, so the two are one class in this model.)</summary>
    AllSameClass,

    /// <summary>§15.37.3 r2 / §15.68.3 r2 / §15.48.3 r3 / §15.79.3 r3 — the positions that share argument-1's
    /// declared kind shall agree in class with argument-1. Positions declared with a DIFFERENT kind (FIND-STRING's
    /// integer argument-3) are governed by their own rule and are not part of this one.</summary>
    MatchArgument1,
}

/// <summary>
/// A catalogued function's §15.x.3 ARGUMENT SCHEMA: what each POSITION requires, what a variadic tail requires,
/// and the cross-argument rule the clause carries.
/// </summary>
/// <remarks>
/// ⛔ THIS REPLACED ONE KIND PER FUNCTION, AND THE OLD SHAPE IS WHY SIX FUNCTIONS COULD NOT BE SCREENED AT ALL
/// (fix-queue PB12). FIND-STRING and the four FORMATTED-* functions take MIXED argument classes — an
/// alphanumeric/national format string PLUS integer operands — so a single-kind row would have screened every
/// position by the first one's class and REJECTED LEGAL SOURCE. That is not a gap in the table, it is a gap in
/// the table's SHAPE, and CLAUDE.md rule 5 says the answer is the restructuring rather than the smallest diff
/// (<c>feedback_model_the_rule_shape_not_one_case</c>).
/// <para>
/// ⚠ The uniform case stays a one-liner (<see cref="Uniform"/>) and every pre-existing row migrated to it
/// unchanged — a schema whose <see cref="Positions"/> is empty and whose <see cref="Tail"/> carries the one kind
/// is EXACTLY the old behaviour, position for position, which is what made the restructuring provable rather
/// than merely tested.
/// </para>
/// </remarks>
internal sealed record ArgSchema(ArgRule[] Positions, ArgRule? Tail, CrossArgRule Cross, string CrossClause)
{
    /// <summary>The rule governing argument position <paramref name="i"/> (0-based), or null when the schema
    /// says nothing about it — a position past the declared ones with no variadic tail is UNSCREENED, never
    /// screened by the previous position's kind.</summary>
    public ArgRule? At(int i) => i < Positions.Length ? Positions[i] : Tail;

    /// <summary>The positions <see cref="CrossArgRule.MatchArgument1"/> governs: those declaring the SAME kind as
    /// argument-1. Derived from the table rather than listed beside it, so a new row cannot forget to say which
    /// positions participate — and it is the honest reading, since "argument-2 shall be of the same class as
    /// argument-1" is only meaningful where both were declared in the same family.</summary>
    public IEnumerable<int> MatchedPositions()
    {
        if (Positions.Length == 0) yield break;
        char first = Positions[0].Kind;
        for (int i = 0; i < Positions.Length; i++)
            if (Positions[i].Kind == first) yield return i;
    }
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
    /// <remarks>
    /// ⛔ THIS IS A VIEW OF <see cref="CandidateClasses"/>, NOT A SECOND CLASSIFIER. "The class of this operand"
    /// is the right question only for an operand that HAS one; a figurative constant does not (§8.3.3.6.4 GR4 —
    /// ZERO is "the numeric value '0' … or … the character '0' … DEPENDING ON CONTEXT"). Modelling the answer as
    /// a SET and reading the singleton off it keeps both questions on one table
    /// (<c>feedback_model_the_rule_shape_not_one_case</c>); the previous scalar-only shape is why a figurative
    /// had to fall out of the screen entirely as "not statically decidable".
    /// </remarks>
    public static CobolClass? ClassOf(BoundOperand op) =>
        CandidateClasses(op) is [var only] ? only : null;

    /// <summary>
    /// Every ISO §8.5.2.1 Table-2 class this operand is capable of presenting — a singleton for everything with a
    /// fixed class, EMPTY when the class is not statically decidable, and genuinely plural only for a figurative
    /// constant, whose class is chosen by the context it appears in.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>§8.3.3.6.4 GR1 + GR4–GR8, read as the table they are.</b> GR1: "When a figurative constant is used in a
    /// context requiring national characters, the figurative constant represents a national character value.
    /// Otherwise, when a figurative constant represents a character value, the figurative constant represents an
    /// alphanumeric character value." GR4 (Format 1) then makes ZERO the exceptional one: it "represents the
    /// numeric value '0', one or more of the boolean character '0', or one or more of the character '0' in the
    /// computer's runtime coded character set, depending on context." GR5/GR6/GR7/GR8 give SPACE, HIGH-VALUE,
    /// LOW-VALUE and QUOTE a CHARACTER reading only — none of them names a numeric or boolean value.
    /// </para>
    /// <para>
    /// ⛔ SO THE ASYMMETRY IS THE POINT, and it is what a scalar column could not express: <b>ZERO is admissible
    /// in a numeric argument position and in a string one; SPACE and its siblings are admissible only in a string
    /// one.</b> §8.3.3.6.3 SR1a says the same thing from the other side — "If the literal is restricted to a
    /// numeric literal, the only figurative constant permitted is ZERO (ZEROS, ZEROES) without the ALL phrase" —
    /// and §8.8.1.1 lists exactly one figurative among the operands an arithmetic expression may be built from.
    /// </para>
    /// <para>
    /// ⚠ <c>ALL literal-1</c> is NOT class-neutral and is deliberately screened here: SR1a's "without the ALL
    /// phrase" bars even <c>ALL "5"</c> from a numeric position, so it carries its literal's own class. Before
    /// this, <c>FUNCTION ABS(ALL "5")</c> compiled clean and aborted at run time on <c>bound operand
    /// 'BoundAllLiteral'</c> — the wrong-stage family again, on source the standard rejects.
    /// </para>
    /// </remarks>
    public static CobolClass[] CandidateClasses(BoundOperand op) => op switch
    {
        // §8.3.3.6.4 GR4 — the zero format is the numeric value '0', the boolean character '0', OR the character
        // '0'; GR1 makes that character national in a national context. Four candidates, and the context picks.
        BoundFigurative { Kind: 'Z' } =>
            [CobolClass.Numeric, CobolClass.Alphanumeric, CobolClass.National, CobolClass.Boolean],
        // GR5/GR6/GR7/GR8 — SPACE, HIGH-VALUE, LOW-VALUE, QUOTE are CHARACTER values, alphanumeric by GR1 or
        // national in a national context. Never numeric, never boolean.
        BoundFigurative { Kind: 'S' or 'H' or 'L' or 'Q' } => [CobolClass.Alphanumeric, CobolClass.National],
        // NULL/NULLS is the pointer figurative (§8.3.3.7) — class pointer, and no §15 argument rule admits it.
        BoundFigurative { Kind: 'N' } => [CobolClass.Pointer],
        BoundFigurative => [],                                   // an unmodelled kind screens as before: fail open
        BoundAllLiteral all => ClassOfCategory(all.Category) is { } ac ? [ac] : [],
        _ => ClassOf1(op) is { } c ? [c] : [],
    };

    /// <summary>The single-class classifier for every operand whose class is fixed — the original body of
    /// <see cref="ClassOf"/>, now reached through <see cref="CandidateClasses"/> so there is one table.</summary>
    private static CobolClass? ClassOf1(BoundOperand op) => op switch
    {
        // ⛔ A LITERAL HAS A CATEGORY, AND THIS IGNORED IT — the PB1 trap, sprung a second time and caught by the
        // corpus, not by reading. `BoundStringLiteral` carries `Category` (Alphanumeric by default, but National
        // for N"…" and BOOLEAN for B"…"), and returning a flat Alphanumeric made the brand-new §15.45.3 r1
        // boolean screen REJECT `FUNCTION INTEGER-OF-BOOLEAN(B"00000101")` — legal source, written in
        // `2002/intrinsics_boolean_conv` and in a spec-derived unit test. A screen is only as good as its
        // classifier, and a classifier that flattens a category is how a correct rule rejects a correct program.
        BoundStringLiteral sl => ClassOfCategory(sl.Category) ?? CobolClass.Alphanumeric,
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
    /// <summary>ONE kind for every position — the shape most §15.x.3 argument rules actually have ("argument-1
    /// shall be of class numeric", with argument-1 repeating for a variadic list). Expressed as a variadic TAIL
    /// with no fixed positions, which is precisely what the pre-schema table did for every row.</summary>
    private static ArgSchema Uniform(char kind, string clause, CrossArgRule cross = CrossArgRule.None,
        string crossClause = "") => new([], new ArgRule(kind, clause), cross, crossClause);

    /// <summary>Per-POSITION kinds, for a rule that constrains its ordinals differently — FIND-STRING's two
    /// string operands plus an integer, the FORMATTED-* family's format literal plus its numeric values.
    /// <paramref name="tail"/> is the kind for positions past the declared ones (a variadic tail), or
    /// <c>'\0'</c> for none — and NONE is the honest default: a position the clause does not describe is
    /// UNSCREENED, never screened by whatever the last declared position happened to be.</summary>
    private static ArgSchema Schema(string clause, char[] kinds, char tail = '\0',
        CrossArgRule cross = CrossArgRule.None, string crossClause = "") =>
        new([.. kinds.Select(k => new ArgRule(k, clause))],
            tail == '\0' ? null : new ArgRule(tail, clause), cross, crossClause);

    public static readonly IReadOnlyDictionary<string, ArgSchema> Verified =
        new Dictionary<string, ArgSchema>(StringComparer.OrdinalIgnoreCase)
        {
            ["ABS"] = Uniform('n', "§15.7.3 r1"),                            // shall be of class numeric
            ["ORD"] = Uniform('s', "§15.70.3 r1"),                           // category alphabetic/alphanumeric/national
            ["ORD-MAX"] = Uniform('p', "§15.71.3 r1"),                       // NOT boolean/message-tag/object/pointer
            ["ORD-MIN"] = Uniform('p', "§15.72.3 r1"),                       // NOT boolean/message-tag/object/pointer
            ["PRESENT-VALUE"] = Uniform('n', "§15.74.3 r1"),                 // argument-1 and argument-2 class numeric
            ["RANDOM"] = Uniform('n', "§15.75.3 r1"),                        // shall be of class numeric
            ["RANGE"] = Uniform('n', "§15.76.3 r1"),                         // shall be of class numeric
            ["REM"] = Uniform('n', "§15.77.3 r1"),                           // argument-1 and argument-2 class numeric
            ["REVERSE"] = Uniform('s', "§15.78.3 r1"),                       // class alphabetic/alphanumeric/national
            // §15.79.3 r1/r3 — argument-1 is a national or alphanumeric LITERAL (its literal-ness is enforced by
            // the existing COBOLNET1517 arm); argument-2 "shall have the same type as argument-1", so both sit in
            // the string family for the purposes of a class screen.
            ["SECONDS-FROM-FORMATTED-TIME"] = Uniform('s', "§15.79.3 r1/r3"),
            // PI takes no arguments (§15.73.2) — present so the drift test can hold this table and the Phase-B
            // batch's function list in agreement rather than silently tolerating a gap.
            ["PI"] = Uniform(' ', "§15.73.2 — no arguments"),

            // ── Phase-B batch 2 (§15.8–15.19). Eight of its twelve functions; the other four are listed under
            //    DELIBERATELY UNSCREENED below, because their argument rule is not a class constraint at all.
            ["ACOS"] = Uniform('n', "§15.8.3 r1"),                           // shall be of class numeric
            ["ASIN"] = Uniform('n', "§15.10.3 r1"),                          // shall be of class numeric
            ["ATAN"] = Uniform('n', "§15.11.3 r1"),                          // shall be of class numeric
            ["ANNUITY"] = Uniform('n', "§15.9.3 r1"),                        // argument-1 class numeric; r3 makes
                                                                      // argument-2 an integer, also class numeric
            ["CHAR"] = Uniform('i', "§15.15.3 r1"),                          // shall be an integer
            ["CHAR-NATIONAL"] = Uniform('i', "§15.16.3 r1"),                 // shall be an integer
            ["BOOLEAN-OF-INTEGER"] = Uniform('i', "§15.13.3 r1/r2"),         // both arguments positive integers
            // §15.17.3 r1/r2 — argument-1 "in integer date form", argument-2 "in standard numeric time form".
            // Both are numeric FORMS, so the class screen is the same for each position even though the two
            // rules differ in what they additionally require of the VALUE.
            ["COMBINED-DATETIME"] = Uniform('n', "§15.17.3 r1/r2"),

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
            ["EXP"] = Uniform('n', "§15.34.3 r1"),                           // shall be of class numeric
            ["EXP10"] = Uniform('n', "§15.35.3 r1"),                         // shall be of class numeric
            ["FRACTION-PART"] = Uniform('n', "§15.42.3 r1"),                 // shall be of the class numeric
            ["INTEGER"] = Uniform('n', "§15.44.3 r1"),                       // shall be of class numeric
            // ⚠ PARTIAL by construction: §15.36.3 r1 is "an integer GREATER THAN OR EQUAL TO ZERO". The 'i' kind
            // screens the CLASS half (§15.3 type 6); the VALUE half is a run-time property this compile-time
            // screen cannot see, so FACTORIAL(-1) still passes here and is EC-ARGUMENT-FUNCTION's business.
            ["FACTORIAL"] = Uniform('i', "§15.36.3 r1"),                     // shall be an integer (>= 0 is a value rule)
            // Zero-argument functions: the general format admits no argument at all, so any argument is an arity
            // error (COBOLNET1504) before class screening is reached. Recorded so the review can close the row.
            ["EXCEPTION-STATEMENT"] = Uniform(' ', "§15.32.2 — no arguments"),
            ["EXCEPTION-STATUS"] = Uniform(' ', "§15.33.2 — no arguments"),

            // ── §15.45–15.57, the review's fifth batch (fix-queue PB19) ─────────────────────────────────────
            // Every rule below was read at its own clause and `cite.py --check`ed. The batch reported these as
            // "entirely unscreened", which they were: CheckArgumentClasses returns at its TryGetValue guard for
            // any function absent from this table, so §15.49.3 r1 was enforced NOWHERE while the adjacent INTEGER
            // and FRACTION-PART — whose rule text is the same sentence — were enforced. That asymmetry is what
            // "the table grows as the review adjudicates each clause" looks like from inside.
            ["INTEGER-OF-DATE"] = Uniform('i', "§15.46.3 r1"),               // an integer of the form YYYYMMDD
            ["INTEGER-OF-DAY"] = Uniform('i', "§15.47.3 r1"),                // an integer of the form YYYYDDD
            ["INTEGER-PART"] = Uniform('n', "§15.49.3 r1"),                  // shall be of class numeric
            ["LOG"] = Uniform('n', "§15.55.3 r1"),                           // shall be of class numeric
            ["LOG10"] = Uniform('n', "§15.56.3 r1"),                         // shall be of class numeric
            // ⚠ PARTIAL, exactly like REVERSE (`AR-15.78.3-1`): §15.57.3 r1 / §15.97.3 r1 are ONE sentence with
            // TWO halves — "of class alphabetic, alphanumeric, or national" AND "at least one character position
            // in length". This screens the class half only. Half a rule enforced is PARTIAL, never CONFORMS.
            ["LOWER-CASE"] = Uniform('s', "§15.57.3 r1"),                    // class alphabetic/alphanumeric/national
            ["UPPER-CASE"] = Uniform('s', "§15.97.3 r1"),                    // the SAME sentence, verbatim
            // §15.48.3 r1 makes argument-1 a national or alphanumeric LITERAL (its literal-ness is the existing
            // COBOLNET1517 arm, its CONTENT the COBOLNET1631 format screen); r3 makes argument-2 "a data item of
            // the same type as argument-1". Both positions are therefore in the string family, so one kind serves
            // both — the same shape SECONDS-FROM-FORMATTED-TIME already uses.
            // ⚠ RESIDUE: r3 is a CROSS-ARGUMENT rule ("the SAME type as argument-1"), which a per-position screen
            // cannot express — the sibling of `AR-15.79.3-3`. This closes the class half, which is the half that
            // was silently accepting a class-numeric argument-2; the cross-argument half stays PARTIAL.
            ["INTEGER-OF-FORMATTED-DATE"] = Uniform('s', "§15.48.3 r1/r3"),
            // ⛔ THE ONE THAT NEEDED PB20 FIRST. §15.45.3 r1 is "Argument-1 shall be of class BOOLEAN" — the only
            // rule in the catalogue that names that class, and `Admissible` had no arm able to say it. It is
            // listed last because it was BLOCKED, not forgotten: until PB20, every reference-modified operand was
            // typed class ALPHANUMERIC on the authority of a clause that does not exist, so this screen would
            // have rejected the standard's OWN Annex D worked example, `FUNCTION INTEGER-OF-BOOLEAN (bit (1:6))`.
            // §8.4.3.3.4 GR6 preserves the category, so a ref-modified boolean item is class boolean and the
            // example is admitted. Ordering was the whole risk here.
            ["INTEGER-OF-BOOLEAN"] = Uniform('b', "§15.45.3 r1"),            // shall be of class boolean

            // ── fix-queue PB30 · eight functions whose §15.x.3 rule no code consulted ────────────────────────
            // Each read at its own clause and `cite.py --check`ed. MEAN/MEDIAN/MIDRANGE are VARIADIC and their
            // rule names argument-1 only, which is the tail form: "argument-1 shall be of class numeric" governs
            // every occurrence of the repeated argument, so Uniform is the rule's own shape, not a widening.
            ["MEAN"] = Uniform('n', "§15.60.3 r1"),                          // shall be of class numeric
            ["MEDIAN"] = Uniform('n', "§15.61.3 r1"),                        // shall be of class numeric
            ["MIDRANGE"] = Uniform('n', "§15.62.3 r1"),                      // shall be of class numeric
            ["MOD"] = Uniform('i', "§15.64.3 r1"),                           // argument-1 and argument-2 integers

            // ── fix-queue PB31 · the CROSS-ARGUMENT rule, which no per-position screen could express ─────────
            // §15.59.3 / §15.63.3 r1 is the NEGATIVE class list ('p'); r2 is the list-wide agreement. Both halves
            // now fire: `FUNCTION MAX(1 "A")` was accepted because every argument passed its own position's
            // check while the LIST was illegal.
            // ⚠ RESIDUE, recorded rather than silently dropped: r1's "nor shall it be a strongly-typed group
            // item" and r3's "argument-1 shall not be a zero-length literal" are not class constraints, so this
            // row does not enforce them. Half a rule enforced is PARTIAL, never CONFORMS.
            ["MAX"] = Uniform('p', "§15.59.3 r1", CrossArgRule.AllSameClass, "§15.59.3 r2"),
            ["MIN"] = Uniform('p', "§15.63.3 r1", CrossArgRule.AllSameClass, "§15.63.3 r2"),

            // ── fix-queue PB12 · the MIXED-CLASS functions, which are why the schema exists ──────────────────
            // §15.68.3 r1 makes argument-1 "of category alphanumeric or national"; r2 makes argument-2 "of the
            // same class as argument-1" — a cross-argument rule, so the currency string cannot be national while
            // the subject is alphanumeric. ⚠ The screen runs BEFORE the §15.68.3 r3 default-currency injection,
            // deliberately: that operand is compiler-supplied, not written by the user, and screening it would
            // report a class disagreement on source that contains no argument-2 at all.
            ["NUMVAL-C"] = Schema("§15.68.3 r1/r2", ['s', 's'], cross: CrossArgRule.MatchArgument1,
                crossClause: "§15.68.3 r2"),
            // §15.37.3 — r1: argument-1 of class alphabetic, alphanumeric, or national; r2: argument-2 in the
            // SAME family as argument-1; r3: "argument-3 shall be an integer data item or integer literal".
            // ⛔ THIS ROW IS THE WHOLE ARGUMENT FOR THE SCHEMA. Under one-kind-per-function it would have
            // screened argument-3 as a string and REJECTED the legal `FUNCTION FIND-STRING(a b 2)`.
            ["FIND-STRING"] = Schema("§15.37.3 r1/r2/r3", ['s', 's', 'i'],
                cross: CrossArgRule.MatchArgument1, crossClause: "§15.37.3 r2"),
            // The FORMATTED-* family (§15.38–15.41): argument-1 is "a national or alphanumeric literal" (its
            // LITERAL-ness is the existing COBOLNET1517 arm, its CONTENT the format screen); the remaining
            // positions are date/time VALUES — integer date form (§15.39.3 r3, §15.40.3 r3), standard numeric
            // time form (§15.40.3 r4, §15.41.3 r3), and the integer UTC offset (§15.40.3 r5, §15.41.3 r4).
            // ⚠ Standard numeric time form is 'n', not 'i': §15.3.3 admits a fractional-seconds representation,
            // so screening it as an integer would reject a legal fractional time.
            ["FORMATTED-CURRENT-DATE"] = Schema("§15.38.3 r1", ['s']),
            ["FORMATTED-DATE"] = Schema("§15.39.3 r1/r3", ['s', 'i']),
            ["FORMATTED-DATETIME"] = Schema("§15.40.3 r1/r3/r4/r5", ['s', 'i', 'n', 'i']),
            ["FORMATTED-TIME"] = Schema("§15.41.3 r1/r3/r4", ['s', 'n', 'i']),
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
            // ⛔ HIGHEST-ALGEBRAIC / LOWEST-ALGEBRAIC — and the PB12 note that put them here was WRONG about WHY.
            // It recorded them as "left unscreened until a kind can express 'category numeric or numeric-edited'",
            // which reads as a missing capability. Measured, the truth is the opposite: `BindAlgebraicFold`
            // already enforces §15.43.3 r1 / §15.58.3 r1 IN FULL — the category test, the index exclusion, and
            // the half a class screen structurally cannot express ("shall be a data item … and shall not be an
            // integer function or numeric function", so a literal, expression, group, ref-mod or nested function
            // is refused). Adding an admissible-set kind here would be a SECOND mechanism for a rule already
            // enforced more completely by the first (feedback_one_mechanism_per_job), and the two would drift.
            // ⚠ The finding was "unscreened"; the measurement is "screened by a better mechanism" —
            // validate_the_premise_not_only_the_rule.
            ["HIGHEST-ALGEBRAIC"] = "§15.43.3 r1 is enforced IN FULL by BindAlgebraicFold, including its data-item half",
            ["LOWEST-ALGEBRAIC"] = "§15.58.3 r1 is enforced IN FULL by BindAlgebraicFold, including its data-item half",
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
        // ⛔ 'b' — class BOOLEAN, and §15.45.3 r1 (INTEGER-OF-BOOLEAN) is the only rule in the catalogue that
        // names it. There was no arm able to express this at all before PB19, which is why the function was
        // unscreened rather than merely unlisted: a screen cannot reject what its vocabulary cannot describe.
        // ⚠ IT DEPENDS ON PB20. §8.4.3.3.4 GR6 preserves a reference-modified item's class, so
        // `FUNCTION INTEGER-OF-BOOLEAN (bit-item (1:6))` — the standard's own Annex D worked example — passes.
        // Under the pre-PB20 rule, which typed EVERY ref-mod result class alphanumeric on the authority of a
        // clause that does not exist, this arm would have rejected that example. Adding the row before fixing
        // the class rule would have shipped a screen that rejects the specification's own sample program.
        'b' => [CobolClass.Boolean],
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
    /// <remarks>
    /// ⛔ THE TEST IS AN INTERSECTION, NOT AN EQUALITY, and that is what admits a figurative constant. An operand
    /// is inadmissible only when NONE of the classes it is capable of presenting
    /// (<see cref="CandidateClasses"/>) is one the rule permits — so ZERO passes both a class-numeric rule and a
    /// class-alphanumeric one (§8.3.3.6.4 GR4, "depending on context"), while SPACE passes only the second.
    /// For every operand with a fixed class the set is a singleton and the intersection IS the old equality, so
    /// no previously-screened shape changes verdict.
    /// </remarks>
    public static string? Violation(char kind, BoundOperand op)
    {
        var candidates = CandidateClasses(op);
        if (candidates.Length == 0) return null;                          // not statically decidable — fail open

        if (kind == 'p')
        {
            // §15.59.3 r1 and siblings are a NEGATIVE list, so the operand is rejected only when EVERY class it
            // could present is excluded. A figurative that can be alphanumeric is therefore fine here, which is
            // right: `FUNCTION MAX(SPACE "A")` is two alphanumeric arguments (§15.59.3 r2 — the same class).
            return candidates.All(PolymorphicExcluded.Contains)
                ? $"is of class {Describe(candidates)}, which ISO §15.71.3 excludes from a MAX/MIN-family argument list"
                : null;
        }

        if (Admissible(kind) is not { } ok || candidates.Any(ok.Contains)) return null;

        string wanted = ok.Length == 1
            ? Name(ok[0])
            : string.Join(", ", ok[..^1].Select(Name)) + " or " + Name(ok[^1]);
        return $"is of class {Describe(candidates)}; ISO §15.3 requires class {wanted}";
    }

    /// <summary>The operand's class for a diagnostic — the one it has, or the choice it offers.</summary>
    private static string Describe(CobolClass[] candidates) => candidates.Length == 1
        ? Name(candidates[0])
        : string.Join(" or ", candidates.Select(Name));

    /// <summary>Why this argument LIST violates the schema's cross-argument rule, or <see langword="null"/>.</summary>
    /// <remarks>
    /// ⛔ THE CANDIDATE-SET MODEL MAKES THIS AN INTERSECTION, and that is the whole reason it is three lines
    /// rather than a special case per figurative. "All arguments shall be of the same class" is exactly "some
    /// class is available to every argument": <c>MAX(ZERO 5)</c> intersects to {numeric} because §8.3.3.6.4 GR4
    /// lets ZERO be either; <c>MAX(SPACE "A")</c> intersects to {alphanumeric}; <c>MAX(SPACE 5)</c> and
    /// <c>MAX(1 "A")</c> intersect to EMPTY and are rejected. The PB48 set model was built for a different
    /// question and answers this one for free (CLAUDE.md rule 5 — the shape that makes the next case automatic).
    /// <para>
    /// ⚠ FAIL-OPEN, like every other arm here: an operand whose class is not statically decidable contributes no
    /// constraint rather than an empty intersection, so an undecidable argument can never turn a legal list into
    /// a rejected one.
    /// </para>
    /// </remarks>
    public static string? CrossViolation(ArgSchema schema, IReadOnlyList<BoundOperand> args)
    {
        if (schema.Cross == CrossArgRule.None || args.Count < 2) return null;

        IEnumerable<int> governed = schema.Cross == CrossArgRule.AllSameClass
            ? Enumerable.Range(0, args.Count)
            : schema.MatchedPositions();

        CobolClass[]? common = null;
        foreach (int i in governed)
        {
            if (i >= args.Count) continue;
            var cs = CandidateClasses(args[i]);
            if (cs.Length == 0) continue;                       // not statically decidable — contributes nothing
            common = common is null ? cs : [.. common.Intersect(cs)];
            if (common.Length == 0)
                return schema.Cross == CrossArgRule.AllSameClass
                    ? $"argument-{i + 1} is of class {Describe(cs)}, which cannot agree with the earlier "
                      + $"arguments; ISO {schema.CrossClause} requires all arguments to be of the same class"
                    : $"argument-{i + 1} is of class {Describe(cs)}, which cannot agree with argument-1; "
                      + $"ISO {schema.CrossClause} requires it to be of the same class as argument-1";
        }
        return null;
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
