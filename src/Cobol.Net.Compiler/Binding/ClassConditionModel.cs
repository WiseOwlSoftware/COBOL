// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.Editions.Diagnostics;

namespace CobolNet.Binding;

/// <summary>One ISO §8.8.4.4.3 OPERAND rule, as a member of the set a class-condition alternative carries.
/// Each member is a rule of the standard's own shape — "shall reference a data item whose usage is …",
/// "shall not be specified if the category … is …" — so the alternative table below states WHICH rules name
/// an alternative and this enum states WHAT each rule says, once.</summary>
internal enum ClassOperandRule
{
    /// <summary>§8.8.4.4.3 SR4 — "ALPHABETIC, ALPHABETIC-LOWER, ALPHABETIC-UPPER, or class-name-1 shall not be
    /// specified if the category of the data item referenced by identifier-1 is boolean, numeric, or
    /// numeric-edited." ⚠ alphabet-name-1 is NOT in this rule's list, which is why the alphabet-name
    /// alternative carries its own <see cref="ClassConditionModel.AlphabetName"/> kind rather than being folded
    /// onto the class-name one (it was: one kind served both, so broadening SR4 from its boolean half to the
    /// rule as written would have rejected `IF NUM-ITEM IS SOME-ALPHABET`, which SR4 does not name).</summary>
    NotBooleanNumericOrNumericEdited,

    /// <summary>§8.8.4.4.3 SR5 — "BOOLEAN shall not be specified if the category of the data item referenced by
    /// identifier-1 is numeric or numeric-edited." One category short of SR4's list, deliberately: a BOOLEAN
    /// class test over a category-boolean item is the ordinary case.</summary>
    NotNumericOrNumericEdited,

    /// <summary>§8.8.4.4.3 SR3 — "If the alphabet-name-1, ALPHABETIC, ALPHABETIC-LOWER, ALPHABETIC-UPPER,
    /// BOOLEAN, or class-name-1 phrase is specified, identifier-1 shall reference a data-item whose usage is
    /// display or national."</summary>
    UsageDisplayOrNational,

    /// <summary>§8.8.4.4.3 SR8 — "If the NUMERIC phrase is specified, identifier-1 shall reference a data item
    /// whose usage is display or national or whose category is numeric." SR3's rule with the category escape,
    /// which is why NUMERIC is absent from SR3's list and present here.</summary>
    NumericUsageOrCategory,
}

/// <summary>One alternative of the ISO §8.8.4.4.2 class-condition general format: the tag the bound node and
/// the renderer key on, the phrase a diagnostic names it by, and the §8.8.4.4.3 rules that name it — in the
/// order they are APPLIED, category rules before usage rules, so an operand that breaks both is reported
/// against the rule that speaks about its category.</summary>
internal readonly record struct ClassAlternative(char Kind, string Spelling, ClassOperandRule[] Rules);

/// <summary>
/// ⛔ THE ISO §8.8.4.4.2 CLASS-CONDITION ALTERNATIVES AND THEIR §8.8.4.4.3 OPERAND RULES, WRITTEN DOWN ONCE.
/// </summary>
/// <remarks>
/// <para>The general format prints FOURTEEN alternatives in one brace group (the printed page was rendered:
/// PDF page 224 / printed 194 — every keyword alternative underlined, alphabet-name-1 and class-name-1 not,
/// no choice indicator, so exactly one is selected). Which of them this compiler OFFERS is a grammar question
/// (<c>className</c> in <c>Core/CobolExpressions.g4</c>); which OPERAND each admits is this table, and adding
/// an alternative is a row here plus a grammar alternative plus a renderer arm — never a new screen.</para>
/// <para>⛔ IT REPLACED THREE PARTIAL COPIES (kb/Work PB571 + PB590). The alternatives were enumerated in the
/// <c>className</c> grammar rule AND in a second grammar rule <c>classCondition</c> (whose list carried
/// ALPHANUMERIC — which §8.8.4.4.2 does not offer at all — and omitted BOOLEAN, class-name-1 and
/// alphabet-name-1), and the kind was decoded in <c>ConditionBinder.BindClassConditionOn</c> AND again in
/// <c>EvaluateBinder.SubjectAsCondition</c>. The operand SCREEN was a third partial: it returned early unless
/// the operand's category was boolean, so §8.8.4.4.3 SR1 — "Identifier-1 shall not reference a data item of
/// class index, message-tag, object, or pointer" — was never asked, and <c>IF IX IS NUMERIC</c> over a USAGE
/// INDEX item compiled clean and printed TRUE, answering a class question about a class-INDEX item as though
/// its class were numeric (§13.18.60.4 GR10: "The class and category of an index data item are index").</para>
/// <para>⚠ NOT YET OFFERED, and each is one row when it lands: FARTHEST-FROM-ZERO, IN-ARITHMETIC-RANGE and
/// NEAREST-TO-ZERO (SR6 — "identifier-1 shall reference a data item whose category is numeric") and the four
/// FLOAT-INFINITY / FLOAT-NOT-A-NUMBER[-QUIET|-SIGNALING] phrases (SR7 — "a data item described with a
/// standard floating-point usage"). Their rules are deliberately NOT declared here: a lookup nothing reads
/// has never been contradicted (<c>feedback_a_dead_lookup_is_also_unverified</c>).</para>
/// </remarks>
internal static class ClassConditionModel
{
    /// <summary>NUMERIC (§8.8.4.4.4 GR3 n).</summary>
    public const char Numeric = 'N';
    /// <summary>ALPHABETIC (GR3 b).</summary>
    public const char Alphabetic = 'A';
    /// <summary>ALPHABETIC-UPPER (GR3 d).</summary>
    public const char AlphabeticUpper = 'U';
    /// <summary>ALPHABETIC-LOWER (GR3 c).</summary>
    public const char AlphabeticLower = 'L';
    /// <summary>BOOLEAN (GR3 e) — a COBOL-2002 introduction with boolean data.</summary>
    public const char Boolean = 'B';
    /// <summary>class-name-1, a SPECIAL-NAMES user-defined class (GR3 f).</summary>
    public const char ClassName = 'C';
    /// <summary>alphabet-name-1, the coded character set an alphabet identifies (GR3 a). ⚠ A DISTINCT kind from
    /// <see cref="ClassName"/> because SR4 names class-name-1 and NOT alphabet-name-1.</summary>
    public const char AlphabetName = 'S';

    /// <summary>The offered alternatives, each with the §8.8.4.4.3 rules that name it.</summary>
    public static readonly ClassAlternative[] Alternatives =
    [
        new(Numeric, "NUMERIC", [ClassOperandRule.NumericUsageOrCategory]),
        new(Alphabetic, "ALPHABETIC",
            [ClassOperandRule.NotBooleanNumericOrNumericEdited, ClassOperandRule.UsageDisplayOrNational]),
        new(AlphabeticUpper, "ALPHABETIC-UPPER",
            [ClassOperandRule.NotBooleanNumericOrNumericEdited, ClassOperandRule.UsageDisplayOrNational]),
        new(AlphabeticLower, "ALPHABETIC-LOWER",
            [ClassOperandRule.NotBooleanNumericOrNumericEdited, ClassOperandRule.UsageDisplayOrNational]),
        new(Boolean, "BOOLEAN",
            [ClassOperandRule.NotNumericOrNumericEdited, ClassOperandRule.UsageDisplayOrNational]),
        new(ClassName, "class-name-1",
            [ClassOperandRule.NotBooleanNumericOrNumericEdited, ClassOperandRule.UsageDisplayOrNational]),
        new(AlphabetName, "alphabet-name-1", [ClassOperandRule.UsageDisplayOrNational]),
    ];

    /// <summary>The row for <paramref name="kind"/>, or null when the kind is one this table does not offer
    /// (a recovery path's placeholder — the screen then asks SR1 alone, never nothing).</summary>
    public static ClassAlternative? For(char kind)
    {
        foreach (var a in Alternatives) if (a.Kind == kind) return a;
        return null;
    }

    /// <summary>ISO §8.8.4.4.3 SR1 — "Identifier-1 shall not reference a data item of class index, message-tag,
    /// object, or pointer, nor a strongly-typed group, nor a variable-length group" — over EVERY alternative,
    /// which is why it is not a row above. Returns the phrase naming why, or null when SR1 admits the operand.
    /// <para>⛔ THE CLASS HALF DELEGATES TO THE TWO EXISTING DEFINITIONS AND WRITES NO THIRD LIST.
    /// <see cref="ItemCategory.IsIndexMessageTagObjectOrPointer"/> is §13.18.60.3 SR4's phrase reader — the
    /// population that answers for MESSAGE-TAG and FUNCTION-POINTER, whose entries never gain a
    /// <see cref="PicInfo"/> at all — and <see cref="IntrinsicArgumentRules.ClassOf"/> is the §8.5.2.1 Table-2
    /// classifier, which answers for the operand shapes that are not a plain data-item reference (an index-NAME,
    /// an intrinsic of §15.2 item 6's index class, the NULL predefined address). Neither alone is total, and a
    /// hand-written class list beside them would be the third copy this table exists to prevent.</para>
    /// <para>The strongly-typed-group arm keeps its own catalog entry (the COBOLNET1533 strong-typing family,
    /// which is split by rule); the class and variable-length-group arms share this one. SR1 is therefore ONE
    /// rule reported through two codes, stated here so it is not re-argued.</para></summary>
    public static string? Sr1Refusal(BoundOperand op)
    {
        if (op is BoundFieldOperand f)
        {
            var item = f.Place.Item;
            if (ItemCategory.IsIndexMessageTagObjectOrPointer(item))
                return "a data item of class index, message-tag, object, or pointer";
            if (item.IsGroup && ReferenceResolver.HasVariableLengthSubordinate(item))
                return "a variable-length group";
        }
        return IntrinsicArgumentRules.ClassOf(op) is CobolClass.Index or CobolClass.Object or CobolClass.Pointer
            ? "an operand of class index, message-tag, object, or pointer"
            : null;
    }

    /// <summary>Whether <paramref name="rule"/> is BROKEN by an operand whose OPERAND picture is
    /// <paramref name="pic"/> (null = an ordinary alphanumeric group or a picture-less leaf, which fails OPEN:
    /// the screen exists to reject what the standard names, never what this compiler cannot classify).</summary>
    public static bool Violates(ClassOperandRule rule, PicInfo? pic) => pic is not null && rule switch
    {
        ClassOperandRule.NotBooleanNumericOrNumericEdited =>
            pic.Category is PicCategory.Boolean or PicCategory.Numeric or PicCategory.NumericEdited,
        ClassOperandRule.NotNumericOrNumericEdited =>
            pic.Category is PicCategory.Numeric or PicCategory.NumericEdited,
        ClassOperandRule.UsageDisplayOrNational => !DisplayOrNational(pic.Usage),
        ClassOperandRule.NumericUsageOrCategory =>
            !DisplayOrNational(pic.Usage) && pic.Category is not PicCategory.Numeric,
        _ => false,
    };

    private static bool DisplayOrNational(Usage u) => u is Usage.Display or Usage.National;

    /// <summary>The diagnostic code, the clause reference and the rule's own words for
    /// <paramref name="rule"/> — declared beside the predicate that tests it, so a message can never name a
    /// rule the predicate does not test and a code can never outlive its rule (CLAUDE.md rule 1: a citation
    /// that passes <c>--check</c> while pointing at the wrong rule is the failure mode).
    /// <para>SR4 and SR8 keep COBOLNET0844, which has reported them (in their boolean-operand halves) since the
    /// data increment; SR3 and SR5 had no arm at all and get their own codes.</para></summary>
    public static (string Code, string Clause, string Text) Wording(ClassOperandRule rule) => rule switch
    {
        ClassOperandRule.NotBooleanNumericOrNumericEdited => ("COBOLNET0844", "ISO §8.8.4.4.3 SR4",
            "ALPHABETIC, ALPHABETIC-LOWER, ALPHABETIC-UPPER, or class-name-1 shall not be specified if the "
            + "category of the data item referenced by identifier-1 is boolean, numeric, or numeric-edited"),
        ClassOperandRule.NotNumericOrNumericEdited =>
            (DiagnosticCatalog.ClassConditionBooleanCategory.Code, "ISO §8.8.4.4.3 SR5",
            "BOOLEAN shall not be specified if the category of the data item referenced by identifier-1 is "
            + "numeric or numeric-edited"),
        ClassOperandRule.UsageDisplayOrNational =>
            (DiagnosticCatalog.ClassConditionOperandUsage.Code, "ISO §8.8.4.4.3 SR3",
            "if the alphabet-name-1, ALPHABETIC, ALPHABETIC-LOWER, ALPHABETIC-UPPER, BOOLEAN, or class-name-1 "
            + "phrase is specified, identifier-1 shall reference a data-item whose usage is display or national"),
        _ => ("COBOLNET0844", "ISO §8.8.4.4.3 SR8",
            "if the NUMERIC phrase is specified, identifier-1 shall reference a data item whose usage is "
            + "display or national or whose category is numeric"),
    };
}
