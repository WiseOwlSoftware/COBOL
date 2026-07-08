// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Editions;
using CobolNet.Frontend.Generated;

namespace CobolNet.Binding;

using Core = CobolParserCore;

/// <summary>
/// The PASS-1 class symbol table of a compilation group (OO deep-dive D1; ISO §11.2/§11.3/§11.7): built from
/// every <c>classDefinition</c> parse context BEFORE any unit binds, so a driver program's INVOKE / typed
/// <c>USAGE OBJECT REFERENCE</c> resolves a class defined LATER in the source file (the cross-unit hard
/// problem — "a genuine two-pass over the compilation group, owned by the BINDER"). Pass-2 (statement binding)
/// consults it to choose each INVOKE's call form and validate the method roster; the emitters only render the
/// bound facts. Class and method names compare case-insensitively (§8.3.2.2).
/// </summary>
public sealed class OoClassTable
{
    private readonly Dictionary<string, OoClassSymbol> _byName = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>All classes in source order (the emitter's deterministic emission order).</summary>
    public IReadOnlyList<OoClassSymbol> Classes => _classes;
    private readonly List<OoClassSymbol> _classes = [];

    /// <summary>All interfaces in source order (§11.6 — emitted as C# interfaces BEFORE the classes, purely
    /// for readability; Roslyn needs no ordering). Interfaces and classes share ONE name namespace
    /// (<see cref="_ifaceByName"/> is checked against <see cref="_byName"/> at Build — a collision is 0840).</summary>
    public IReadOnlyList<OoInterfaceSymbol> Interfaces => _interfaces;
    private readonly List<OoInterfaceSymbol> _interfaces = [];
    private readonly Dictionary<string, OoInterfaceSymbol> _ifaceByName = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The interface named <paramref name="name"/>, or null (case-insensitive, §8.3.2.2).</summary>
    public OoInterfaceSymbol? FindInterface(string name) => _ifaceByName.TryGetValue(name, out var i) ? i : null;
    /// <summary>Covariant-return adapter pairs (§9.3.8.2.3 rules 5a/5c2 — conforming COBOL that C# interface
    /// implementation cannot express directly, because interface implementations demand the EXACT return
    /// type): the emitter renders an EXPLICIT interface implementation
    /// <c>PROTO_RET I_CS.M(…) =&gt; this.M(…);</c> per pair. (Item1 = the interface; Item2 = the prototype;
    /// Item3 = the implementing method; Item4 = true for the FACTORY side.)</summary>
    public List<(OoInterfaceSymbol Iface, OoMethodSymbol Proto, OoMethodSymbol Impl, bool Factory)> AdapterPairs { get; } = [];

    /// <summary>The class named <paramref name="name"/>, or null (COBOL class names are case-insensitive).</summary>
    public OoClassSymbol? Find(string name) => _byName.TryGetValue(name, out var c) ? c : null;

    /// <summary>Validate every override's SIGNATURE against the overridden method (§9.3.8.2 method-signature
    /// conformance: the same formal count with identical descriptions, and identical RETURNING items — via
    /// <see cref="DescriptionMismatch"/>, the ONE description-equality check shared with INVOKE argument
    /// conformance so the two rules can never drift apart). Runs AFTER every class's data has bound (formals
    /// resolve at data-bind time, not pass-1). A violation is COBOLNET0829 — a COBOL-worded bind diagnostic,
    /// never a Roslyn CS0508/CS0115 on user source (the G4 rule).</summary>
    public void ValidateOverrideSignatures(EditionContext edition)
    {
        foreach (var cls in _classes)
            foreach (var m in cls.Methods.Concat(cls.FactoryMethods))
            {
                if (m.OverrideOf is not { } baseM) continue;
                string where = $"class '{cls.Name}', method '{m.Name}' overriding '{baseM.Name}'";
                if (m.Formals.Count != baseM.Formals.Count)
                {
                    edition.Error("COBOLNET0829", $"{where}: {m.Formals.Count} formal parameter(s) vs the "
                        + $"overridden method's {baseM.Formals.Count} (ISO §9.3.8.2 — an override's signature "
                        + "shall conform)");
                    continue;
                }
                for (int i = 0; i < m.Formals.Count; i++)
                    if (DescriptionMismatch(baseM.Formals[i].Item, m.Formals[i].Item) is { } err)
                        edition.Error("COBOLNET0829", $"{where}: formal parameter #{i + 1} "
                            + $"('{m.Formals[i].Item.CobolName}'): {err} (ISO §9.3.8.2)");
                if ((m.Returning is null) != (baseM.Returning is null))
                    edition.Error("COBOLNET0829", $"{where}: RETURNING presence differs from the overridden "
                        + "method (ISO §9.3.8.2)");
                else if (m.Returning is { } r && baseM.Returning is { } br)
                {
                    // §9.3.8.2.3 rules 5a/5c2 — a COVARIANT object-reference RETURNING is legal: a universal
                    // base accepts any object-reference override; a typed base accepts the SAME class or a
                    // SUBCLASS (C# 9+ covariant returns render it directly). Everything else stays the strict
                    // rule-6 identical-description check.
                    if (r.Pic is { Category: PicCategory.ObjectReference } rp
                        && br.Pic is { Category: PicCategory.ObjectReference } brp)
                    {
                        if (ObjectRefWideningMismatch(rp, brp) is { } werr)
                            edition.Error("COBOLNET0829", $"{where}: RETURNING item: {werr} "
                                + "(ISO §9.3.8.2.3 rules 5a/5c2 — the override's class shall be the same "
                                + "class or a subclass of the overridden method's)");
                    }
                    else if (DescriptionMismatch(br, r) is { } rerr)
                        edition.Error("COBOLNET0829", $"{where}: RETURNING item: {rerr} (ISO §9.3.8.2)");
                }
            }
    }

    /// <summary>The §9.3.11 IMPLEMENTS conformance pass (via §9.3.8.2.3 — D-I1: the BINDER is the authority;
    /// Roslyn satisfaction is provably insufficient BOTH directions: the C# projection is lossy for
    /// non-object descriptions [PIC 9(4) and 9(8) both emit `ref long` — Roslyn under-rejects], and C#
    /// interface implementations forbid covariant returns that rules 5a/5c2 PERMIT [Roslyn over-rejects
    /// conforming COBOL — cured by the explicit-implementation adapters]). Runs AFTER all class AND
    /// interface formals resolve. The check runs over the §11.8.4 GR2 transitive CLOSURE (direct IMPLEMENTS
    /// + interface-INHERITed + class-INHERITed) per class; each violation is COBOLNET0841 citing the
    /// numbered rule.</summary>
    public void ValidateImplements(EditionContext edition)
    {
        foreach (var cls in _classes)
        {
            Check(cls, ImplementsClosure(cls, factory: false), factory: false);
            Check(cls, ImplementsClosure(cls, factory: true), factory: true);
        }

        void Check(OoClassSymbol cls, IReadOnlyList<OoInterfaceSymbol> closure, bool factory)
        {
            string side = factory ? "factory " : "";
            foreach (var iface in closure)
                foreach (var proto in iface.AllPrototypes())
                {
                    var impl = factory ? cls.FindFactoryMethod(proto.Name) : cls.FindMethod(proto.Name);
                    if (impl is null)
                    {
                        edition.Error("COBOLNET0841",
                            $"class '{cls.Name}': the {side}interface '{iface.Name}' requires a method "
                            + $"'{proto.Name}' and none is defined or inherited (ISO §9.3.11 — a class "
                            + "shall implement ALL the method prototypes of its interfaces, including "
                            + "inherited ones)");
                        continue;
                    }
                    if (impl.Formals.Count != proto.Formals.Count)
                    {
                        edition.Error("COBOLNET0841",
                            $"class '{cls.Name}', method '{impl.Name}': {impl.Formals.Count} formal(s) vs "
                            + $"the '{iface.Name}' prototype's {proto.Formals.Count} (ISO §9.3.8.2.3 rule 1)");
                        continue;
                    }
                    for (int i = 0; i < impl.Formals.Count; i++)
                        if (DescriptionMismatch(proto.Formals[i].Item, impl.Formals[i].Item) is { } err)
                            edition.Error("COBOLNET0841",
                                $"class '{cls.Name}', method '{impl.Name}', formal #{i + 1}: {err} "
                                + $"(ISO §9.3.8.2.3 rules 2/3 vs interface '{iface.Name}' — identical "
                                + "descriptions; the C# projection cannot check this)");
                    if ((impl.Returning is null) != (proto.Returning is null))
                        edition.Error("COBOLNET0841",
                            $"class '{cls.Name}', method '{impl.Name}': RETURNING presence differs from the "
                            + $"'{iface.Name}' prototype (ISO §9.3.8.2.3 rule 4)");
                    else if (impl.Returning is { } r && proto.Returning is { } pr)
                    {
                        if (r.Pic is { Category: PicCategory.ObjectReference } rp
                            && pr.Pic is { Category: PicCategory.ObjectReference } prp)
                        {
                            if (ObjectRefWideningMismatch(rp, prp) is { } werr)
                                edition.Error("COBOLNET0841",
                                    $"class '{cls.Name}', method '{impl.Name}': RETURNING: {werr} "
                                    + "(ISO §9.3.8.2.3 rules 5a/5c2)");
                            else if (!string.Equals(rp.ObjectClassName, prp.ObjectClassName,
                                         StringComparison.OrdinalIgnoreCase))
                                // Conformant-but-covariant: C# needs the explicit-implementation adapter.
                                AdapterPairs.Add((iface, proto, impl, factory));
                        }
                        else if (DescriptionMismatch(pr, r) is { } rerr)
                            edition.Error("COBOLNET0841",
                                $"class '{cls.Name}', method '{impl.Name}': RETURNING: {rerr} "
                                + "(ISO §9.3.8.2.3 rule 6)");
                    }
                }
        }
    }

    /// <summary>The §11.8.4 GR2 closure: direct IMPLEMENTS + everything an implemented interface INHERITS +
    /// everything an inherited CLASS implements (transitively, cycle-safe).</summary>
    public IReadOnlyList<OoInterfaceSymbol> ImplementsClosure(OoClassSymbol cls, bool factory)
    {
        var result = new List<OoInterfaceSymbol>();
        var seen = new HashSet<OoInterfaceSymbol>();
        var seenCls = new HashSet<OoClassSymbol>();
        for (OoClassSymbol? c = cls; c is not null && seenCls.Add(c); c = c.Base)
            foreach (var direct in factory ? c.FactoryImplements : c.Implements)
                AddWithInherits(direct);
        return result;

        void AddWithInherits(OoInterfaceSymbol i)
        {
            if (!seen.Add(i)) return;
            result.Add(i);
            foreach (var b in i.Inherits) AddWithInherits(b);
        }
    }

    /// <summary>The ONE strict IDENTICAL-DESCRIPTION check — §14.8.2.3.2 (BY REFERENCE parameters ONLY; BY
    /// CONTENT follows §14.8.2.3.3 COMPUTE/MOVE/SET rules in the binder mode dispatch) and §9.3.8.2
    /// override-signature validation. Identical = same category; numeric: same USAGE + SIGN representation +
    /// BLANK WHEN ZERO + digits + scale + sign; alphanumeric: same length + JUSTIFIED; object reference: same
    /// declared class; group: image crossing with equal character length (except the §14.8.2.2 rule-1 BY
    /// REFERENCE prefix case — <paramref name="byRefGroupPrefix"/> allows a SMALLER formal). Null when
    /// conformant. This strictness keeps BY REFERENCE marshaling TYPE-PRESERVING (the slice-2 design fact);
    /// CONTENT conversions qualify the owner class internal profiles instead.</summary>
    /// <summary>The RUNTIME projection of the strict-conformance rule (D-U3 — the universal-dispatch
    /// wave): ONE descriptor string per description, computed at BIND time on both sides of a universal
    /// crossing; the generated <c>__CobolInvoke</c> switch compares for STRING EQUALITY and raises
    /// EC-OO-UNIVERSAL on mismatch (ISO §14.9.23.4 GR7c — §14.8.2/§14.8.3 conformance through a universal
    /// receiver is checked at runtime, §9.3.8.2.1 NOTE). Locked invariant (unit-tested): descriptor
    /// equality ⇔ <see cref="DescriptionMismatch"/> == null over every carried category — derived NEXT TO
    /// the one mismatch function so the two projections cannot drift (feedback_singular_pattern).
    /// Deliberate strictness deltas, both LOUD-fail directions (AS-BUILT notes): equality cannot express
    /// §14.8.2.2 rule 1's by-ref group-PREFIX leniency (a smaller formal group raises EC-OO-UNIVERSAL
    /// through universal where the TYPED path accepts a prefix), and JUSTIFIED is encoded on alphanumeric
    /// items while the group⇄alphanumeric image pairing carries none. Not-carried categories return the
    /// <c>T:!</c> sentinel — bind rejects them from universal crossings (0866) before any box exists, and
    /// the sentinel deliberately matches nothing the callee ever emits as a checked formal.</summary>
    public static string ConformanceDescriptor(DataItem item)
    {
        if (item.IsGroup)
            return item.IsImageCapable ? $"S:{item.ImageWidth}:N" : "T:!";
        if (item.Pic is not { } p) return "T:!";
        return p.Category switch
        {
            PicCategory.ObjectReference =>
                p.ObjectClassName is { } cls ? "O:" + cls.ToUpperInvariant() : "O:*",
            PicCategory.Numeric =>
                $"N:{p.Usage}:{p.Digits}:{p.Scale}:{(p.Signed ? "S" : "U")}:{p.ImageSignKind}:"
                + (item.BlankWhenZero ? "B" : "-"),
            PicCategory.Alphanumeric =>
                $"S:{p.Length}:{(item.Justified ? "J" : "N")}",
            _ => "T:!",
        };
    }

    public static string? DescriptionMismatch(DataItem formal, DataItem arg, bool byRefGroupPrefix = false)
    {
        if (formal.IsGroup)
        {
            if (!(arg.IsGroup || arg.Pic?.Category is PicCategory.Alphanumeric))
                return "a group formal requires a group or alphanumeric argument";
            if (arg.IsGroup && !arg.IsImageCapable)
                return "the argument group has a float/COMP-5/INDEX leaf (no character image — Tier-C)";
            if (!formal.IsImageCapable)
                return "the formal group has a float/COMP-5/INDEX leaf (no character image — Tier-C)";
            // §14.8.2.2 rule 1 (BY REFERENCE): the formal may be SMALLER than (a prefix of) the argument —
            // the callee sees the leading formal-width character positions; the tail survives write-back.
            // Override signatures and RETURNING pairs keep strict equality.
            return byRefGroupPrefix
                ? (formal.ImageWidth > arg.ImageWidth
                    ? $"the formal ({formal.ImageWidth} character positions) exceeds the argument "
                      + $"({arg.ImageWidth}) (ISO §14.8.2.2 rule 1 — the formal shall not be larger)"
                    : null)
                : arg.ImageWidth != formal.ImageWidth
                    ? $"character length mismatch (formal {formal.ImageWidth}, argument {arg.ImageWidth})"
                    : null;
        }
        if (formal.Pic is not { } f)
            return "the formal parameter has no resolvable description (PICTURE-less item — a later slice)";
        if (arg.IsGroup)
        {
            if (f.Category is not PicCategory.Alphanumeric)
                return "a group argument requires a group or alphanumeric formal";
            if (!arg.IsImageCapable) return "the argument group has no character image (Tier-C)";
            return byRefGroupPrefix
                ? (f.Length > arg.ImageWidth
                    ? $"the formal ({f.Length} character positions) exceeds the argument "
                      + $"({arg.ImageWidth}) (ISO §14.8.2.2 rule 1)"
                    : null)
                : arg.ImageWidth != f.Length
                    ? $"character length mismatch (formal {f.Length}, argument {arg.ImageWidth})"
                    : null;
        }
        if (arg.Pic is not { } a)
            return "the argument has no resolvable description (PICTURE-less item — a later slice)";
        if (f.Category != a.Category)
            return $"category mismatch (formal {f.Category}, argument {a.Category})";
        switch (f.Category)
        {
            case PicCategory.ObjectReference:
                return string.Equals(f.ObjectClassName, a.ObjectClassName, StringComparison.OrdinalIgnoreCase)
                    ? null
                    : $"declared class mismatch (formal '{f.ObjectClassName ?? "universal"}', argument "
                      + $"'{a.ObjectClassName ?? "universal"}')";
            case PicCategory.Numeric:
                if (f.Usage != a.Usage)
                    return $"USAGE mismatch (formal {f.Usage}, argument {a.Usage} — §14.8.2.3.2 rule 2 "
                        + "requires the same USAGE clause BY REFERENCE)";
                if (f.ImageSignKind != a.ImageSignKind)
                    return $"SIGN clause mismatch (formal {f.ImageSignKind}, argument {a.ImageSignKind} — "
                        + "§14.8.2.3.2 rule 2: the SIGN clauses shall be the same)";
                if (formal.BlankWhenZero != arg.BlankWhenZero)
                    return "BLANK WHEN ZERO mismatch (§14.8.2.3.2 rule 2)";
                return f.Digits != a.Digits || f.Scale != a.Scale || f.Signed != a.Signed
                    ? $"numeric description mismatch (formal {(f.Signed ? "S" : "")}9({f.Digits}) scale "
                      + $"{f.Scale}, argument {(a.Signed ? "S" : "")}9({a.Digits}) scale {a.Scale})"
                    : null;
            case PicCategory.Alphanumeric:
                if (formal.Justified != arg.Justified)
                    return "JUSTIFIED mismatch (§14.8.2.3.2 rule 2)";
                return f.Length != a.Length
                    ? $"length mismatch (formal X({f.Length}), argument X({a.Length}))"
                    : null;
            default:
                return $"formal category {f.Category} is not yet carried across INVOKE";
        }
    }

    /// <summary>The §14.8.3.3-rule-1 / SET-SR12a2 WIDENING direction for object-reference assignment pairs
    /// (INVOKE RETURNING delivery, BY CONTENT object-reference arguments, covariant override RETURNING —
    /// specs/ISO_COBOL.md:25456-25458, :31340): a UNIVERSAL receiver accepts any object reference; a typed
    /// receiver accepts the SAME class or a SUBCLASS. Distinct from <see cref="DescriptionMismatch"/> strict
    /// identity, which stays correct for BY REFERENCE (§14.8.2.3.2) and invariant override formals.</summary>
    public string? ObjectRefWideningMismatch(PicInfo sender, PicInfo receiver)
    {
        if (receiver.ObjectClassName is not { } recvName) return null;   // universal receiver (SET SR8)
        if (sender.ObjectClassName is not { } sendName)
            return "a UNIVERSAL object reference does not conform to a typed receiver "
                + "(ISO SET SR12 — the sender shall be of the receiver class or a subclass)";
        // An INTERFACE-typed receiver accepts a class that IMPLEMENTS it — through the §11.8.4 GR2 closure
        // (SET SR10/§9.3.8.2 interface conformance) — or an interface that INHERITS it.
        if (FindInterface(recvName) is { } recvIface)
        {
            if (Find(sendName) is { } sc)
                return ImplementsClosure(sc, factory: false).Contains(recvIface)
                    ? null
                    : $"class {sendName} does not implement interface {recvName} (ISO §9.3.8.2/SET SR10)";
            if (FindInterface(sendName) is { } si)
                return si == recvIface || si.Inherits.Contains(recvIface)
                       || ImpliedBy(si, recvIface)
                    ? null
                    : $"interface {sendName} does not inherit interface {recvName}";
            return $"unresolvable sender '{sendName}'";

            static bool ImpliedBy(OoInterfaceSymbol from, OoInterfaceSymbol target)
            {
                var seen = new HashSet<OoInterfaceSymbol>();
                var stack = new Stack<OoInterfaceSymbol>([from]);
                while (stack.Count > 0)
                {
                    var cur = stack.Pop();
                    if (!seen.Add(cur)) continue;
                    if (cur == target) return true;
                    foreach (var b in cur.Inherits) stack.Push(b);
                }
                return false;
            }
        }
        var sendCls = Find(sendName);
        var recvCls = Find(recvName);
        if (sendCls is null || recvCls is null)
            return $"unresolvable class in the pair (sender {sendName} to receiver {recvName})";
        return sendCls.ConformsTo(recvCls)
            ? null
            : $"class {sendName} is not {recvName} or one of its subclasses (ISO SET SR12a2 via §14.8.3.3 rule 1)";
    }

    /// <summary>
    /// Build the table from the group's class definitions (pass-1: identity + roster only — no data or statement
    /// binding). Structural diagnostics raised here, each per its ISO rule: duplicate class name / emitted-type
    /// collision (COBOLNET0820), END CLASS name mismatch (§10.7 — 0820), unknown INHERITS base (§11.3.2 —
    /// COBOLNET0821, never silently a root class), duplicate method name within a class (COBOLNET0822 — the v1
    /// unique-name restriction, deep-dive D9: parametric polymorphism §12063 is OPTIONAL and deferred).
    /// INHERITS emission itself is a later port slice (3a) — a KNOWN base still 0899s until it lands, but the
    /// base link is recorded so method lookup walks the chain from day one.
    /// </summary>
    public static OoClassTable Build(IReadOnlyList<Core.ClassDefinitionContext> classes, EditionContext edition,
        IReadOnlyList<Core.InterfaceDefinitionContext>? interfaces = null)
    {
        var table = new OoClassTable();
        var usedCsNames = new HashSet<string>(StringComparer.Ordinal);

        // ── INTERFACES first (§11.6) — classes' IMPLEMENTS then resolve regardless of source order ──
        foreach (var ictx in interfaces ?? [])
        {
            string iname = ictx.interfaceName(0).GetText();
            ConstructRegistry.Check(edition.Edition, edition, Constructs.InterfaceDefinition2002, $"interface definition '{iname}' (INTERFACE-ID compilation unit)");   // COBOL-2002 introduction, bind-time gate (rearch migration Cluster 7)
            string icsName = DataItem.Sanitize(iname).ToUpperInvariant();
            var isym = new OoInterfaceSymbol(iname, icsName, ictx);
            if (table._ifaceByName.ContainsKey(iname) || table._byName.ContainsKey(iname)
                || !usedCsNames.Add(icsName))
            {
                edition.Error("COBOLNET0840",
                    $"duplicate interface definition '{iname}' — interface and class names share one "
                    + "namespace and shall be unique in the compilation group (ISO §8.3.2.2/§11.6)");
                continue;
            }
            table._ifaceByName.Add(iname, isym);
            table._interfaces.Add(isym);
            foreach (var inh in ictx.interfaceName().Skip(1).Take(ictx.interfaceName().Length - 2))
                isym.InheritNames.Add(inh.GetText());
            if (!string.Equals(ictx.interfaceName(ictx.interfaceName().Length - 1).GetText(), iname,
                    StringComparison.OrdinalIgnoreCase))
                edition.Error("COBOLNET0840",
                    $"END INTERFACE does not match INTERFACE-ID '{iname}' (ISO §10.7)");

            // PROTOTYPES (§10.6.2 SR4): a header + optional LINKAGE-only data division, NO procedure body,
            // NO OVERRIDE/FINAL attributes (§11.7 SR2/SR8 — the OVERRIDE/FINAL wave's forward obligation).
            foreach (var m in ictx.methodDefinition())
            {
                string pname = (m.methodName().Length > 0 ? m.methodName(0)?.GetText() : null)
                    ?? m.methodPropertySelector()?.propertyName()?.GetText() ?? "?";
                var pd = m.procedureDivision();
                if (m.OVERRIDE() is not null || m.FINAL() is not null)
                    edition.Error("COBOLNET0840",
                        $"interface '{iname}', method '{pname}': OVERRIDE/FINAL may not appear in a method "
                        + "PROTOTYPE (ISO §11.7 SR2/SR8)");
                if (pd?.procedureUnit().Length > 0 || pd?.declarativePart().Length > 0)
                    edition.Error("COBOLNET0840",
                        $"interface '{iname}', method '{pname}': a method prototype has no procedure body "
                        + "(ISO §10.6.2 SR4 — header only)");
                if (m.dataDivision() is { } pdd
                    && (pdd.workingStorageSection() is not null || pdd.localStorageSection() is not null
                        || pdd.fileSection() is not null))
                    edition.Error("COBOLNET0840",
                        $"interface '{iname}', method '{pname}': a prototype's data division may carry only "
                        + "a LINKAGE SECTION (ISO §10.6.2 SR4)");
                if (m.methodPropertySelector() is not null)
                {
                    edition.Error("COBOLNET0899",
                        $"interface '{iname}': a GET/SET PROPERTY prototype is recognized but not yet "
                        + "implemented (the property-prototype leg — a later refinement)");
                    continue;
                }
                var proto = new OoMethodSymbol(
                    pname,
                    HasUsing: pd?.usingClause() is not null,
                    HasReturning: pd?.returningClause() is not null,
                    m)
                { CsName = DataItem.Sanitize(pname).ToUpperInvariant() };
                if (!isym.TryAddPrototype(proto))
                    edition.Error("COBOLNET0840",
                        $"interface '{iname}': duplicate method prototype '{pname}' (v1 unique-name rule, D9)");
            }
        }
        // Interface INHERITS resolution + cycle check (§11.6.3 SR2/SR3/SR6).
        foreach (var isym in table._interfaces)
            foreach (string inh in isym.InheritNames)
            {
                if (table.FindInterface(inh) is { } b)
                {
                    if (isym.Inherits.Contains(b))
                        edition.Error("COBOLNET0840",
                            $"interface '{isym.Name}': duplicate INHERITS FROM '{inh}' (ISO §11.6 SR6)");
                    else
                        isym.Inherits.Add(b);
                }
                else
                    edition.Error("COBOLNET0840",
                        $"interface '{isym.Name}': INHERITS FROM unknown interface '{inh}' (ISO §11.6 SR2)");
            }
        foreach (var isym in table._interfaces)
        {
            var seenI = new HashSet<OoInterfaceSymbol>();
            var stack = new Stack<OoInterfaceSymbol>([isym]);
            while (stack.Count > 0)
            {
                var cur = stack.Pop();
                foreach (var b in cur.Inherits)
                {
                    if (ReferenceEquals(b, isym))
                    {
                        edition.Error("COBOLNET0840",
                            $"interface '{isym.Name}': the INHERITS graph is cyclic (ISO §11.6 SR3)");
                        stack.Clear();
                        break;
                    }
                    if (seenI.Add(b)) stack.Push(b);
                }
            }
        }

        foreach (var ctx in classes)
        {
            var id = ctx.classIdParagraph();
            string name = id.className(0).GetText();
            ConstructRegistry.Check(edition.Edition, edition, Constructs.ClassDefinition2002, $"class definition '{name}' (CLASS-ID compilation unit)");   // COBOL-2002 introduction, bind-time gate (rearch migration Cluster 7)
            string csName = DataItem.Sanitize(name).ToUpperInvariant();   // MUST match PicInfo.ClrType's mapping
            var sym = new OoClassSymbol(name, csName, ctx)
            {
                BaseName = id.className().Length > 1 ? id.className(1).GetText() : null,
                IsFinal = id.FINAL() is not null,
            };
            usedCsNames.Add(csName + "__FACTORY");   // belt-and-braces (a `__` name cannot collide with COBOL-derived names)
            if (table._ifaceByName.ContainsKey(name))
                edition.Error("COBOLNET0840",
                    $"'{name}' is defined as both a class and an interface — one name namespace "
                    + "(ISO §8.3.2.2)");
            if (!table._byName.TryAdd(name, sym) || !usedCsNames.Add(csName))
            {
                edition.Error("COBOLNET0820",
                    $"duplicate class definition '{name}' — a class-name shall be unique within the compilation "
                    + "group (ISO §8.3.2.2/§11.3; class names compare case-insensitively and map to one emitted "
                    + "type)");
                continue;
            }
            table._classes.Add(sym);

            if (ctx.endClassHeader().className().GetText() is { } endName
                && !string.Equals(endName, name, StringComparison.OrdinalIgnoreCase))
                edition.Error("COBOLNET0820",
                    $"END CLASS '{endName}' does not match CLASS-ID '{name}' (ISO §10.7 — the end marker names "
                    + "its class)");

            foreach (var m in ctx.objectParagraph()?.methodDefinition() ?? [])
            {
                var sel = m.methodPropertySelector();
                string methodName = sel is not null
                    ? (sel.GET() is not null ? "__GET_" : "__SET_")
                      + DataItem.Sanitize(sel.propertyName().GetText()).ToUpperInvariant()
                    : m.methodName(0).GetText();
                var pd = m.procedureDivision();
                string mcs = sel is not null ? methodName : DataItem.Sanitize(methodName).ToUpperInvariant();
                // CS0542 guard: a C# member may not be named like its enclosing type — a METHOD-ID named like
                // its CLASS-ID is legal COBOL (§8.4.5 distinct name categories), so the SYMBOL renames.
                if (mcs == csName) mcs += "_M";
                var method = new OoMethodSymbol(
                    methodName,
                    HasUsing: pd?.usingClause() is not null,
                    HasReturning: pd?.returningClause() is not null,
                    m)
                { CsName = mcs, Owner = sym, HasOverride = m.OVERRIDE() is not null, IsFinal = m.FINAL() is not null,
                  Accessor = sel is null ? '\0' : sel.GET() is not null ? 'G' : 'S',
                  PropertyName = sel?.propertyName().GetText() };
                // §11.7 SR6/SR7 — the accessor SHAPES: GET = no USING + exactly one RETURNING; SET = exactly
                // one USING + no RETURNING (checked here on header presence; formal counts re-checked at
                // data-bind when they resolve).
                if (method.Accessor == 'G' && (method.HasUsing || !method.HasReturning))
                    edition.Error("COBOLNET0842",
                        $"class '{name}': METHOD-ID GET PROPERTY {method.PropertyName} shall have no USING "
                        + "and exactly one RETURNING (ISO §11.7 SR6)");
                if (method.Accessor == 'S' && (!method.HasUsing || method.HasReturning))
                    edition.Error("COBOLNET0842",
                        $"class '{name}': METHOD-ID SET PROPERTY {method.PropertyName} shall have exactly "
                        + "one USING and no RETURNING (ISO §11.7 SR7)");
                if (!sym.TryAddMethod(method))
                    edition.Error("COBOLNET0822",
                        $"class '{name}': duplicate method name '{methodName}' — method names shall be unique "
                        + "within a class (v1 restriction, OO deep-dive D9: overloading by method resolution "
                        + "signature, ISO §12063, is an OPTIONAL feature and is deferred)");
                if (sel is null && m.methodName().Length > 1
                    && !string.Equals(m.methodName(1).GetText(), methodName, StringComparison.OrdinalIgnoreCase))
                    edition.Error("COBOLNET0820",
                        $"class '{name}': END METHOD '{m.methodName(1).GetText()}' does not match METHOD-ID "
                        + $"'{methodName}' (ISO §10.7)");
            }

            // FACTORY methods (§11.4) — a SEPARATE roster/interface (§9.3.6: an instance method and a
            // factory method may share a name). A factory METHOD-ID named NEW is COBOLNET0836: the
            // predefined New (§16.2.1) is the generated ctor (D4) and overriding it is a v1 restriction.
            foreach (var m in ctx.factoryParagraph()?.methodDefinition() ?? [])
            {
                var fsel = m.methodPropertySelector();
                string methodName = fsel is not null
                    ? (fsel.GET() is not null ? "__GET_" : "__SET_")
                      + DataItem.Sanitize(fsel.propertyName().GetText()).ToUpperInvariant()
                    : m.methodName(0).GetText();
                var pd = m.procedureDivision();
                if (string.Equals(methodName, "NEW", StringComparison.OrdinalIgnoreCase))
                {
                    edition.Error("COBOLNET0836",
                        $"class '{name}': a factory method may not be named 'NEW' — the predefined New "
                        + "(ISO §16.2.1) is realized by the generated constructor (deep-dive D4); overriding "
                        + "New is a deferred v1 restriction");
                    continue;
                }
                string fcs = fsel is not null ? methodName : DataItem.Sanitize(methodName).ToUpperInvariant();
                if (fcs == csName + "__FACTORY") fcs += "_M";   // unreachable (no __ in COBOL names) — defensive
                var method = new OoMethodSymbol(
                    methodName,
                    HasUsing: pd?.usingClause() is not null,
                    HasReturning: pd?.returningClause() is not null,
                    m)
                { CsName = fcs, Owner = sym, IsFactory = true,
                  HasOverride = m.OVERRIDE() is not null, IsFinal = m.FINAL() is not null,
                  Accessor = fsel is null ? '\0' : fsel.GET() is not null ? 'G' : 'S',
                  PropertyName = fsel?.propertyName().GetText() };
                if (!sym.TryAddFactoryMethod(method))
                    edition.Error("COBOLNET0822",
                        $"class '{name}': duplicate factory method name '{methodName}' — method names shall "
                        + "be unique within the factory definition (v1 restriction, deep-dive D9)");
                if (fsel is null && m.methodName().Length > 1
                    && !string.Equals(m.methodName(1).GetText(), methodName, StringComparison.OrdinalIgnoreCase))
                    edition.Error("COBOLNET0820",
                        $"class '{name}': END METHOD '{m.methodName(1).GetText()}' does not match METHOD-ID "
                        + $"'{methodName}' (ISO §10.7)");
            }
        }

        // Resolve base links AFTER all classes are registered (a base may be defined later in the file).
        foreach (var sym in table._classes)
        {
            if (sym.BaseName is not { } baseName) continue;
            if (table.Find(baseName) is { } baseSym)
            {
                sym.Base = baseSym;
                if (baseSym.IsFinal)
                    edition.Error("COBOLNET0839",
                        $"class '{sym.Name}': INHERITS FROM '{baseName}', which is declared FINAL — a FINAL "
                        + "class shall not be a superclass (ISO §11.3 SR5/GR3)");
            }
            else
                edition.Error("COBOLNET0821",
                    $"class '{sym.Name}': INHERITS FROM unknown class '{baseName}' — object-class-name-2 shall "
                    + "reference a class defined in the compilation group (ISO §11.3.2; never degraded to a "
                    + "root class)");
        }

        // An inheritance CYCLE would emit circular C# base declarations — a Roslyn CS error on user source
        // (the loud-failure violation). Reject at pass-1 and CUT the link so downstream chain walks stay finite.
        foreach (var sym in table._classes)
        {
            var seen = new HashSet<OoClassSymbol> { sym };
            for (OoClassSymbol? b = sym.Base; b is not null; b = b.Base)
                if (!seen.Add(b))
                {
                    edition.Error("COBOLNET0820",
                        $"class '{sym.Name}': the INHERITS chain is cyclic through '{b.Name}' (ISO §11.3.2 — "
                        + "a class shall not inherit from itself directly or indirectly)");
                    sym.Base = null;
                    break;
                }
        }

        // OVERRIDE marking + the §11.7 SR3/SR4a/GR3 attribute rules (the OVERRIDE/FINAL wave, DEVLOG 605 —
        // the former by-name-inference leniency is RETIRED as the default): an EXPLICIT OVERRIDE marks the
        // override (0839 when the overridden method is FINAL — GR3); a name match WITHOUT the attribute is
        // the SR4a 0837 via EditionContext.Removed (error strict; warning + the pre-wave inference under
        // --permissive — the documented migration leniency), and the override is STILL marked so 0829
        // signature messages stay coherent; OVERRIDE with NO matching base method is the SR3 0838. Both
        // rosters (instance + factory) take the identical rules — per-interface, never cross-roster (D11).
        // The uppercase CsName convention still neutralizes trap #2; the override adopts the base slot's
        // CsName (C# requires the exact member name; the class-name collision corner stays 0820).
        foreach (var sym in table._classes)
        {
            MarkRoster(sym, sym.Methods, sym.Base is null ? null : (n => sym.Base!.FindMethod(n)), "");
            MarkRoster(sym, sym.FactoryMethods, sym.Base is null ? null : (n => sym.Base!.FindFactoryMethod(n)), "factory ");
        }

        // IMPLEMENTS capture (§11.8.2 — the OBJECT/FACTORY paragraph headers; interface names resolve
        // against the group's interface table; §11.8.3 SR1's REPOSITORY requirement is enforced with the
        // repository binding — staged as a documented follow-up, the conformance pass is the substance).
        foreach (var sym in table._classes)
        {
            CaptureImplements(sym.Ctx.objectParagraph()?.implementsClause(), sym.Implements, "OBJECT");
            CaptureImplements(sym.Ctx.factoryParagraph()?.implementsClause(), sym.FactoryImplements, "FACTORY");

            void CaptureImplements(Core.ImplementsClauseContext? impl, List<OoInterfaceSymbol> into, string where)
            {
                foreach (var iref in impl?.interfaceName() ?? [])
                {
                    if (table.FindInterface(iref.GetText()) is { } isym)
                    {
                        if (into.Contains(isym))
                            edition.Error("COBOLNET0840",
                                $"class '{sym.Name}' ({where}): duplicate IMPLEMENTS '{iref.GetText()}' "
                                + "(ISO §11.8 SR)");
                        else
                            into.Add(isym);
                    }
                    else
                        edition.Error("COBOLNET0840",
                            $"class '{sym.Name}' ({where}): IMPLEMENTS unknown interface '{iref.GetText()}' "
                            + "(ISO §11.8.3 — interface-name shall reference an interface of the group)");
                }
            }
        }
        return table;

        void MarkRoster(OoClassSymbol sym, IReadOnlyList<OoMethodSymbol> roster,
            Func<string, OoMethodSymbol?>? findInBase, string kind)
        {
            foreach (var m in roster)
            {
                var baseM = findInBase?.Invoke(m.Name);
                if (baseM is null)
                {
                    if (m.HasOverride)
                        edition.Error("COBOLNET0838",
                            $"class '{sym.Name}': {kind}method '{m.Name}' specifies OVERRIDE but no "
                            + "superclass defines a method with that name"
                            + (sym.Base is null ? " (the class has no INHERITS clause)" : "")
                            + " (ISO §11.7 SR3)");
                    continue;
                }
                if (!m.HasOverride)
                    edition.Removed("COBOLNET0837",
                        $"class '{sym.Name}': {kind}method '{m.Name}' redefines a method inherited from "
                        + $"'{baseM.Owner.Name}' without the OVERRIDE attribute (ISO §11.7 SR4a — an "
                        + "inherited method may be redefined only with OVERRIDE; add OVERRIDE to the "
                        + "METHOD-ID paragraph)");
                if (baseM.IsFinal)
                    edition.Error("COBOLNET0839",
                        $"class '{sym.Name}': {kind}method '{m.Name}' overrides '{baseM.Owner.Name}'."
                        + $"'{baseM.Name}', which is declared FINAL — a FINAL method shall not be "
                        + "overridden (ISO §11.7 SR3/GR3)");
                m.OverrideOf = baseM;
                m.CsName = baseM.CsName;
                if (m.CsName == sym.CsName)
                    edition.Error("COBOLNET0820",
                        $"class '{sym.Name}': the inherited method '{m.Name}' collides with the class's "
                        + "own emitted type name (implementation restriction — rename the class or the "
                        + "method; §8.3.2.2 externalized-name mapping)");
            }
        }
    }
}

/// <summary>One class of the pass-1 table: identity, base link, and the method roster (name → symbol,
/// case-insensitive per §8.3.2.2). <see cref="CsName"/> is the emitted C# type name — the SAME mapping
/// <c>PicInfo.ClrType</c> applies to a typed object reference's declared class (Sanitize + uppercase).</summary>
public sealed class OoClassSymbol(string name, string csName, CobolParserCore.ClassDefinitionContext ctx)
{
    public string Name { get; } = name;
    public string CsName { get; } = csName;
    public CobolParserCore.ClassDefinitionContext Ctx { get; } = ctx;
    public string? BaseName { get; init; }
    public OoClassSymbol? Base { get; set; }

    /// <summary>CLASS-ID … IS FINAL (§11.3 — GR3: a FINAL class shall not be a superclass; 0839). Emits
    /// C# <c>sealed</c>; every fresh method slot in it emits NON-virtual (the CS0549 trap — a virtual
    /// member inside a sealed class is a Roslyn error on emitted code).</summary>
    public bool IsFinal { get; init; }

    /// <summary>The OBJECT paragraph's IMPLEMENTS interfaces (§11.8.2), resolved at pass-1; the CONFORMANCE
    /// closure (§11.8.4 GR2: direct + interface-INHERITed + class-INHERITed) is computed by
    /// <c>ValidateImplements</c> — the C# base-list emission carries only the DIRECT list (the closure
    /// arrives transitively at the C# level).</summary>
    public List<OoInterfaceSymbol> Implements { get; } = [];

    /// <summary>The FACTORY paragraph's IMPLEMENTS interfaces (§11.8.2 :12755). The factory SINGLETON class
    /// (brief D11 — real virtual members, post-FACTORY-wave) emits and satisfies them exactly like the
    /// instance side; the original brief's validate-only posture is SUPERSEDED by D11.</summary>
    public List<OoInterfaceSymbol> FactoryImplements { get; } = [];


    private readonly Dictionary<string, OoMethodSymbol> _methods = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The INSTANCE methods in declaration order (the emitter's roster).</summary>
    public IReadOnlyList<OoMethodSymbol> Methods => _methodList;
    private readonly List<OoMethodSymbol> _methodList = [];

    /// <summary>The emitted C# type of this class's FACTORY OBJECT (brief D11 — a REAL sibling singleton
    /// class, `FOO__FACTORY : BASE__FACTORY | CobolObject`, NEVER statics: §8.6.4 gives every class its OWN
    /// copy of inherited factory data; SELF in a factory method is polymorphic §14.9.23.3 SR4f + §8.4.3.8
    /// GR2; factory resolution walks INHERITS §9.3.6. `__` cannot appear in a COBOL-derived name — no
    /// collision).</summary>
    public string FactoryCsName => CsName + "__FACTORY";

    private readonly Dictionary<string, OoMethodSymbol> _factoryMethods = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The FACTORY methods in declaration order (§11.4 — a SEPARATE interface from the instance
    /// roster; the two may share names, §9.3.6).</summary>
    public IReadOnlyList<OoMethodSymbol> FactoryMethods => _factoryMethodList;
    private readonly List<OoMethodSymbol> _factoryMethodList = [];

    internal bool TryAddMethod(OoMethodSymbol m)
    {
        if (!_methods.TryAdd(m.Name, m)) return false;
        _methodList.Add(m);
        return true;
    }

    internal bool TryAddFactoryMethod(OoMethodSymbol m)
    {
        if (!_factoryMethods.TryAdd(m.Name, m)) return false;
        _factoryMethodList.Add(m);
        return true;
    }

    /// <summary>Resolve a FACTORY method by name over THIS class and its base chain (§9.3.6 — factory
    /// resolution walks INHERITS exactly like instance resolution, over the factory interface).</summary>
    public OoMethodSymbol? FindFactoryMethod(string name)
    {
        var seen = new HashSet<OoClassSymbol>();
        for (OoClassSymbol? c = this; c is not null && seen.Add(c); c = c.Base)
            if (c._factoryMethods.TryGetValue(name, out var m))
                return m;
        return null;
    }

    /// <summary>Resolve a method by name over THIS class and its base chain (§9.3.6 method resolution; the
    /// walk is cycle-safe — pass-1 rejects unknown bases, and a cycle cannot outlast the seen-set).</summary>
    public OoMethodSymbol? FindMethod(string name)
    {
        var seen = new HashSet<OoClassSymbol>();
        for (OoClassSymbol? c = this; c is not null && seen.Add(c); c = c.Base)
            if (c._methods.TryGetValue(name, out var m))
                return m;
        return null;
    }

    /// <summary>True when <paramref name="other"/> is this class or one of its superclasses — the object-view /
    /// RETURNING-receiver conformance direction (§14.8: a subclass object conforms to its superclass).</summary>
    public bool ConformsTo(OoClassSymbol other)
    {
        var seen = new HashSet<OoClassSymbol>();
        for (OoClassSymbol? c = this; c is not null && seen.Add(c); c = c.Base)
            if (ReferenceEquals(c, other))
                return true;
        return false;
    }
}

/// <summary>One method of a class roster (ISO §11.7): the COBOL name, the emitted C# method name, the header
/// USING/RETURNING presence recorded at pass-1, and — filled by <c>DataBinder.OoBindMethodData</c> at class
/// bind time (BEFORE any statement binds, so every INVOKE site in the group sees the full signature) — the
/// resolved formal list, RETURNING item, and data roots (port slice 2, deep-dive D3/D6).</summary>
public sealed record OoMethodSymbol(
    string Name, bool HasUsing, bool HasReturning,
    CobolParserCore.MethodDefinitionContext Ctx)
{
    /// <summary>The emitted C# method name. Starts as the sanitized-uppercase COBOL name; an OVERRIDE adopts
    /// its base slot's name verbatim (C# requires the override member name to match), and a collision with
    /// the owning class's type name (C# CS0542) or an emitted field takes a deterministic suffix — always a
    /// SYMBOL-level rename so every INVOKE site in the group follows automatically (§8.3.2.2: the
    /// user-word→externalized-name mapping is implementor-defined).</summary>
    public required string CsName { get; set; }

    /// <summary>The class that declares this method (set at pass-1) — the marshaling qualifier for the
    /// formal's class-level statics (numeric profiles) at CONTENT-conversion call sites.</summary>
    public OoClassSymbol Owner { get; set; } = null!;

    /// <summary>True for a FACTORY method (§11.4) — the SELF/SUPER roster selector and diagnostic wording;
    /// its formals' profiles/statics live on the FACTORY class, so CONTENT-conversion call sites qualify by
    /// <see cref="OoClassSymbol.FactoryCsName"/>.</summary>
    public bool IsFactory { get; init; }

    /// <summary>METHOD-ID … OVERRIDE (§11.7 SR3/SR4a — the OVERRIDE/FINAL wave, DEVLOG 605): an explicit
    /// override declaration. Redefinition WITHOUT it is the SR4a 0837 (error strict; warning + the pre-wave
    /// name-match inference under <c>--permissive</c> — the documented migration leniency).</summary>
    public bool HasOverride { get; init; }

    /// <summary>METHOD-ID … [IS] FINAL (§11.7 GR3 — shall not be overridden; 0839 on the attempt). Emits
    /// C# <c>sealed override</c> (or a non-virtual fresh slot at a root).</summary>
    public bool IsFinal { get; init; }

    /// <summary>The property-accessor identity (§11.7/§13.18.42 — the PROPERTY wave): 'G'/'S' for a GET/SET
    /// accessor of <see cref="PropertyName"/> (explicit <c>METHOD-ID. GET|SET PROPERTY p</c> or synthesized
    /// from a PROPERTY clause), '\0' for an ordinary named method. Accessor rosters key by the PINNED
    /// implementor-defined names <c>__GET_&lt;P&gt;</c>/<c>__SET_&lt;P&gt;</c> (§11.7.4 GR1a — the `__`
    /// cannot-collide rule), so override/0829/implements machinery applies to accessors unchanged.</summary>
    public char Accessor { get; init; }
    public string? PropertyName { get; init; }

    /// <summary>The method PD-header RAISING partition (§14.2.2; the EC-OO wave D-EO8): level-3 EC-USER
    /// names + classes of the group — loaded into the statement binder's per-source-element sets before
    /// this method's body binds (a method IS a source element, §14.9.18.3 SR2/SR4a).</summary>
    public List<string> RaisingEcNames { get; } = [];
    public List<string> RaisingClasses { get; } = [];

    /// <summary>For a PROPERTY-clause-synthesized accessor: the SUBJECT data item (the emitter renders a
    /// direct field read/write — observably identical to the spec's implicit MOVE method, §13.18.42 GR1/GR2
    /// :21214-21229, because the cloned descriptions are identical by construction). Null for explicit
    /// GET/SET PROPERTY methods (they carry real bodies).</summary>
    public DataItem? PropertySubject { get; set; }

    /// <summary>The method's contiguous pc range in its class's one dispatch space — assigned by
    /// <c>StatementBinder.BindClassBody</c> (the exit-bounded range IS the fall-through guard: running past
    /// the last paragraph returns from the method, never into a sibling's paragraphs — the legacy trap #4).</summary>
    public int EntryPc { get; set; } = -1;
    public int EndPc { get; set; } = -2;   // Entry > End ⇔ an empty method body

    /// <summary>The ordered PD USING formals (§14.9.23.4 GR3 — positional correspondence; every formal is
    /// BY REFERENCE, the header BY VALUE phrase being an unparsed grammar extension).</summary>
    public List<OoFormal> Formals { get; } = [];

    /// <summary>The PD RETURNING item (a LINKAGE 01/77 — §14.2.3 GR6: callee-allocated; the method's C# return
    /// value delivers it, §14.9.23.4 GR8), or null for a void method.</summary>
    public DataItem? Returning { get; set; }

    /// <summary>The method's LINKAGE roots (ALL of them — formals, the RETURNING item, and any unattached
    /// entry): each becomes a capturable C# LOCAL of the emitted method.</summary>
    public List<DataItem> LinkageRoots { get; } = [];

    /// <summary>LOCAL-STORAGE roots → C# locals, re-initialized on every activation (§14.5.3).</summary>
    public List<DataItem> LocalRoots { get; } = [];

    /// <summary>Method WORKING-STORAGE roots → STATIC fields (D3 — shared across instances, persistent across
    /// activations, §11.7; ILLEGAL at 2023, §13.5.3 SR 1 — the EditionValidator window row).</summary>
    public List<DataItem> StaticRoots { get; } = [];

    /// <summary>The method's own name scope (§11.7 GR5 shadowing; sibling invisibility — trap #6).</summary>
    public OoMethodDataScope DataScope { get; } = new();

    /// <summary>The base-chain method this method OVERRIDES (slice 3a — §9.3.6 dispatch is on the runtime
    /// class; D7: emitted as C# <c>override</c>), or null for a fresh <c>virtual</c> slot. Marked at pass-1
    /// by name (the OVERRIDE attribute is not in the grammar yet — the documented SR4a leniency); the
    /// SIGNATURE conformance (§9.3.8.2) validates after all class data binds
    /// (<see cref="OoClassTable.ValidateOverrideSignatures"/>).</summary>
    public OoMethodSymbol? OverrideOf { get; set; }
}

/// <summary>One resolved USING formal: the LINKAGE item (its <see cref="DataItem.CsName"/> is the capturable
/// LOCAL the body addresses), the 0-based positional slot, and the emitted C# parameter name.</summary>
public sealed record OoFormal(DataItem Item, int Position, string ParamName);

/// <summary>One INTERFACE-ID of the compilation group (§11.6 — pass-1): the PROTOTYPE roster (reusing
/// <see cref="OoMethodSymbol"/> — headers + LINKAGE formals, no bodies), the INHERITS list (repetition
/// SUPPORTED — C# interface lists are native; the deliberate asymmetry with the class-side single-base
/// restriction, SSOT §18.18), and the emitted C# interface name.</summary>
public sealed class OoInterfaceSymbol(string name, string csName, CobolParserCore.InterfaceDefinitionContext ctx)
{
    public string Name { get; } = name;
    public string CsName { get; } = csName;
    public CobolParserCore.InterfaceDefinitionContext Ctx { get; } = ctx;
    public List<string> InheritNames { get; } = [];
    public List<OoInterfaceSymbol> Inherits { get; } = [];

    private readonly Dictionary<string, OoMethodSymbol> _protos = new(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyList<OoMethodSymbol> Prototypes => _protoList;
    private readonly List<OoMethodSymbol> _protoList = [];

    internal bool TryAddPrototype(OoMethodSymbol m)
    {
        if (!_protos.TryAdd(m.Name, m)) return false;
        _protoList.Add(m);
        return true;
    }

    /// <summary>The interface's FULL method surface: own prototypes + the INHERITS closure (§9.3.8.2.2),
    /// first declaration wins per name (SR5 mutual-conformance across multi-inherit is validated at Build).</summary>
    public IEnumerable<OoMethodSymbol> AllPrototypes()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var stack = new Stack<OoInterfaceSymbol>();
        var visited = new HashSet<OoInterfaceSymbol>();
        stack.Push(this);
        while (stack.Count > 0)
        {
            var i = stack.Pop();
            if (!visited.Add(i)) continue;
            foreach (var m in i._protoList)
                if (seen.Add(m.Name))
                    yield return m;
            foreach (var b in i.Inherits) stack.Push(b);
        }
    }
}
