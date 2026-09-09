// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using CobolNet.Binding;
using CobolNet.Frontend.Generated;

namespace CobolNet.Compiler.Oo;

using Core = CobolParserCore;

/// <summary>
/// ⛔ THE ONE FUNNEL FOR RESOLVING A WRITTEN object-class-name OR interface-name (kb/Work PB365).
///
/// <para><b>The rule it owns is ISO §8.4.6.4, Scope of object-class-names and interface-names</b>: "The
/// object-class-name of an object class referenced within a source element shall be either the name of the
/// containing object class definition or declared in the REPOSITORY paragraph of that or a containing source
/// element", and the same sentence for interface-names. Every per-statement rule that says "shall be the name
/// of a class specified in the REPOSITORY paragraph" — §11.3.3 SR2 (CLASS-ID INHERITS), §11.4.3 SR1 (FACTORY
/// IMPLEMENTS), §11.6.3 SR2 (INTERFACE-ID INHERITS), §11.8.3 SR1 (OBJECT IMPLEMENTS), §14.2.2 SR8/SR9 (the
/// PROCEDURE DIVISION header's RAISING phrase), §14.9.49.3 SR16/SR17 (USE Format 4) — is an INSTANCE of it, so
/// they are one mechanism and not seven.</para>
///
/// <para><b>What it replaced.</b> Every one of those sites called <c>OoClassTable.Find</c> /
/// <c>FindInterface</c> directly, and <see cref="OoClassTable"/> is built ONCE PER COMPILATION GROUP from every
/// CLASS-ID / INTERFACE-ID definition in the file. So the compiler answered a question about the GROUP where
/// the standard asks about the REFERRING SOURCE ELEMENT: a program with no REPOSITORY paragraph at all could
/// write <c>USE AFTER EXCEPTION OBJECT CF4A</c> and compile clean, and COBOLNET0859 said so out loud — "does
/// not name a class of the compilation group". Widening a set is silent by construction: nothing downstream can
/// notice a reference that should not have resolved.</para>
///
/// <para><b>The scope is computed from the parse tree, not plumbed.</b> §12.3.3 SR1 forbids a configuration
/// section in a contained program, SR2 in a method definition, and SR3 the REPOSITORY paragraph in a factory or
/// instance definition — so the source elements that CAN carry a REPOSITORY are the outermost program, the
/// function definition, the class definition and the interface definition, and §12.3.4 GR1 spreads each one's
/// entries to "each directly or indirectly contained source unit". An ancestor walk from the reference site
/// therefore IS the rule, with no state to thread and nothing to keep in sync; a new reference site gets the
/// right scope by construction (CLAUDE.md rule 5 — the shape that makes the next case automatic).</para>
/// </summary>
public static class OoNameResolution
{
    /// <summary>Which alternatives the referencing construct admits. <c>Either</c> is the USE Format-4 /
    /// USAGE OBJECT REFERENCE shape (a brace group of object-class-name-1 | interface-name-1).</summary>
    public enum Want { Class, Interface, Either }

    /// <summary>A resolution outcome. <see cref="Class"/> and <see cref="Interface"/> are mutually exclusive:
    /// classes and interfaces share ONE name namespace (<see cref="OoClassTable"/> rejects a collision as
    /// COBOLNET0840), so at most one can be set.</summary>
    public readonly record struct Result(OoClassSymbol? Class, OoInterfaceSymbol? Interface)
    {
        public bool Ok => Class is not null || Interface is not null;
    }

    /// <summary>Resolve <paramref name="name"/> as an object-class-name / interface-name visible from
    /// <paramref name="site"/>, reporting through <paramref name="descriptor"/> when it is not. The message
    /// distinguishes the two failures, because their fixes differ: a name that is DEFINED in the group but not
    /// declared in scope needs a REPOSITORY entry (§8.4.6.4); a name that is defined nowhere needs a
    /// definition.</summary>
    /// <param name="where">The reference site, e.g. <c>"declarative section 'EO-SEC': USE AFTER EXCEPTION OBJECT"</c>.</param>
    /// <param name="code">The REFERENCING construct's own diagnostic code — each site keeps the code its
    /// goldens pin; what the funnel owns is the SET and the message, not the code.</param>
    /// <param name="ruleCitation">The referencing construct's own syntax rule, e.g. <c>"ISO §14.9.49.3 SR16/SR17"</c>.</param>
    public static Result Resolve(OoClassTable? table, EditionContext edition, RuleContext? site, string name,
        Want want, string where, string code, string ruleCitation)
    {
        var found = Lookup(table, site, name, want);
        if (found.Ok) return found;

        // Defined in the group but out of scope vs. not defined at all — name the actual condition (the old
        // 0859 named neither: it said "does not name a class of the compilation group", which was BOTH the
        // wrong set and, for an interface operand, the wrong rule).
        bool definedInGroup = want switch
        {
            Want.Class => table?.Find(name) is not null,
            Want.Interface => table?.FindInterface(name) is not null,
            _ => table?.Find(name) is not null || table?.FindInterface(name) is not null,
        };
        string kind = want switch
        {
            Want.Class => "a class",
            Want.Interface => "an interface",
            _ => "a class or interface",
        };
        edition.Error(code, definedInGroup
            ? $"{where} '{name}': {kind} of that name is defined in this compilation group, but it is not the "
              + "name of the containing class or interface definition and is not declared in the REPOSITORY "
              + $"paragraph of this source element or a containing one ({ruleCitation}; ISO §8.4.6.4)"
            : $"{where} '{name}': no such {kind} is defined in this compilation group ({ruleCitation})");
        return default;
    }

    /// <summary>The NON-DIAGNOSING half, for the sites that PARTITION on "is this word a class-name here?" —
    /// the PROCEDURE DIVISION / METHOD-ID RAISING word that may instead be an exception-name (§14.2.2 SR7 vs
    /// SR8/SR9), and INVOKE's receiver, which may instead be an identifier. The partition uses the SAME scoped
    /// set: a class the source element cannot reference is not a class-name in that position.</summary>
    public static Result Lookup(OoClassTable? table, RuleContext? site, string name, Want want)
    {
        if (table is null) return default;
        var scope = table.RepositoryScopeFor(site);
        if (want is Want.Class or Want.Either
            && scope.DeclaresClass(name) && table.Find(name) is { } cls) return new Result(cls, null);
        if (want is Want.Interface or Want.Either
            && scope.DeclaresInterface(name) && table.FindInterface(name) is { } ifc) return new Result(null, ifc);
        return default;
    }
}

/// <summary>
/// The set of object-class-names and interface-names ONE source element may reference (ISO §8.4.6.4): its own
/// REPOSITORY declarations plus every containing source element's (§12.3.4 GR1), plus the name of the
/// containing class or interface definition itself — §12.3.8.2 GR5/GR8 say the self-naming specifier "is
/// ignored", so the definition's own name is in scope whether or not it is declared.
/// </summary>
public sealed class OoRepositoryScope
{
    /// <summary>The scope of a reference outside every source element that can declare one. It admits nothing:
    /// §8.4.6.4 offers no third source for a visible name.</summary>
    public static readonly OoRepositoryScope Empty = new([], []);

    private readonly HashSet<string> _classes;
    private readonly HashSet<string> _interfaces;

    private OoRepositoryScope(HashSet<string> classes, HashSet<string> interfaces)
    {
        _classes = classes;
        _interfaces = interfaces;
    }

    public bool DeclaresClass(string name) => _classes.Contains(name);
    public bool DeclaresInterface(string name) => _interfaces.Contains(name);

    /// <summary>Build the scope visible at <paramref name="site"/> by walking its parse-tree ancestors: every
    /// REPOSITORY paragraph on the way out contributes (the reference's own source element first, then each
    /// containing one — §12.3.4 GR1), and a class / interface definition contributes its own name.</summary>
    public static OoRepositoryScope Build(RuleContext? site)
    {
        var classes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var interfaces = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (RuleContext? c = site; c is not null; c = c.Parent)
        {
            switch (c)
            {
                case Core.ClassDefinitionContext cd:
                    // className()[0] is the class's OWN name; the rest are the INHERITS operands
                    // (§11.3.2's `[ INHERITS FROM { object-class-name-2 } … ]`), which are REFERENCES.
                    if (cd.classIdParagraph()?.className().FirstOrDefault() is { } cn) classes.Add(cn.GetText());
                    break;
                case Core.InterfaceDefinitionContext idf:
                    if (idf.interfaceName().FirstOrDefault() is { } inm) interfaces.Add(inm.GetText());
                    break;
            }
            // The environment division of THIS context, when it directly carries one. Only a source-element
            // context does; a statement or clause context has no such child, so the test is a cheap miss.
            for (int i = 0; i < c.ChildCount; i++)
                if (c.GetChild(i) is Core.EnvironmentDivisionContext env)
                    Collect(env, classes, interfaces);
        }
        return classes.Count == 0 && interfaces.Count == 0 ? Empty : new OoRepositoryScope(classes, interfaces);
    }

    private static void Collect(Core.EnvironmentDivisionContext env, HashSet<string> classes,
        HashSet<string> interfaces)
    {
        foreach (var re in env.configurationSection()?.configurationParagraph()
                     .Select(p => p.repositoryParagraph()).Where(r => r is not null)
                     .SelectMany(r => r!.repositoryEntry()) ?? [])
        {
            if (re.CLASS() is not null && re.className() is { } cn) classes.Add(cn.GetText());
            else if (re.INTERFACE() is not null && re.interfaceName() is { } inm) interfaces.Add(inm.GetText());
        }
    }
}
