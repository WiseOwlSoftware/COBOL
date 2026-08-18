// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;

namespace CobolNet.Binding;

/// <summary>
/// HOW A FUNCTION'S §15.2 TYPE IS DECIDED — the shape of the rule, as a catalog COLUMN.
/// </summary>
/// <remarks>
/// <para>
/// ISO §15.2 classifies every intrinsic function by TYPE (alphanumeric · boolean · national · numeric · integer ·
/// index), and <see cref="IntrinsicSig.Type"/> is that column. For most functions the type is a constant. For
/// TWENTY of them it is NOT: their §15.x.1 "General" clause carries a table whose left column is an ARGUMENT
/// property and whose right column is the function type — "The type of this function depends on the type of
/// argument-1 as follows". A national argument makes the function national; an all-integer argument list makes it
/// integer. That table is a RULE, and a rule needs a place to live.
/// </para>
/// <para>
/// ⛔ <b>WHY THIS ENUM EXISTS: THE CATALOG STOPPED BEING THE SINGLE SOURCE OF RESULT-CATEGORY TRUTH.</b>
/// <c>COBOLNET_INTRINSICS_DESIGN.md</c> D2 promises "one declarative IntrinsicCatalog table … is the single source
/// of result-category truth". It was not. Three mechanisms decided a result category:
/// <list type="number">
///   <item>the scalar <see cref="IntrinsicSig.Type"/> column;</item>
///   <item>a literal <c>RuntimeMethod is "UpperCase" or "LowerCase" or "Reverse"</c> chain in
///         <c>IntrinsicBinder</c> (added by CA25 for three functions);</item>
///   <item>a <c>sig.Name is "MAX" or "MIN"</c> arm inside the <c>ArgKinds == "p"</c> branch (added by V54).</item>
/// </list>
/// Each new function whose result follows its arguments needed a fourth hand-edit that nobody was reminded to
/// make — so the other TEN were never made. Measured 2026-08-04, all silent under-rejections: a national result
/// mislabelled alphanumeric passes straight through the §14.9.25.3 Table-16 MOVE guard, and
/// <c>MOVE FUNCTION TRIM(national-item) TO alphanumeric-item</c> compiled clean where the standard requires a
/// diagnostic. The defect was never in any one of the three mechanisms; it was in there being three
/// (<c>feedback_one_mechanism_per_job</c>). This column restores D2's promise: the RULE is data on the row, and
/// <see cref="IntrinsicResultType.Resolve"/> is the ONE reader.
/// </para>
/// <para>
/// ⭐ <b>THE MEMBERS ARE SPEC SHAPES, NOT FUNCTION NAMES.</b> Twenty functions carry a result-type table and they
/// use SEVEN distinct shapes — ten of them share <see cref="FollowsArgument1"/> alone. A member is added only when
/// the standard genuinely states a new shape, and <c>IntrinsicResultTypeDriftTests</c> re-derives the population
/// from <c>specs/ISO_COBOL.md</c> itself, so a function whose §15.x.1 grows a table and whose row still says
/// <see cref="Fixed"/> fails the build. That is the half of CLAUDE.md rule 5 that makes a restructuring stick.
/// </para>
/// </remarks>
public enum IntrinsicResultRule
{
    /// <summary>
    /// The type is a CONSTANT — <see cref="IntrinsicSig.Type"/> is the whole answer. Either §15.x.1 states it
    /// outright ("The function type is boolean", the seven prose-typed clauses: §15.13, §15.45, §15.51–§15.54,
    /// §15.85) or §15.x.1 says nothing and the type comes from the §15.6 summary table. The default.
    /// </summary>
    Fixed,

    /// <summary>
    /// The type FOLLOWS ARGUMENT-1's category through the two-way string table
    /// (<c>Alphabetic → Alphanumeric · Alphanumeric → Alphanumeric · National → National</c>).
    /// <para>
    /// Ten functions, the largest group: §15.12 BASECONVERT · §15.38 FORMATTED-CURRENT-DATE · §15.39
    /// FORMATTED-DATE · §15.40 FORMATTED-DATETIME · §15.41 FORMATTED-TIME · §15.57 LOWER-CASE · §15.78 REVERSE ·
    /// §15.87 SUBSTITUTE · §15.96 TRIM · §15.97 UPPER-CASE.
    /// </para>
    /// <para>
    /// ⚠ For the four FORMATTED-* functions argument-1 is the FORMAT (a national or alphanumeric LITERAL,
    /// §15.38.3 r1 / §15.39.3 r1 / §15.40.3 r1 / §15.41.3 r1) — so <c>FUNCTION FORMATTED-DATE(N"YYYYMMDD" D)</c>
    /// is a NATIONAL function even though the DATE it renders is an integer. The table keys on argument-1's type,
    /// not on what the function is "about".
    /// </para>
    /// <para>
    /// ⚠ The <c>Alphabetic → Alphanumeric</c> row needs no separate arm: <see cref="PicCategory"/> deliberately
    /// folds <c>PIC A</c> into <see cref="PicCategory.Alphanumeric"/> (see <c>CobolClass</c>'s remarks — inventing
    /// a class the model cannot produce would be dead code reading as coverage), and the row's OUTPUT is
    /// alphanumeric regardless, so the fold is observationally exact here rather than an approximation.
    /// </para>
    /// </summary>
    FollowsArgument1,

    /// <summary>
    /// INTEGER when argument-1 is an integer, otherwise NUMERIC.
    /// <para>
    /// §15.7 ABS and §15.83 SMALLEST-ALGEBRAIC state exactly that two-row table. §15.43 HIGHEST-ALGEBRAIC and
    /// §15.58 LOWEST-ALGEBRAIC state a four-row one — <c>Alphanumeric → Numeric · Integer → Integer ·
    /// National → Numeric · Numeric → Numeric</c> — which is the SAME rule: every non-integer row yields numeric.
    /// The extra rows record which argument categories are ADMITTED, not a different type rule.
    /// </para>
    /// </summary>
    IntegerFollowsArgument1,

    /// <summary>
    /// INTEGER when EVERY argument is an integer, otherwise NUMERIC — the variadic form of
    /// <see cref="IntegerFollowsArgument1"/>. §15.76 RANGE and §15.88 SUM: "All arguments integer → Integer ·
    /// Numeric (some arguments may be integer) → Numeric".
    /// </summary>
    IntegerFollowsAllArguments,

    /// <summary>
    /// The full UNIFORM-ARGUMENT-LIST table of §15.59.1 (MAX) and §15.63.1 (MIN) — six rows, the only shape that
    /// reaches every §15.2 type: <c>Alphabetic → Alphanumeric · Alphanumeric → Alphanumeric · Index → Index ·
    /// All arguments integer → Integer · National → National · Numeric (some arguments may be integer) → Numeric</c>.
    /// The argument list is uniform by §15.59.3 / §15.63.3, so the rule reads all arguments and not just the first.
    /// </summary>
    FollowsUniformArguments,

    /// <summary>
    /// §15.18.1 CONCAT — keyed on CLASS <i>and</i> USAGE, because a numeric or boolean argument of usage national
    /// makes the function national while the same class of usage display makes it alphanumeric:
    /// <c>All alphabetic → Alphabetic · Alphanumeric → Alphanumeric · Boolean usage Display → Alphanumeric ·
    /// Numeric usage Display → Alphanumeric · Boolean usage National → National · Numeric usage National →
    /// National · National → National</c>.
    /// <para>
    /// §15.18.4 r2 states the same rule in prose and is the authority for the last row, whose Function-type cell is
    /// EMPTY in the transcription: "If argument-1 is of class or usage national, the function will return a
    /// national value." §15.18.3 r2 makes the argument list uniform in usage, so argument-1 decides for all.
    /// </para>
    /// </summary>
    FollowsConcatArguments,

    /// <summary>
    /// §15.19.1 CONVERT — the ODD ONE OUT: the type is keyed on the DESTINATION FORMAT KEYWORDS, not on any
    /// argument's category. <c>ALPHANUMERIC alone → Alphanumeric · ALPHANUMERIC and HEX → Alphanumeric ·
    /// NATIONAL alone → National · NATIONAL and HEX → National · BYTE → Alphanumeric</c>. Reading it as an
    /// argument rule would give <c>FUNCTION CONVERT(x NATIONAL)</c> an alphanumeric result.
    /// </summary>
    FollowsDestinationFormat,
}

/// <summary>
/// THE ONE READER of <see cref="IntrinsicSig.Result"/> — resolves a call's §15.2 function type from its bound
/// arguments, per the §15.x.1 result-type table the row names.
/// </summary>
/// <remarks>
/// <para>
/// ⭐ <b>IT RESOLVES THE §15.2 TYPE, NOT THE CATEGORY, AND THAT IS DELIBERATE.</b> The obvious shape was to return
/// a <see cref="PicCategory"/> directly, since that is what the binder stores on the bound node. But
/// <see cref="IntrinsicSig.ResultCategory"/> collapses INTEGER and NUMERIC into
/// <see cref="PicCategory.Numeric"/> — a correct collapse for every consumer that exists TODAY, and one that would
/// have made the four integer-following rules unrepresentable, i.e. dead members that read as coverage. Resolving
/// the §15.2 type keeps the standard's own classification intact (design D1: "type every intrinsic by its ISO
/// §15.2 declared function-type"), and the category falls out of it exactly as it does for a fixed row. When a
/// consumer of integer-ness arrives — a function result in a subscript or an §15.3 type-6 argument position — the
/// answer is already correct instead of needing this rule set re-derived.
/// </para>
/// <para>
/// <b>FAIL-SOFT BY CONSTRUCTION.</b> An argument whose category is not statically decidable (a group, a
/// figurative, an ALL literal, an error operand) leaves the row's declared <see cref="IntrinsicSig.Type"/> in
/// place. Every one of these rules can only ever move a result from the declared type to a NARROWER, more
/// specific one; none can invent a type from nothing. A missed resolution is an open inventory row, whereas a
/// wrong one rejects legal source — the same deliberate asymmetry <c>IntrinsicArgumentRules</c> documents.
/// </para>
/// </remarks>
internal static class IntrinsicResultType
{
    /// <summary>
    /// The §15.2 type of this call: the row's declared <see cref="IntrinsicSig.Type"/> for a
    /// <see cref="IntrinsicResultRule.Fixed"/> row, otherwise the type its §15.x.1 table selects for these
    /// arguments.
    /// </summary>
    public static IntrinsicType Resolve(in IntrinsicSig sig, IReadOnlyList<BoundOperand> args) => sig.Result switch
    {
        IntrinsicResultRule.FollowsArgument1 =>
            CategoryOf(args, 0) is PicCategory.National ? IntrinsicType.National : IntrinsicType.Alphanumeric,

        // Integer only when it is KNOWN to be an integer — an undecidable argument keeps the declared Numeric.
        IntrinsicResultRule.IntegerFollowsArgument1 =>
            IsIntegerArg(args, 0) ? IntrinsicType.Integer : IntrinsicType.Numeric,

        // §15.76.1 / §15.88.1 "All arguments integer". An empty list cannot satisfy "all" in any useful sense
        // (and the arity model forbids it for both functions), so it keeps the declared type.
        IntrinsicResultRule.IntegerFollowsAllArguments =>
            args.Count > 0 && AllIntegerArgs(args) ? IntrinsicType.Integer : IntrinsicType.Numeric,

        IntrinsicResultRule.FollowsUniformArguments => UniformArgumentType(sig, args),

        // §15.18.4 r2 — "If argument-1 is of class OR USAGE national, the function will return a national value";
        // §15.18.3 r2 makes the argument list uniform in usage, so argument-1 decides for the whole call.
        // ⚠ THE TWO "usage National" ROWS OF THE §15.18.1 TABLE (Boolean usage National, Numeric usage National)
        // ARE NOT REACHABLE IN THIS COMPILER, and that is a PRE-EXISTING staging, not a hole opened here:
        // `PIC 9 USAGE NATIONAL` / `PIC 1 USAGE NATIONAL` are the §13.18.40.4 SR12 national-form legs, recognized
        // but staged LOUD at the DATA DIVISION (COBOLNET0899, see Usage.National), so no such item can reach a
        // CONCAT call to be classified. Writing an arm for them would be dead code reading as coverage — the
        // failure mode `a_dead_lookup_is_also_unverified` names. When SR12 lands, this arm gains a usage test and
        // the drift test's population is unchanged.
        IntrinsicResultRule.FollowsConcatArguments =>
            CategoryOf(args, 0) is PicCategory.National ? IntrinsicType.National : IntrinsicType.Alphanumeric,

        // CONVERT's destination format is NOT an operand — it is a keyword phrase, so there is nothing here for a
        // generic reader to inspect. `BindConvert` resolves §15.19.1 itself (`dst == 3 ? National : Alphanumeric`)
        // and passes the answer to the node it builds. The arm is written out rather than left to fall through,
        // so the rule is visibly ACCOUNTED FOR at the one place rules are read.
        IntrinsicResultRule.FollowsDestinationFormat => sig.Type,

        IntrinsicResultRule.Fixed => sig.Type,

        // ⚠ THIS CATCH-ALL MEANS A NEW ENUM MEMBER FAILS SILENTLY AS `Fixed`, NOT AT COMPILE TIME — a C# switch
        // expression over an enum warns rather than errors, so exhaustiveness cannot be enforced here.
        // `IntrinsicResultTypeDriftTests.EveryResultRule_IsHandledByTheResolver` is the guard that makes it loud;
        // it is a test precisely because the language will not do it.
        _ => sig.Type,
    };

    /// <summary>
    /// §15.59.1 / §15.63.1, read in the order the table is written. The argument list is uniform
    /// (§15.59.3 / §15.63.3), so argument-1 decides the string and index rows while the integer row must consult
    /// all of them ("ALL arguments integer").
    /// </summary>
    private static IntrinsicType UniformArgumentType(in IntrinsicSig sig, IReadOnlyList<BoundOperand> args)
    {
        if (args.Count == 0) return sig.Type;
        // ⛔ THE INDEX ROW MUST BE TESTED FIRST, AND IT IS NOT A PicCategory. An index data item is PICTURE-less
        // and this model carries it as `PicInfo.IndexItem` — Category NUMERIC with Usage INDEX (DataBinder.cs;
        // IntrinsicBinder's own §13.18.60 note says "class index — not category numeric"). Keying the row on a
        // category would silently route every index argument into the numeric arm, and since an index item has
        // Scale 0 it would then answer INTEGER — a plausible wrong type rather than a visible failure.
        if (IsIndexOperand(args[0])) return IntrinsicType.Index;
        // ⛔ "ARGUMENT-1 DECIDES" IS TRUE ONLY WHEN ARGUMENT-1 HAS A CLASS TO DECIDE WITH (fix-queue PB48).
        // §8.3.3.6.4 GR4 makes the figurative ZERO the numeric value '0' or the character '0' "depending on
        // context", so as argument-1 it carries no category and `CategoryOf(args, 0)` answers null — which fell
        // straight through to the numeric row. `FUNCTION MAX(ZERO "A")` therefore typed its RESULT numeric while
        // IntrinsicBinder chose the STRING comparison body for the same call, and the two halves of one decision
        // disagreeing surfaced as "FUNCTION MAX (no numeric render recipe)" at run time.
        // §15.59.3 r2 is what makes the repair exact rather than a guess: "All arguments shall be of the same
        // class with the exception that mixing of arguments of alphabetic and alphanumeric classes is allowed."
        // Since the list is uniform, ANY argument that has a category has the list's category — so read the
        // first one that does, and the neutral figuratives simply take it.
        return FirstCategory(args) switch
        {
            PicCategory.National => IntrinsicType.National,
            // Alphabetic folds into Alphanumeric in this model, and both rows return Alphanumeric anyway.
            PicCategory.Alphanumeric or PicCategory.NumericEdited => IntrinsicType.Alphanumeric,
            _ => AllIntegerArgs(args) ? IntrinsicType.Integer : IntrinsicType.Numeric,
        };
    }

    /// <summary>USAGE INDEX — §13.18.60: class INDEX, not class numeric, though this model carries it as a
    /// PICTURE-less <c>PicInfo.IndexItem</c> whose category reads Numeric.</summary>
    private static bool IsIndexOperand(BoundOperand op) =>
        op is BoundFieldOperand { Place.Item.Pic.Usage: Usage.Index };

    /// <summary>The statically knowable data category of argument <paramref name="i"/>, or null when it has no
    /// fixed static category (a group, a figurative, an ALL literal, an error operand).</summary>
    private static PicCategory? CategoryOf(IReadOnlyList<BoundOperand> args, int i) =>
        i < args.Count ? OperandCategory(args[i]) : null;

    /// <summary>The category of the first argument that HAS one — the §15.59.1 / §15.63.1 uniform-argument row
    /// read through §15.59.3 r2 (all arguments are of the same class), so a leading class-neutral figurative
    /// (§8.3.3.6.4 GR4) defers to the arguments that do carry a class instead of collapsing the list to numeric.
    /// Null when no argument has a static category, which keeps the numeric/integer rows exactly as before.</summary>
    private static PicCategory? FirstCategory(IReadOnlyList<BoundOperand> args)
    {
        foreach (var a in args)
            if (OperandCategory(a) is { } c) return c;
        return null;
    }

    /// <summary>
    /// ISO §8.5.2.1 — the data category of a bound function argument, TOTAL over the statically categorized
    /// shapes (fix-queue PB59 family 7b). THE one definition; <c>IntrinsicBinder</c> reads it from here rather
    /// than keeping its own copy (<c>feedback_one_rule_one_place</c>). A categorized literal (plain or ALL —
    /// §8.3.3.6.4 GR9 gives an ALL literal its literal's category), a field reference, a nested intrinsic's own
    /// result category, or numeric for a computed arithmetic expression; null only for the genuinely
    /// context-dependent shapes (figuratives, §8.3.3.6.4 GR1/GR4; error operands).
    /// <para>
    /// ⛔ A REF-MOD VIEW TAKES §8.4.3.3.4 GR6's REWRITES THROUGH THE ONE READER (<see cref="RefModPlace.CategoryOf"/>).
    /// An earlier revision read the INNER item's category while its own comment cited GR6 — GR6c rewrites a
    /// numeric view to class-and-category ALPHANUMERIC, so <c>FUNCTION MAX(N9(1:1) "A")</c> is a UNIFORM
    /// alphanumeric list (§15.59.3 r2) whose answer is "A" by the §8.8.4.2.7 relation — and the skipped rewrite
    /// sent it down the numeric row, where it ANSWERED 0 (measured). One citation, two behaviors — the code now
    /// implements the clause it names.
    /// </para>
    /// <para>
    /// ⛔ A GROUP ANSWERS ITS §8.5.2.1 CLASS — "an alphanumeric group item has class and category alphanumeric"
    /// — NOT null. The former null was defended as "the honest answer", and its OWN analysis refutes that: per
    /// §8.5.2.10 item 3 a group is category national only under GROUP-USAGE NATIONAL (§13.18.29, staged), so
    /// every group this compiler can express IS category alphanumeric — a fixed, statically-known category that
    /// null mis-reported as unknowable. The cost was measured twice: <c>FUNCTION MAX(G1 G2)</c> resolved its
    /// TYPE through the numeric row (FirstCategory saw nothing) while <c>IsStringOperand</c> chose the string
    /// BODY, and the two halves of one decision disagreeing crashed at run time ("no numeric render recipe" —
    /// the exact PB48 shape both sites' comments warn about); and <c>DISPLAY-OF(group)</c> sailed past the
    /// §15.26.3 r1 class-national screen. The National/Boolean arms below are the GROUP-USAGE/bit-group legs
    /// (unreachable until those land — <c>ClassOfPlace</c>'s group arm is the same shape, deliberately).
    /// </para>
    /// </summary>
    public static PicCategory? OperandCategory(BoundOperand op) => op switch
    {
        BoundStringLiteral sl => sl.Category,
        BoundNumericLiteral => PicCategory.Numeric,
        BoundFieldOperand { Place: RefModPlace rmp } =>
            rmp.Inner.Item.Pic is { } ip ? RefModPlace.CategoryOf(ip) : PicCategory.Alphanumeric,
        BoundFieldOperand f => f.Place.Item.IsGroup
            ? f.Place.Item.Pic?.Category switch
              {
                  PicCategory.National => PicCategory.National,
                  PicCategory.Boolean => PicCategory.Boolean,
                  _ => PicCategory.Alphanumeric,
              }
            : f.Place.Item.Pic?.Category,
        BoundComputedOperand { Expr: BoundIntrinsicCall ic } => ic.ResultCategory,
        BoundComputedOperand => PicCategory.Numeric,
        BoundAllLiteral al => al.Category,
        // A boolean EXPRESSION operand (the B-op tier, §8.8.2) is class and category boolean — the classifier is
        // TOTAL over the operand kinds so the relation checkpoint asks it instead of re-deriving (kb/Work PB68).
        BoundBoolOperand => PicCategory.Boolean,
        _ => null,
    };

    /// <summary>
    /// §15.2 type 5 — an INTEGER argument: a numeric value with no digit positions to the right of the decimal
    /// point. A numeric literal written without a decimal point is one (§8.3.1.2), a numeric item is one when its
    /// PICTURE has scale 0, and a nested INTEGER-typed function is one. Anything not provably an integer answers
    /// false, which keeps the numeric row — the fail-soft direction.
    /// </summary>
    private static bool IsIntegerArg(IReadOnlyList<BoundOperand> args, int i) =>
        i < args.Count && IsIntegerOperand(args[i]);

    private static bool IsIntegerOperand(BoundOperand op) => op switch
    {
        BoundNumericLiteral n => !n.Text.Contains('.') && !n.Text.Contains(','),
        // Usage INDEX is excluded: it is class index (§13.18.60), not an integer argument, and it reaches here
        // as a scale-0 numeric PicInfo that would otherwise answer true.
        BoundFieldOperand { Place.Item: { IsGroup: false } item } =>
            item.Pic is { Category: PicCategory.Numeric, Scale: 0, Usage: not Usage.Index },
        // ⚠ A NESTED CALL IS ASKED FOR ITS RESOLVED TYPE, NOT ITS DECLARED ONE. Reading `ic.Sig.Type` here would
        // answer NUMERIC for `FUNCTION SUM(FUNCTION ABS(I) J)` — ABS's row DECLARES Numeric and resolves to
        // Integer only against its own argument — so the outer "all arguments integer" row would never be
        // selected through a nested integer function. The recursion is bounded by expression nesting depth.
        BoundComputedOperand { Expr: BoundIntrinsicCall ic } =>
            Resolve(ic.Sig, ic.Args) is IntrinsicType.Integer,
        // The figurative ZERO read numerically IS the integer 0 (§8.3.3.6.4 GR4 — "the numeric value '0'";
        // §8.8.1.1 admits it as an arithmetic operand). It reaches here only in an all-numeric list, because any
        // argument with a character category makes AllIntegerArgs fail on that argument first (PB48).
        BoundFigurative { Kind: 'Z' } => true,
        _ => false,
    };

    private static bool AllIntegerArgs(IReadOnlyList<BoundOperand> args)
    {
        foreach (var a in args)
            if (!IsIntegerOperand(a)) return false;
        return true;
    }
}
