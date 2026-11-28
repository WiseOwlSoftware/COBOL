// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Compiler.Binding.Model;

namespace WiseOwl.Cobol.Compiler.Oo;

/// <summary>Which alternatives a construct that writes an object-class-name or interface-name admits
/// (<c>SymbolTable.ClassNames.Resolve</c> and <c>Lookup</c>). <see cref="Either"/> is the USE Format-4 / USAGE OBJECT REFERENCE
/// shape (a brace group of object-class-name-1 | interface-name-1).</summary>
public enum OoNameWant { Class, Interface, Either }

/// <summary>What a written object-class-name or interface-name resolved to (<c>SymbolTable.ClassNames.Resolve</c>). Only
/// <c>SymbolTable</c> makes one (<c>NameTableOwnershipDriftTests</c>, rule (2)'s companion), so a class or interface reached from
/// a written word came through the group tier's recording point, <c>SymbolTable.ClassNames.Lookup</c>.
/// <see cref="Class"/> and <see cref="Interface"/> are mutually exclusive: classes and interfaces share ONE name namespace (a
/// collision is COBOL0820, ISO §2023:8.3.2.2), so at most one is set.
/// <para><see cref="IsFormal"/> is the third outcome, and sets neither: the name is a PARAMETER-NAME of the parameterized
/// definition the reference is written in (§2023:11.3.4 GR6 / §2023:11.6.4 GR4 — "may be specified within this class definition
/// only where an object-class-name or an interface-name is permitted"). It names whatever class or interface each expansion
/// passes (§2023:12.3.8.4 GR5 / GR8), so the definition itself knows no symbol for it — only that the reference is legal (kb/Work
/// PB2051). It is not <see cref="Ok"/>: a site that needs the symbol has nothing to read, and asks <see cref="IsFormal"/> first
/// when the definition is bound as itself.</para></summary>
public readonly record struct OoNameResult(OoClassSymbol? Class, OoInterfaceSymbol? Interface, bool IsFormal = false)
{
    public bool Ok => Class is not null || Interface is not null;

    /// <summary>The symbol-table entry the resolved name's declaration made (the class's, the interface's, or the parameter-name's;
    /// docs/rearchitecture/DESIGN-listing.md section 6.7.1, "Step 6b, the entry ids"), set by <c>SymbolTable.ClassNames.Lookup</c> for
    /// every answer that is <see cref="Ok"/> or <see cref="IsFormal"/>; none for no answer.</summary>
    internal Binding.SymbolId Entry { get; init; }
}
