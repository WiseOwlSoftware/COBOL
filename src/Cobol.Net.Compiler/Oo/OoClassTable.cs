// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Editions;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;

using CobolNet.Binding;
using CobolNet.Binding.Model;

namespace CobolNet.Compiler.Oo;

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
    /// <summary>The class named <paramref name="name"/>, or null (COBOL class names are case-insensitive).</summary>
    public OoClassSymbol? Find(string name) => _byName.TryGetValue(name, out var c) ? c : null;

    /// <summary>True when an item CROSSES the INVOKE boundary as a character string: groups (image crossing),
    /// image-stored numerics, alphanumeric / numeric-edited items. Native numerics and object references cross
    /// typed. The §14.8.2 strict-conformance bind rules guarantee both sides agree on the crossing form's
    /// WIDTH/description — which is what keeps the marshaling free of cross-class numeric profiles.
    /// (THE one definition — relocated from the emitter, P6 Step 5: the bind-phase harmonize below and the
    /// emitter's signature/marshaling renders both consult it.)</summary>
    public static bool StringCarried(DataItem item) =>
        item.IsGroup || item.StoreAsImage
        || item.Pic?.Category is PicCategory.Alphanumeric or PicCategory.NumericEdited
            or PicCategory.National or PicCategory.Boolean;   // string-stored (D-N1/D-B1) — char crossing

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

    /// <summary>
    /// Build the table from the group's class definitions (pass-1: identity + roster only — no data or statement
    /// binding). Structural diagnostics raised here, each per its ISO rule: duplicate class name / emitted-type
    /// collision (COBOLNET0820), END CLASS name mismatch (§10.7 — 0820), unknown INHERITS base (§11.3.2 —
    /// COBOLNET0821, never silently a root class), duplicate method name within a class (COBOLNET0822 — the
    /// unique-name restriction, deep-dive D9: parametric polymorphism, ISO §9.3.5.3, is the OPTIONAL Annex
    /// A.4.10 item 3 whose support this implementation does not claim).
    /// INHERITS emission itself is a later port slice (3a) — a KNOWN base still 0899s until it lands, but the
    /// base link is recorded so method lookup walks the chain from day one.
    /// <para>⚠ The three §12063 citations that stood here and at the two COBOLNET0822 emit sites were NOT
    /// clause numbers — ISO/IEC 1989:2023 has no §12063; the number was a stray line anchor, recorded as
    /// finding 44 of <c>docs/rearchitecture/DESIGN-SPEC-RECONCILIATION.md</c>. Re-derived and
    /// <c>cite.py --check</c>ed: §9.3.5.3 is "Parametric polymorphism", its rule 7 is "Parametric polymorphism
    /// is an optional feature in this Working Draft International Standard", and Annex A.4.10 item 3 is
    /// "Parametric polymorphism (9.3.5.3)". The CLAUDE.md rule-1 inherited-citation failure, caught at the
    /// point the rows it covers were being witnessed.</para>
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
            // COBOL-2002 introduction gate: VersionConformancePass ParseArm.VisitInterfaceDefinition (rearch 14g.3,
            // recognition — fires per parse node, so a duplicate/colliding definition dropped below still names its edition).
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
                    edition.Error(DiagnosticCatalog.OoInterfacePropertyPrototype,
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
            // COBOL-2002 introduction gate: VersionConformancePass ParseArm.VisitClassDefinition (rearch 14g.3,
            // recognition — fires per parse node, so a duplicate/colliding definition dropped below still names its edition).
            string csName = DataItem.Sanitize(name).ToUpperInvariant();   // MUST match PicInfo.ClrType's mapping
            var bases = id.className().Skip(1).Select(c => c.GetText()).ToList();
            var sym = new OoClassSymbol(name, csName, ctx)
            {
                Bases = bases,
                BaseName = bases.Count >= 1 ? bases[0] : null,
                IsFinal = id.FINAL() is not null,
            };
            if (bases.Count > 1)
                // ISO §11.3.2 permits several INHERITS bases; COBOL.NET v1 restricts to SINGLE inheritance and
                // rejects the rest LOUDLY (SSOT §18 #18; A.4.10 — multiple inheritance / parametric polymorphism
                // rejected). Silently compiling against only the first base was the R9 silent-miscompile.
                edition.Error("COBOLNET0849",
                    $"class '{name}': INHERITS FROM {bases.Count} base classes ({string.Join(", ", bases)}) — "
                    + "COBOL.NET v1 supports single inheritance only; multiple inheritance is rejected "
                    + "(ISO §11.3.2; SSOT §18 #18 / A.4.10)");
            usedCsNames.Add(csName + NamingConvention.FactorySuffix);   // belt-and-braces (a `__` name cannot collide with COBOL-derived names)
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
                    ? (sel.GET() is not null
                        ? NamingConvention.GetAccessorName(sel.propertyName().GetText())
                        : NamingConvention.SetAccessorName(sel.propertyName().GetText()))
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
                        + "within a class in this implementation (OO deep-dive D9). Overloading by method "
                        + "resolution signature is PARAMETRIC POLYMORPHISM (ISO §9.3.5.3), an OPTIONAL element "
                        + "(Annex A.4.10 item 3; §9.3.5.3 rule 7) whose support COBOL.NET does not claim");
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
                    ? (fsel.GET() is not null
                        ? NamingConvention.GetAccessorName(fsel.propertyName().GetText())
                        : NamingConvention.SetAccessorName(fsel.propertyName().GetText()))
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
                        + "be unique within the factory definition in this implementation (OO deep-dive D9). "
                        + "Overloading by method resolution signature is PARAMETRIC POLYMORPHISM (ISO "
                        + "§9.3.5.3), an OPTIONAL element (Annex A.4.10 item 3) whose support COBOL.NET does "
                        + "not claim — the factory arm carried NO citation at all before this");
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
