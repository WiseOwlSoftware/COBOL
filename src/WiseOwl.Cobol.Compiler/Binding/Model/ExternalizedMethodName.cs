// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime;

namespace WiseOwl.Cobol.Compiler.Binding.Model;

/// <summary>A method's EXTERNALIZED name (ISO §2023:11.7.4 GR1b): "the name is method-name-1. However, literal-1, if specified,
/// is the name of method that is externalized to the operating environment"): the ROSTER KEY of a class's instance and factory
/// interfaces and of an interface's prototypes (<see cref="OoMethodSymbol.RosterKey"/>, kb/Work PB303). An INVOKE names its method
/// only by literal-1 or identifier-2 (§2023:14.9.23.2 gives it no word form), resolved "as described in 8.3.2.2, User-defined
/// words" against this name (§2023:14.9.23.4 GR2a), so a roster is keyed by no COBOL word the program declares but by the
/// operating environment's name, and it is keyed by a type of its own, never a string (docs/rearchitecture/DESIGN-listing.md
/// section 6.7.1, "Step 6b, the OO names"), as the AS NESTED table is keyed by <see cref="ExternalizedProgramName"/>. Two names
/// are equal under the user-word rule GR2 a) cites, the Annex C fold.</summary>
public readonly struct ExternalizedMethodName : IEquatable<ExternalizedMethodName>
{
    /// <summary>The one equality of the name, the user-word rule GR2 a) cites.</summary>
    private static readonly StringComparer Rule = CobolNames.Comparer;

    private readonly string _name;

    /// <summary>The externalized method name <paramref name="name"/>: a METHOD-ID's AS literal or method-name, or the method an
    /// INVOKE's literal or a property's accessor names.</summary>
    public ExternalizedMethodName(string name) => _name = name;

    /// <inheritdoc/>
    public bool Equals(ExternalizedMethodName other) => Rule.Equals(_name, other._name);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is ExternalizedMethodName other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => Rule.GetHashCode(_name ?? "");

    /// <summary>Equal names.</summary>
    public static bool operator ==(ExternalizedMethodName left, ExternalizedMethodName right) => left.Equals(right);

    /// <summary>Different names.</summary>
    public static bool operator !=(ExternalizedMethodName left, ExternalizedMethodName right) => !left.Equals(right);

    /// <inheritdoc/>
    public override string ToString() => _name;
}
