// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.CodeGen.Emit;

namespace CobolNet.CodeGen;

/// <summary>
/// THE renderer of the OBJECT-COMPUTER-derived members of a runtime-module TYPE (ISO/IEC 1989:2023 §12.3.6 — the
/// PROGRAM COLLATING SEQUENCE and the CHARACTER CLASSIFICATION; kb/Work PB64 T5 / PB111): the program emitter
/// calls it for a program class, the OO emitter for an instance class AND a factory class — ONE place, so a clause
/// that the configuration of a CLASS-ID carries (§11.3 — its ENVIRONMENT DIVISION applies to its methods) is
/// declared wherever the renderers reference it. Before PB111 only the program class declared <c>__COLLATE</c> /
/// <c>__CLASSIFY</c>, and a class with either clause was a Roslyn CS0103 on emitted code.
/// <para>The collating carriers are per-TYPE constants (<c>__COLLATE</c> / <c>__COLLATE_NAT</c>). The
/// classification is per ACTIVATION (§12.3.6.4 GR8 "effective with the initial state of the runtime modules";
/// §14.6.6 r2 "on activation of a runtime element"): a PROGRAM carries it in a field assigned by its
/// <c>__Activate</c> prologue (a RECURSIVE program and every function get a fresh instance per activation —
/// ProgramTable — so the field IS per activation); a METHOD, which is re-entered on the SAME object, carries it in
/// a LOCAL declared at the top of its body (<see cref="ClassificationLocal"/>) — each invocation resolves its own,
/// and an inner invocation's resolution never leaks into the outer's.</para>
/// </summary>
internal static class ObjectComputerEmit
{
    /// <summary>The per-type members: the collating carriers and, for a program type, the <c>__CLASSIFY</c> field.</summary>
    public static void EmitMembers(DataBinder data, CodeWriter w, bool classificationField)
    {
        if (data.Collating is { } collate)
            // The NON-native alphanumeric program collating sequence (ISO §12.3.6) as the type's ONE CobolCollation
            // carrier (kb/Work PB101): an AlphanumericCollation for a literal phrase (positions + the §15.15.4 r2
            // representative array + NextFree + the GR8/GR9 extremes — PB59) or a LocaleCollation for the LOCALE
            // phrase; every comparison consumer, MAX/MIN, CHAR/ORD take the object.
            w.Line($"private static readonly CobolCollation __COLLATE = {CollationEmit.New(collate)};");
        if (classificationField && data.Classification is not null)
            // The OBJECT-COMPUTER CHARACTER CLASSIFICATION in effect for this runtime module (ISO §12.3.6.4 GR5–GR8;
            // kb/Work PB64 T5): resolved at EVERY activation (GR8; §14.6.6 r2), in __Activate's prologue, so the word
            // LOCALE binds the locale current when the program is entered. Read by UPPER-CASE / LOWER-CASE without a
            // LOCALE phrase and by the ALPHABETIC class tests.
            w.Line("private CharacterClassification __CLASSIFY = CharacterClassification.None;   // CHARACTER CLASSIFICATION (ISO §12.3.6) — resolved at activation");
        if (data.NationalCollating is { } nat)
            // The NON-native NATIONAL program collating sequence (ISO §12.3.6 GR9/GR11): a SPARSE NationalCollation
            // for an ALPHABET … FOR NATIONAL literal phrase (the runtime computes every unspecified character's
            // §12.3.7.4 GR7 1.3 position arithmetically) or a LocaleCollation.
            w.Line($"private static readonly CobolCollation __COLLATE_NAT = {CollationEmit.New(nat)};");
    }

    /// <summary>The activation-time resolution expression of a classification (§12.3.6.4 GR5 a–j; GR8; §14.6.6 r2).</summary>
    public static string ClassificationResolve(ClassificationSpec cls)
        => $"CharacterClassification.Resolve({Kind(cls.Alphanumeric)}, {Tag(cls.Alphanumeric)}, {Kind(cls.National)}, {Tag(cls.National)})";

    /// <summary>The program prologue statement — assigns the type's <c>__CLASSIFY</c> field.</summary>
    public static string ClassificationPrologue(ClassificationSpec cls)
        => $"__CLASSIFY = {ClassificationResolve(cls)};   // CHARACTER CLASSIFICATION (ISO §12.3.6.4 GR5; §14.6.6 r2)";

    /// <summary>The method-body local — a method is re-entered on the same object, so its classification is an
    /// activation LOCAL the method's dispatch local function captures (the renderers name <c>__CLASSIFY</c> either way).</summary>
    public static string ClassificationLocal(ClassificationSpec cls)
        => $"var __CLASSIFY = {ClassificationResolve(cls)};   // CHARACTER CLASSIFICATION of the class (ISO §12.3.6.4 GR5; §14.6.6 r2 — per method activation)";

    private static string Kind(LocalePhrase? p) => p is null ? "LocalePhraseKind.None" : $"LocalePhraseKind.{p.Kind}";
    private static string Tag(LocalePhrase? p) => p?.Tag is { } t ? EmitText.CsLiteral(t) : "null";
}
