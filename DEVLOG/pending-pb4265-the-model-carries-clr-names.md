## PB4265: the model carries CLR names, allocated so none is a C# keyword

**Why.** The owner asked for the backend seam leaks to go first (PB2293 campaign). PB4265 found that
DESIGN-backend-abstraction §1.2, L4, §2.4 and §6 still said a render-time `NameMangler` names everything and that no
node may carry an identifier, while the later, owner-approved external-repository design made `CsNames.Allocate` the
one bind-time allocator of the names both backends must emit, an ABI. The code follows the later design, but its field
name, `CsName`, hid that these are CLR names.

**Re-probe.** The note called the keyword question harmless. It was not: the model held the C# spelling of a name.
`DataItem.Sanitize` appended `@` to a C# keyword, so a lowercase data-name `int` was stored as `@int`, and every name
derived from it was invalid C#. `01 int PIC X(4). 01 double REDEFINES int PIC 9(4).` failed in the backend with
CS1002/CS1519 on `_redef_@int`, and a GLOBAL redefinition read by a contained program failed the same way. The same
mechanism reached `_scell_`, `__cap_` (capacity registers), `__sum_` (report SUM counters), `__fk_` (method-scope
file keys), `_impliedRecord` and the host entry's `<word>_Record`. `NamingConvention.AddressCarrierName` had already
patched one site by stripping the `@` (`TrimStart`), and that patch is deleted.

**Fix.** `Sanitize` now returns the CLR base (the identifier characters and the leading-digit underscore) with no
escape. Every `CsNameScope` kind is seeded with `CsNames.CSharpReservedKeywords`, derived from Roslyn's `SyntaxFacts`
in the same way as `ImportedTypeNames`, so `int` is allocated `int_2`. That text is the same in the metadata and in
the C#, and it stays an identifier when a prefix or suffix is added. The module namespace is the one emitted name that
is not allocated (the assembly name is the user's), so `ModuleNamespace` keeps only its CLR name and
`ProgramEmitter.CSharpNamespace` writes `Cobol.@class`. A compiler-driven, type-precise rename turned the CLR-name
members into `ClrName` (`DataItem`, the OO symbols, `BoundMethod`, the REDEFINES backings, `CallExternalBacking`,
`BoundInvoke`, the pending property op and the standard-class names) and deleted the `OoClassUnit.CsName` forwarder.
The C# method locals (`PositionLocal`, `LocalVector`, `BoundReportVaryingRef`) keep `CsName`, which is accurate: they
are the Roslyn backend's names. Both design docs now say this. DESIGN-backend-abstraction §1.3 adds two open rows: L7
for C# access text built from names in the binder (the carrier formal's `__lnk{Uid}.Value`, `AccessPath.Reroot`, the
GLOBAL bridge paths, `SortMemberPath`), and L8 for the C# locals on model nodes.

**Witnesses.** The golden `13.18.44-REDEFINES/pb4265_csharp_keyword_data_names` runs at 2002/2014/2023 (§13.18.44.4
GR2, §13.18.27.4 GR2, §8.1.3.2 3) a), each checked with cite.py). It needs no refusal case, because no edition
introduced the rule. The drift test `CsNameReservationDriftTests.EveryScopeKind_RefusesEveryCSharpReservedKeyword_AndSanitizeNeverEscapes`
covers the seed, and `EmittedGlobalNameDriftTests` reads the namespace's C# spelling from the backend. No inventory
row changed.
