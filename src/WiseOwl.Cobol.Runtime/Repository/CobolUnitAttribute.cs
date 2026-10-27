// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Runtime.Repository;

/// <summary>
/// The identity of one published unit (docs/rearchitecture/DESIGN-external-repository.md §5.3, §6.2; kb/Work PB2100):
/// its kind, its user-defined word and its externalized name (ISO §8.3.2.2, §8.13), carried by the unit's host-entry
/// class — a program, a function or a prototype — or by its public CLR type — a class or an interface. Every unit of a
/// module carries one whether or not the module carries signatures (§7.1), and the assembly's
/// <see cref="CobolRepositoryAttribute.Units"/> index lists the same rows.
/// <para>Like every attribute of the repository schema it is <c>sealed</c>, not inherited, takes its identity in one
/// constructor and every other fact as a named property of a primitive, <c>string</c>, array or <c>int</c>-backed enum
/// type, and references no <c>System.Type</c>, so a reader decodes it from metadata without loading the assembly (§6).</para>
/// </summary>
/// <param name="kind">What the unit is.</param>
/// <param name="name">The unit's user-defined word as declared (the program-name, user-function-name, class-name or
/// interface-name); the word <see cref="CobolRepositoryAttribute.Units"/> lists.</param>
/// <param name="externalizedName">The unit's externalized name as formed (DOC-A.1-68), compared by the Annex C fold.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
public sealed class CobolUnitAttribute(RepositoryUnitKind kind, string name, string externalizedName) : Attribute
{
    /// <summary>What the unit is.</summary>
    public RepositoryUnitKind Kind { get; } = kind;

    /// <summary>The unit's user-defined word as declared.</summary>
    public string Name { get; } = name;

    /// <summary>The unit's externalized name as formed.</summary>
    public string ExternalizedName { get; } = externalizedName;
}
