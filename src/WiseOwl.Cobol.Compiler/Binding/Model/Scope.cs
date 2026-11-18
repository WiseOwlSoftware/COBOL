// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

namespace WiseOwl.Cobol.Binding.Model;

/// <summary>The name-resolution SCOPE of one lookup (P6 Step 7): <see cref="Program"/> for program/object-level
/// code, or a METHOD scope carrying the method's own name overlay (ISO §2023:11.7.4 GR5 — method-local names SHADOW
/// object/program names and are invisible to sibling methods). The scope is an EXPLICIT parameter of every
/// <c>SymbolTable</c> lookup — the "which overload" decision the old <c>LookupData</c>/
/// <c>LookupDataInScopeOf</c> pair encoded in the METHOD NAME is now data.</summary>
public readonly record struct Scope(OoMethodDataScope? Method)
{
    /// <summary>The program/object-level scope (no method overlay).</summary>
    public static Scope Program => new((OoMethodDataScope?)null);
}
