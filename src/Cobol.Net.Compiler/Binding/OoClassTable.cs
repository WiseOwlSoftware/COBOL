// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Compiler.Generated;

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

    /// <summary>The ONE strict IDENTICAL-DESCRIPTION check — §14.8.2.3.2 (BY REFERENCE parameters ONLY; BY
    /// CONTENT follows §14.8.2.3.3 COMPUTE/MOVE/SET rules in the binder mode dispatch) and §9.3.8.2
    /// override-signature validation. Identical = same category; numeric: same USAGE + SIGN representation +
    /// BLANK WHEN ZERO + digits + scale + sign; alphanumeric: same length + JUSTIFIED; object reference: same
    /// declared class; group: image crossing with equal character length (except the §14.8.2.2 rule-1 BY
    /// REFERENCE prefix case — <paramref name="byRefGroupPrefix"/> allows a SMALLER formal). Null when
    /// conformant. This strictness keeps BY REFERENCE marshaling TYPE-PRESERVING (the slice-2 design fact);
    /// CONTENT conversions qualify the owner class internal profiles instead.</summary>
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
    public static OoClassTable Build(IReadOnlyList<Core.ClassDefinitionContext> classes, EditionContext edition)
    {
        var table = new OoClassTable();
        var usedCsNames = new HashSet<string>(StringComparer.Ordinal);

        foreach (var ctx in classes)
        {
            var id = ctx.classIdParagraph();
            string name = id.className(0).GetText();
            string csName = DataItem.Sanitize(name).ToUpperInvariant();   // MUST match PicInfo.ClrType's mapping
            var sym = new OoClassSymbol(name, csName, ctx)
            {
                BaseName = id.className().Length > 1 ? id.className(1).GetText() : null,
            };
            usedCsNames.Add(csName + "__FACTORY");   // belt-and-braces (a `__` name cannot collide with COBOL-derived names)
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
                string methodName = m.methodName(0).GetText();
                var pd = m.procedureDivision();
                string mcs = DataItem.Sanitize(methodName).ToUpperInvariant();
                // CS0542 guard: a C# member may not be named like its enclosing type — a METHOD-ID named like
                // its CLASS-ID is legal COBOL (§8.4.5 distinct name categories), so the SYMBOL renames.
                if (mcs == csName) mcs += "_M";
                var method = new OoMethodSymbol(
                    methodName,
                    HasUsing: pd?.usingClause() is not null,
                    HasReturning: pd?.returningClause() is not null,
                    m)
                { CsName = mcs, Owner = sym };
                if (!sym.TryAddMethod(method))
                    edition.Error("COBOLNET0822",
                        $"class '{name}': duplicate method name '{methodName}' — method names shall be unique "
                        + "within a class (v1 restriction, OO deep-dive D9: overloading by method resolution "
                        + "signature, ISO §12063, is an OPTIONAL feature and is deferred)");
                if (m.methodName().Length > 1
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
                string methodName = m.methodName(0).GetText();
                var pd = m.procedureDivision();
                if (string.Equals(methodName, "NEW", StringComparison.OrdinalIgnoreCase))
                {
                    edition.Error("COBOLNET0836",
                        $"class '{name}': a factory method may not be named 'NEW' — the predefined New "
                        + "(ISO §16.2.1) is realized by the generated constructor (deep-dive D4); overriding "
                        + "New is a deferred v1 restriction");
                    continue;
                }
                string fcs = DataItem.Sanitize(methodName).ToUpperInvariant();
                if (fcs == csName + "__FACTORY") fcs += "_M";   // unreachable (no __ in COBOL names) — defensive
                var method = new OoMethodSymbol(
                    methodName,
                    HasUsing: pd?.usingClause() is not null,
                    HasReturning: pd?.returningClause() is not null,
                    m)
                { CsName = fcs, Owner = sym, IsFactory = true };
                if (!sym.TryAddFactoryMethod(method))
                    edition.Error("COBOLNET0822",
                        $"class '{name}': duplicate factory method name '{methodName}' — method names shall "
                        + "be unique within the factory definition (v1 restriction, deep-dive D9)");
                if (m.methodName().Length > 1
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
                sym.Base = baseSym;
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

        // OVERRIDE detection (slice 3a — §9.3.6 runtime-class dispatch / D7): a subclass method whose name
        // matches a base-chain method OVERRIDES it (the corpus and the legacy write overrides WITHOUT the
        // OVERRIDE attribute — the grammar does not carry §11.7's [OVERRIDE]/[IS FINAL] attributes yet, so
        // §11.7 SR4a's redefinition-without-OVERRIDE error is a DOCUMENTED leniency until they land; the
        // uppercase CsName convention already neutralizes the legacy case-mismatch trap #2 — both spellings
        // map to ONE emitted name). The override's SIGNATURE must conform (§9.3.8.2: corresponding formals
        // and returning items with identical descriptions) — mismatch is COBOLNET0829, never a Roslyn CS
        // error on user source.
        foreach (var sym in table._classes)
        {
            if (sym.Base is null) continue;
            foreach (var m in sym.Methods)
                if (sym.Base.FindMethod(m.Name) is { } baseM)
                {
                    m.OverrideOf = baseM;
                    // C# requires the override member to reuse the base slot's exact name (which may carry a
                    // collision suffix). The one unrepresentable corner: the base slot's name equals THIS
                    // class's type name — reject loud, never a raw Roslyn CS0542 on user source (G4 rule).
                    m.CsName = baseM.CsName;
                    if (m.CsName == sym.CsName)
                        edition.Error("COBOLNET0820",
                            $"class '{sym.Name}': the inherited method '{m.Name}' collides with the class's "
                            + "own emitted type name (implementation restriction — rename the class or the "
                            + "method; §8.3.2.2 externalized-name mapping)");
                }
        }

        // FACTORY overrides mark against the base chain's FACTORY roster ONLY (§9.3.8.2 conformance is
        // per-interface — never cross-roster; the factory C# hierarchy mirrors the class hierarchy, D11).
        foreach (var sym in table._classes)
        {
            if (sym.Base is null) continue;
            foreach (var m in sym.FactoryMethods)
                if (sym.Base.FindFactoryMethod(m.Name) is { } baseM)
                {
                    m.OverrideOf = baseM;
                    m.CsName = baseM.CsName;
                }
        }
        return table;
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
