// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime;

namespace WiseOwl.Cobol.Compiler.Binding.Model;

/// <summary>A program's EXTERNALIZED name (ISO §2023:8.3.2.2 2)), formed from a literal by the implementor's rules
/// (<see cref="ExternalizedNames"/>, DOC-A.1-68): the key of the AS NESTED callee table (<c>StatementBinder.NestedCallables</c>,
/// §2023:14.9.4.3 SR15), which a CALL's literal-1 names exactly as the run-time program table does. An externalized name is the
/// operating environment's, no COBOL word the program declares, so the table is keyed by a type of its own, never a string
/// (docs/rearchitecture/DESIGN-listing.md section 6.7.1, "Step 6b, slice 6"), and two names are equal by the mapping rule
/// <see cref="ExternalizedNames.Same"/> states once.</summary>
internal readonly struct ExternalizedProgramName : IEquatable<ExternalizedProgramName>
{
    private readonly string _name;

    /// <summary>The name formed from <paramref name="formed"/>, already formed by <see cref="ExternalizedNames"/>' rules.</summary>
    public ExternalizedProgramName(string formed) => _name = formed;

    /// <inheritdoc/>
    public bool Equals(ExternalizedProgramName other) => ExternalizedNames.Same(_name, other._name);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is ExternalizedProgramName other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => ExternalizedNames.Comparer.GetHashCode(_name ?? "");

    /// <inheritdoc/>
    public override string ToString() => _name;
}
