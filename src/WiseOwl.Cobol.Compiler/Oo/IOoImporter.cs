// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Compiler.Oo;

/// <summary>
/// ⛔ WHERE A CLASS OR INTERFACE THE COMPILATION GROUP DOES NOT DEFINE COMES FROM (ISO §12.3.8.3 SR6 b), SR9: "there shall be
/// information in the external repository for the class" / "the interface"; DESIGN-external-repository §8.4 step 3, §10.1, §10.6).
/// The class table asks it for an IDENTITY — the externalized name a class- or interface-specifier states (<c>lit ?? word</c>,
/// §12.3.8.4 GR2) — after the group's own definitions and the standard class BASE have not claimed it, and registers what it
/// answers (<see cref="OoClassTable.ImportClass"/>, <see cref="OoClassTable.ImportInterface"/>). The one implementation is
/// <c>MetadataRepositoryProvider</c>, which reads the compiled assemblies of the search space; it is an interface because the table
/// (<c>Compiler.Oo</c>) is built before the provider's binder-side collaborators exist and must not depend on them.
/// </summary>
internal interface IOoImporter
{
    /// <summary>The class published under <paramref name="identity"/>, or null when nothing in the search space answers (the
    /// provider has reported a finding of its own when it refused one). The class is returned as a registered-before-complete
    /// pair (<see cref="ImportedClass"/>): a method of the class may take the class itself, or one that takes it, so the table
    /// registers the symbol before its methods are decoded.</summary>
    ImportedClass? Class(string identity, OoClassTable table);

    /// <summary>The interface twin of <see cref="Class"/>.</summary>
    ImportedInterface? Interface(string identity, OoClassTable table);

    /// <summary>The parameterized class (or, with <paramref name="isInterface"/>, interface) published under <paramref name="identity"/>
    /// — its record and the assembly it came from — or null when nothing answers (the provider has reported a finding of its own when
    /// it refused one). An expansion of it is made in this compilation from the source the record holds (ISO §12.3.8.4 GR5 / GR8;
    /// DESIGN-external-repository §10.5).</summary>
    ImportedSkeleton? Skeleton(string identity, bool isInterface);

    /// <summary>True when asking for the class <paramref name="identity"/> (or, with <paramref name="isInterface"/>, the interface)
    /// drew a finding of its own (an unreadable or malformed entry, REPO-6, REPO-7, REPO-8 …), so a REPO-1 beside it would say the
    /// same thing twice.</summary>
    bool Refused(string identity, bool isInterface);

    /// <summary>What REPO-1 says was searched for the class (or interface) <paramref name="identity"/>: every location in order, why
    /// a file named after the unit was no answer, and the files the directory index skipped for their size (§15.2).</summary>
    string Searched(string identity, bool isInterface);
}

/// <summary>An imported class in two steps: the <paramref name="Symbol"/> with its names and attributes, which the table
/// registers, then <paramref name="Complete"/>, which resolves its superclass and decodes its methods (both through the
/// table, so every identity they name is imported or refused on the way).</summary>
public sealed record ImportedClass(OoClassSymbol Symbol, Action Complete);

/// <summary>The interface twin of <see cref="ImportedClass"/>: its INHERITS list and its method prototypes complete the symbol.</summary>
public sealed record ImportedInterface(OoInterfaceSymbol Symbol, Action Complete);

/// <summary>A skeleton another group published: its <paramref name="Record"/> and where it came from.</summary>
internal sealed record ImportedSkeleton(WiseOwl.Cobol.Binding.Model.SkeletonRecord Record, ImportedFrom From);
