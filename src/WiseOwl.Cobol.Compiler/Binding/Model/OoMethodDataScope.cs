// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

namespace WiseOwl.Cobol.Binding.Model;

/// <summary>The method-local NAME SCOPE of one METHOD's data (ISO §2023:11.7.4 GR5 — a method-local name SHADOWS the
/// same name in object data; and it is INVISIBLE to sibling methods, the legacy trap-#6 cross-wiring guard made
/// structural): the identity the symbol table keys the method's own name tables by (<c>SymbolTable.MethodNames</c>, its
/// data-names, condition-names, type-names, CAPACITY registers and index-names), which a lookup consults FIRST while binding
/// this method's statements. It holds the method's type declarations in source order, and no name table. Built by <see cref="OoDataBinder.BindMethodData"/>, activated
/// per-pc by <c>StatementBinder.BindClassBody</c> through <see cref="ProcedureBindState.ActiveMethodScope"/>.</summary>
public sealed class OoMethodDataScope
{
    /// <summary>The method's own TYPE DECLARATIONS, in source order (kb/Work PB3192). A type-name declared in a method definition
    /// is a LOCAL name: §2023:8.4.6.2.2 makes global only a type-name "described with a GLOBAL clause" or "declared in a source
    /// element for an object definition", and §2023:13.18.27.3 SR4 bars GLOBAL in a method. So it is invisible to its object and
    /// to its sibling methods (§2023:8.4.6.2.1 "A local name may be referenced only in the source element in which it is
    /// declared"), it may repeat a type-name of its object without a duplicate, and inside the method it hides its object's
    /// type-name of the spelling (§2023:11.7.4 GR5). The type-names are the symbol table's (<c>SymbolTable.DeclareType</c>, which
    /// adds each declaration here).</summary>
    public IReadOnlyList<DataItem> TypeDeclarations => _typeDeclarations;
    private readonly List<DataItem> _typeDeclarations = [];

    /// <summary>Append one of the method's type declarations: <c>SymbolTable.DeclareType</c>'s, once it has keyed the type-name.</summary>
    internal void AddTypeDeclaration(DataItem template) => _typeDeclarations.Add(template);
}
