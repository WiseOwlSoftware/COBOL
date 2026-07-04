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
                var method = new OoMethodSymbol(
                    methodName,
                    DataItem.Sanitize(methodName).ToUpperInvariant(),
                    HasUsing: pd?.usingClause() is not null,
                    HasReturning: pd?.returningClause() is not null,
                    m);
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
        }

        // Resolve base links AFTER all classes are registered (a base may be defined later in the file).
        foreach (var sym in table._classes)
        {
            if (sym.BaseName is not { } baseName) continue;
            if (table.Find(baseName) is { } baseSym)
            {
                sym.Base = baseSym;
                // The INHERITS EMISSION (`: BASE`, override-under-exact-name, depth ordering) is port slice 3a —
                // recognized but staged loud until it lands; the resolved link above already lets method lookup
                // walk the chain (§9.3.6).
                edition.Error("COBOLNET0899",
                    $"class '{sym.Name}': INHERITS FROM '{baseName}' (ISO §11.3.2) is recognized but not yet "
                    + "implemented (owning roadmap phase: Phase 3, OO port slice 3a)");
            }
            else
                edition.Error("COBOLNET0821",
                    $"class '{sym.Name}': INHERITS FROM unknown class '{baseName}' — object-class-name-2 shall "
                    + "reference a class defined in the compilation group (ISO §11.3.2; never degraded to a "
                    + "root class)");
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

    /// <summary>The methods in declaration order (the emitter's roster).</summary>
    public IReadOnlyList<OoMethodSymbol> Methods => _methodList;
    private readonly List<OoMethodSymbol> _methodList = [];

    internal bool TryAddMethod(OoMethodSymbol m)
    {
        if (!_methods.TryAdd(m.Name, m)) return false;
        _methodList.Add(m);
        return true;
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

/// <summary>One method of a class roster (ISO §11.7): the COBOL name, the emitted C# method name, and whether
/// its procedure-division header declares USING / RETURNING formals (the part-2 spine records presence only;
/// the full param-mode/type list lands with port slice 2 — INVOKE USING/RETURNING marshaling).</summary>
public sealed record OoMethodSymbol(
    string Name, string CsName, bool HasUsing, bool HasReturning,
    CobolParserCore.MethodDefinitionContext Ctx)
{
    /// <summary>The method's contiguous pc range in its class's one dispatch space — assigned by
    /// <c>StatementBinder.BindClassBody</c> (the exit-bounded range IS the fall-through guard: running past
    /// the last paragraph returns from the method, never into a sibling's paragraphs — the legacy trap #4).</summary>
    public int EntryPc { get; set; } = -1;
    public int EndPc { get; set; } = -2;   // Entry > End ⇔ an empty method body
}
