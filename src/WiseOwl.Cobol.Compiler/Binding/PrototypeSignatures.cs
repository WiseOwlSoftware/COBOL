// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Compiler.Oo;
using WiseOwl.Cobol.Runtime;

namespace WiseOwl.Cobol.Binding;

/// <summary>
/// ⛔ THE ONE "same signature" TEST over two prototypes (kb/Work PB817). The standard writes that sentence
/// TWICE, in adjacent syntax rules of one statement, over two carriers:
/// <list type="bullet">
/// <item>ISO §14.9.39.3 SR20 — "The function-prototypes associated with identifier-12 and identifier-13 shall
/// have the same signature." (SET Format 8, function-pointer-assignment.)</item>
/// <item>ISO §14.9.39.3 SR22 — "If identifier-7 references a restricted program-pointer, identifier-8 shall be
/// the predefined address NULL or shall reference a program-pointer and the program-prototypes associated with
/// identifier-7 and identifier-8 shall have the same signature." (SET Format 9.)</item>
/// </list>
/// The only difference is which namespace resolves the prototype NAME, which the caller has already done, so
/// the RULE is written here once. It is also the rule §13.18.60.4 GR25/GR26 state as the carriers' standing
/// invariant, and §14.8.2.3.2's "if either is a restricted pointer, both shall be restricted and of the same
/// type" screens the argument-passing edge of it.
/// <para><b>What "the same signature" means here, derived.</b> The standard's §3 terms do not define the word;
/// Annex D.9.3.2 does, informatively — "all explicit or implicit prototype information that exists for the
/// specified function is applicable to the function identified by the address". The prototype information a
/// COBOL prototype carries is its PROCEDURE DIVISION header: the positional USING formals (their descriptions
/// and their passing modes, §14.8.2) and the RETURNING item (§14.8.3, whose §14.8.3.1 makes its very presence a
/// conformance fact — "if and only if"). So this compares exactly those, position by position, through
/// <see cref="OoConformance.SignatureDifference"/> — the ONE pair-mode relation over two descriptions
/// (<c>ActivationRelations.SignatureDifference</c>) that the override, IMPLEMENTS, interface and property-accessor
/// checks ask too, so a description rule cannot mean one thing here and another there (kb/Work PB3101) — except that a
/// restricted program- or function-pointer formal is asked by what it may CONTAIN, its prototype's signature class,
/// where the pair-mode checks ask the TO phrase's name (<c>pointersBySignature</c>, kb/Work PB4442) — and, for an
/// alphanumeric group, through its subordinate entries as well (<c>SameDescription</c>).</para>
/// <para><b>Every prototype has a signature.</b> A prototype that took ISO §12.3.8.4 GR10 c) — "the details are taken
/// from the external repository" — has the signature the repository entry records, decoded from the assembly that
/// publishes it (<c>MetadataRepositoryProvider</c>, DESIGN-external-repository §8.2), and a specifier no provider answers is
/// refused (REPO-1) and registers no prototype. So this comparison has no "unknown" arm: a caller that holds a NAME looks
/// it up first, and a name its tables do not hold was refused where it was declared (kb/Work PB2103).</para>
/// </summary>
internal static class PrototypeSignatures
{
    /// <summary>True when the two prototypes have the same signature (see the type remarks for the derivation). A formal
    /// that is itself a
    /// restricted program- or function-pointer is compared by the signature class its prototype belongs to
    /// (<see cref="SignatureRestriction"/>), which its description carries as its content restriction; while the classes are being computed that is
    /// the class of the current round of <see cref="PrototypeSignatureClasses"/>' partition refinement, so two prototypes
    /// that name each other or themselves compare by what they describe and never by what they are called (kb/Work
    /// PB2495, PB3101).</summary>
    public static bool Same(CalleeSignature a, CalleeSignature b) => FirstDifference(a, b) is null;

    /// <summary>Where two signatures first differ, in the words a diagnostic names it with (<c>the number of USING
    /// formals</c>, <c>USING formal 2</c>, <c>the RETURNING item</c>), or null when they are <see cref="Same"/>: the ONE
    /// comparison, which <see cref="Same"/> reads and §9.2's repository check (REPO-9) reports from
    /// (DESIGN-external-repository §9.2: "naming the entry and the first differing position").</summary>
    public static string? FirstDifference(CalleeSignature a, CalleeSignature b)
    {
        if (a.Formals.Count != b.Formals.Count)
            return $"the number of USING formals ({a.Formals.Count} and {b.Formals.Count})";
        for (int i = 0; i < a.Formals.Count; i++)
        {
            var (fa, fb) = (a.Formals[i], b.Formals[i]);
            // §14.8.2's positional correspondence carries the MODE as well as the description: a BY VALUE formal
            // and a BY REFERENCE formal of identical description are different prototype information (§14.2.3
            // GR10 copies one and aliases the other), and OPTIONAL changes §14.8.2.1's required count.
            if (fa.ByValue != fb.ByValue) return $"USING formal {i + 1}'s BY VALUE / BY REFERENCE";
            if (fa.Optional != fb.Optional) return $"USING formal {i + 1}'s OPTIONAL";
            if (!SameDescription(fa.Item, fb.Item)) return $"USING formal {i + 1}'s description";
        }
        // §14.8.3.1 — a returning item is present "if and only if"; presence is half the fact, description the other.
        if ((a.Returning is null) != (b.Returning is null)) return "the presence of a RETURNING item";
        return a.Returning is null || SameDescription(a.Returning, b.Returning!) ? null : "the RETURNING item's description";
    }

    /// <summary>ISO §13.7.3 SR2 — a prototype's formal parameter or returning item "shall match the description of
    /// the formal parameters and the returning item in the corresponding function definition or program
    /// definition", the description §8.13 stores as the signature's "description of the parameters". Two halves:
    /// <list type="bullet">
    /// <item>the item's OWN description — <see cref="OoConformance.SignatureDifference"/>, the ONE pair-mode relation
    /// (class and category, the clause identity, ANY LENGTH and the LOCALE identification, the object-reference phrases,
    /// the strong type, a variable-length group's matching information; a bit / national group is the elementary
    /// <c>PIC 1(m)</c> / <c>N(m)</c> item §13.18.29.4 GR1 b) / GR2 b) treat it as, and an alphanumeric group is never
    /// an elementary item; an elementary program- or function-pointer pair is asked by its prototypes' signature class,
    /// <c>pointersBySignature</c>);</item>
    /// <item>for two groups that are NOT bit / national groups, their SUBORDINATE entries (kb/Work PB1115, row
    /// SR-13.7.3-2): such a group's data description entry carries no PICTURE, USAGE or length of its own, so what
    /// describes it is the entries subordinate to it.
    /// Before this half, <c>01 L-G. 05 PIC 9(2). 05 PIC X(2).</c> in a prototype and
    /// <c>01 L-G. 05 PIC X(2). 05 PIC 9(2).</c> in its definition compared equal by WIDTH alone, and the caller —
    /// checked against the prototype — handed the definition a group it reads with the fields swapped. The
    /// structure is compared by §8.5.3.1's essential characteristics
    /// (<see cref="StrongTypeModel.SameElementaryLayout"/>: each elementary item at the same relative position,
    /// of the same length, with the same eight clauses), the standard's own statement of when two group
    /// descriptions describe the same data. A bit or national group is never asked it, opposite an elementary item OR
    /// another bit / national group: §13.18.29.4 GR1 b) / GR2 b) make it the elementary <c>PIC 1(m)</c> /
    /// <c>N(m)</c> item and §13.7.3 states nothing otherwise, so the first half's elementary pair is its whole
    /// description. ⛔ That is also what keeps this relation an EQUIVALENCE, which "the same signature" is (§13.18.60.4
    /// GR25 types a restricted pointer by it) and which <see cref="PrototypeSignatureClasses"/>' founder-based
    /// refinement relies on: were two bit groups of one length still compared by their subordinate split, the
    /// elementary <c>PIC 1(8)</c> would equal both a 4+4 and a 2+6 bit group while those two differed, and a SET
    /// between the 4+4 and the 2+6 pointer would be refused although two SETs through a <c>PIC 1(8)</c> pointer
    /// carry the same address there (kb/Work PB3101, review finding R1-1).</item>
    /// </list></summary>
    private static bool SameDescription(DataItem a, DataItem b) =>
        OoConformance.SignatureDifference(a, b, "prototype's item", "definition's item", pointersBySignature: true) is null
        && (!DescribedBySubordinates(a) || !DescribedBySubordinates(b) || StrongTypeModel.SameElementaryLayout(a, b));

    /// <summary>A group whose description is its subordinate entries' — the "is this a group operation?" test
    /// (<see cref="DataItem.IsAsIfElementary"/>'s remarks): a group that is not a bit / national group.</summary>
    private static bool DescribedBySubordinates(DataItem item) => item.IsPlainGroup;

    /// <summary>⛔ WHAT A RESTRICTED PROGRAM- OR FUNCTION-POINTER ITEM MAY CONTAIN (kb/Work PB2464) — the one reader of
    /// <see cref="DataItem.PointerRestrictionIdentity"/>: the canonical identity (<see cref="CanonicalIdentity"/>) of the
    /// signature class (<see cref="PrototypeSignatureClasses"/>) the item's prototype belongs to, so two pointers restricted
    /// to differently named prototypes of ONE signature have equal identities (ISO §13.18.60.4 GR25 / GR26: the pointer
    /// "shall contain only the predefined address NULL or the address of a program with the same signature"). A pointer the
    /// partition has not stamped names a prototype its unit's tables do not hold — refused where it was declared — and its
    /// identity is its TO phrase (<see cref="NamedRestriction(DataItem)"/>), which no canonical identity
    /// (<see cref="CanonicalPrefix"/>) can equal. Null for an unrestricted pointer and for any other item. ⛔ It answers the SIGNATURE question only — the SET relation (§14.9.39.3 SR20 / SR22, through
    /// <c>ActivationDescription.ContentRestriction</c>), the rules that send to it, and the signature graph's elementary
    /// pointer formal (<see cref="Same"/> through <c>ActivationRelations.SignatureDifference</c>'s <c>pointersBySignature</c>
    /// arm; the pair-mode checks of §9.3.8.2.3 ask the TO phrase by name, kb/Work PB4442). It is NOT the pointer's
    /// type and not its USAGE clause: §14.8.2.3.2's "of the same type", §9.3.6 3 e), §14.8.3.3 and §8.5.3.1's
    /// type-declaration equivalence (a strongly-typed or group formal's member, <c>StrongTypeModel</c>) compare the TO
    /// phrase by the prototype it names (<see cref="NamedRestriction"/>; kb/Work PB3758 / PB4107).</summary>
    public static string? SignatureRestriction(DataItem pointer) =>
        pointer.PointerRestrictionIdentity ?? NamedRestriction(pointer);

    /// <summary>The signature relation over two program- or function-pointer items: their <see cref="SignatureRestriction"/>
    /// values are equal. While <see cref="PrototypeSignatureClasses"/> refines its partition each restricted pointer carries
    /// its prototype's CURRENT class, so this question asked inside the classification is the refinement's own question,
    /// and the greatest fixed point needs no assumption set (kb/Work PB2495, PB3101).</summary>
    public static bool SameSignatureRestriction(DataItem a, DataItem b) =>
        string.Equals(SignatureRestriction(a), SignatureRestriction(b), StringComparison.Ordinal);

    /// <summary>⛔ THE TO PHRASE OF A RESTRICTED PROGRAM- OR FUNCTION-POINTER'S USAGE CLAUSE, AS AN IDENTITY (kb/Work
    /// PB4107): <c>TO</c> and the folded externalized name of the prototype it names — the written name for one the unit's
    /// tables do not hold. ISO §9.3.6 match rule 3 e), §14.8.2.3.2 rule 2 and §14.8.3.3 ask "the same ... USAGE clauses",
    /// and §8.5.3.1's equivalence the same of two type declarations' items: the standard lists the clause texts that match
    /// on meaning (currency strings, DECIMAL-POINT IS COMMA, the LOCALE phrase) and none for the TO phrase, so
    /// <c>TO P1</c> and <c>TO P2</c> are two USAGE clauses whatever the two prototypes' signatures, and a prototype whose
    /// details come from the external repository (§12.3.8.4 GR10 c)) is a name as a group one is (docs/CONFORMANCE.md D-RP1). Null for an
    /// unrestricted pointer and for any other item. The phrase as the entry spells it is the description's reader text
    /// (<c>ActivationDescriptions.ElementaryClauses</c>).</summary>
    public static string? NamedRestriction(DataItem pointer) =>
        pointer.Pic is { RestrictedPrototypeName: { } name } ? NamedRestriction(pointer.RestrictedPrototype?.ExternalizedName ?? name) : null;

    /// <summary><see cref="NamedRestriction(DataItem)"/> for a prototype named <paramref name="externalizedName"/>.</summary>
    public static string NamedRestriction(string externalizedName) => "TO " + CobolNames.Fold(externalizedName);

    /// <summary>The two restrictions of a pointer to a named program prototype: <paramref name="Named"/>, its USAGE clause's
    /// TO phrase (<see cref="NamedRestriction(string)"/>, the identity rules'), and <paramref name="Content"/>, what it may
    /// contain (<see cref="SignatureRestriction"/>'s signature class, the SET relation's) — for the unique program-pointer a
    /// program-address-identifier creates (§8.4.3.13.4 GR3), which has no item to read them from.</summary>
    internal readonly record struct PrototypeRestriction(string Named, string Content);

    /// <summary>⛔ THE CANONICAL, GROUP-INDEPENDENT IDENTITY OF A SIGNATURE CLASS (DESIGN-external-repository §6.4; kb/Work
    /// PB2103): <c>SIG &lt;hex&gt;</c>, the SHA-256 of the canonical encoding of the class's minimized signature graph — the
    /// classes reachable from <paramref name="root"/> numbered in first-visit order, each written as its namespace and its
    /// signature (<see cref="SignatureKey"/>), with every restricted program- or function-pointer inside a signature written
    /// as the number of its class. Two separately compiled groups that describe one signature therefore spell its pointers'
    /// type with ONE string, which the run-time relations compare (§13.18.60.4 GR25: "the address of a program with the same
    /// signature as that identified by the specified program-prototype-name"; GR26 its function twin) — the founder's name
    /// a group's own partition picks is comparable only inside that group. It must agree with <see cref="Same"/>: equal
    /// identities exactly when the signatures are the same, held by <c>SignatureIdentityDriftTests</c>.
    /// <para>The graph is the one <see cref="PrototypeSignatureClasses"/> minimized (its coarsest stable partition, so two
    /// prototypes that name each other are one class when no finite path tells them apart, kb/Work PB3101):
    /// <paramref name="founder"/> gives a class's namespace and a representative signature (every member of a class has the
    /// same signature, so any member describes it), <paramref name="classOf"/> the class of a restricted pointer's
    /// prototype (null for a prototype no class holds). While a class is written, every pointer its signature holds is
    /// stamped with its number (<see cref="DataItem.PointerRestrictionIdentity"/>, the field every description reads) and
    /// restored after.</para></summary>
    public static string CanonicalIdentity<TClass>(TClass root, Func<TClass, (bool Function, CalleeSignature Signature)> founder,
        Func<DataItem, TClass?> classOf) where TClass : class
    {
        var order = new List<TClass> { root };
        var number = new Dictionary<TClass, int>(ReferenceEqualityComparer.Instance) { [root] = 0 };
        var saved = new Dictionary<DataItem, string?>(ReferenceEqualityComparer.Instance);
        var text = new System.Text.StringBuilder();
        try
        {
            for (int i = 0; i < order.Count; i++)
            {
                var (function, signature) = founder(order[i]);
                foreach (var pointer in RestrictedPointersOf(signature))
                {
                    if (classOf(pointer) is not { } cls) continue;
                    if (!number.TryGetValue(cls, out int k)) { number[cls] = k = order.Count; order.Add(cls); }
                    saved.TryAdd(pointer, pointer.PointerRestrictionIdentity);
                    pointer.PointerRestrictionIdentity = string.Create(System.Globalization.CultureInfo.InvariantCulture, $"#{k}");
                }
                text.Append(System.Globalization.CultureInfo.InvariantCulture, $"[{i}:{(function ? "FUNCTION" : "PROGRAM")}{SignatureKey(signature)}]");
            }
        }
        finally
        {
            foreach (var (pointer, identity) in saved) pointer.PointerRestrictionIdentity = identity;
        }
        return CanonicalPrefix + Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(text.ToString())));
    }

    /// <summary>The spelling every canonical identity begins with (<see cref="CanonicalIdentity"/>).</summary>
    public const string CanonicalPrefix = "SIG ";

    /// <summary>Every restricted program- or function-pointer whose signature class a signature's descriptions read, in the
    /// order a description is written: each formal by position, then the RETURNING item — those that ARE such a pointer
    /// (<see cref="DescriptionKey"/>'s class-pointer arm reads <c>ActivationDescription.ContentRestriction</c>). A pointer
    /// SUBORDINATE to a formal, or inside a declaration a restricted data-pointer names, is not one: the §8.5.3.1 keys
    /// (<c>StrongTypeModel.TypeIdentityKey</c>, <c>ElementaryLayoutKey</c>) read its USAGE clause by the prototype it names
    /// (kb/Work PB4107), so numbering its class would make the identity depend on a signature <see cref="Same"/> never
    /// asks.</summary>
    private static IEnumerable<DataItem> RestrictedPointersOf(CalleeSignature signature) =>
        signature.Formals.Select(f => f.Item).Concat(signature.Returning is { } r ? [r] : [])
            .Where(item => item.RestrictedPrototype is not null);

    /// <summary>⛔ <see cref="Same"/> AS A KEY: the text two signatures share exactly when they are the same — the formal
    /// count, each formal's mode (BY VALUE), OPTIONAL phrase and <see cref="DescriptionKey"/> by position, and the RETURNING
    /// item's presence and key (§14.8.3.1's "if and only if"). Data-names and intermediate grouping are not in it (§13.7.3
    /// SR2 matches descriptions, not names).</summary>
    public static string SignatureKey(CalleeSignature signature)
    {
        var key = new System.Text.StringBuilder();
        foreach (var f in signature.Formals)
            key.Append(f.ByValue ? "|VALUE " : "|REFERENCE ").Append(f.Optional ? "OPTIONAL " : "").Append(DescriptionKey(f.Item));
        return key.Append(signature.Returning is { } r ? "|RETURNING " + DescriptionKey(r) : "|NO RETURNING").ToString();
    }

    /// <summary>⛔ <see cref="SameDescription"/> AS A KEY, read from what the relation reads — the item's activation
    /// description (<c>ActivationDescriptions.Of</c>) by the arm <c>ActivationRelations.SignatureDifference</c> takes for it
    /// under <c>pointersBySignature</c>,
    /// and for a group that is not a bit or national group its elementary layout
    /// (<see cref="StrongTypeModel.ElementaryLayoutKey"/>):
    /// <list type="bullet">
    /// <item>an object reference — the kind, the folded class-name or interface-name, and the FACTORY and ONLY phrases
    /// (§9.3.8.2.3 rule 2; an ACTIVE-CLASS description its FACTORY phrase alone);</item>
    /// <item>a strongly-typed group — its §8.5.3.1 type identity;</item>
    /// <item>a variable-length or an alphanumeric group — its shape and length (with the layout, which settles §8.5.1.12's
    /// compatibility: two identical layouts are compatible);</item>
    /// <item>a class pointer — the category and its content restriction (a prototype pointer's signature class), as the
    /// relation compares it;</item>
    /// <item>any other — ANY LENGTH, the category, the §9.3.6 3 e) clause identity and the LOCALE external
    /// identification.</item>
    /// </list>
    /// Two facts cannot key exactly, and both answer the relation's false of an item even against itself, so no two
    /// prototypes share a class through them: an item with no crossing form, and a group whose items cannot be placed — each
    /// keyed by its name. The LOCALE identification compares two literals by case and anything else without (§8.5.3.1); it
    /// is keyed as written, so a literal and a word that differ only in case key apart, as
    /// <see cref="StrongTypeModel.TypeIdentityKey"/> keys them.</summary>
    public static string DescriptionKey(DataItem item)
    {
        if (ActivationDescriptions.Of(item, classes: null) is not { } d) return "UNDESCRIBED " + item.CobolName;
        string own = d.Shape switch
        {
            ActivationShape.ObjectReference => "OBJECT " + (d.ObjectNameIsParameter ? "PARAMETER " : "") + d.ObjectKind switch
            {
                ObjectReferenceKind.Interface => "INTERFACE " + CobolNames.Fold(d.ObjectName ?? ""),
                ObjectReferenceKind.ObjectClass => "CLASS " + CobolNames.Fold(d.ObjectName ?? "")
                    + (d.Factory ? " FACTORY" : "") + (d.Only ? " ONLY" : ""),
                ObjectReferenceKind.ActiveClass => "ACTIVE-CLASS" + (d.Factory ? " FACTORY" : ""),
                var kind => kind.ToString(),
            },
            ActivationShape.StrongGroup => "TYPE " + d.StrongType,
            ActivationShape.VariableLengthGroup => "VARIABLE GROUP",
            ActivationShape.AlphanumericGroup => string.Create(System.Globalization.CultureInfo.InvariantCulture, $"GROUP {d.Positions}"),
            // A class pointer by what it may CONTAIN, the signature class SignatureDifference compares under pointersBySignature
            // (kb/Work PB4442).
            _ => (d.AnyLength ? "ANY LENGTH " : "") + d.Category + " " + (d.ContentRestriction.Length > 0 ? d.ContentRestriction : d.Clauses)
                 + (d.LocaleExternal is { } locale ? (d.LocaleFromLiteral ? " LOCALE L:" + locale : " LOCALE W:" + CobolNames.Fold(locale)) : ""),
        };
        return DescribedBySubordinates(item) ? own + " LAYOUT" + StrongTypeModel.ElementaryLayoutKey(item) : own;
    }
}
