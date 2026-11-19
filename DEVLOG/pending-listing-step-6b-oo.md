## PB5932: the source listing, step 6b, the OO names - the object-class-names and interface-names move into the symbol table's compilation-group tier

Step 6b of `docs/rearchitecture/DESIGN-listing.md` (kb/Work PB5932) makes `SymbolTable` the one store of the program's names, landing
by name class behind a temporary shrink-only baseline (kb/Work/PB5932/2026-11-18-land-by-class.md). This entry lands the OO lane, from
`listing-step-6b-oo`, run beside the data-name lane (controller 2026-11-19): the object-class-names, interface-names and method
rosters, and the PROPERTY specifiers, each change shown behaviour-neutral by the architecture oracle; the lane lowers the baseline by
46 (rule (1) 8 tables, rule (3) 7 lookups, rule (5) 31 sites): 223 to 177 on its base, and 201 to 155 with slice 7, merged.

**The method rosters.** An INVOKE names its method only by literal-1 or identifier-2 (§2023:14.9.23.2), resolved "as described in
8.3.2.2, User-defined words" (§2023:14.9.23.4 GR2) against the method's externalized name (§2023:11.7.4 GR1 b)), so a class's instance
and factory rosters and an interface's prototypes are keyed by a type of their own, `ExternalizedMethodName` (`OoMethodSymbol.RosterKey`),
as slice 6 keyed the AS NESTED table by `ExternalizedProgramName`. `FindMethod`, `FindFactoryMethod`, `FindOwnPrototype` and
`MethodRoster.Find` take it; the interface-conformance and inherited-prototype groupings key it too.

**The compilation group's tier.** A class or interface definition is a source unit of the group, built before any element binds, and
one element's REPOSITORY names another unit's class (§2023:8.4.6.4), so the names are the group's: `SymbolTable.ClassNames`, one per
`OoClassTable` (`OoClassTable.Names`), owns the group's classes and interfaces, the imported ones by identity and the identities asked,
the §2023:8.4.6.4 funnel (`Resolve` and `Lookup`, which was `OoNameResolution`, deleted; they take the referring SITE, whose census
and scope a use belongs to, and the written WORD, one token), each element's REPOSITORY scope (`SymbolTable.RepositoryScope`, which
was `IdentityScope`), an interface's method surface by method-name with the §2023:11.6.3 SR5 choice, the §2023:9.3.8.2.3 RETURNING
reference graph and the INHERITS, END-marker and accessor comparisons. A class name a symbol carries (an object-reference description,
a RAISING element) resolves from the carrier (`ClassOf`, `InterfaceOf`, `SkeletonOf`); `OoClassTable.Find` and `FindInterface` are
gone, and the specifier questions that read the group whatever the scope (§2023:12.3.8.3 SR6, SR9, SR16) are adjudicated in
`OoNameResolutionDriftTests`, rewritten for the tier. The PROPERTY specifiers (`ConfigurationSlice.OoRepositoryProperties`, deleted)
are the element tier's (`SymbolTable.Properties.cs`), inherited with the configuration-section names.

**Evidence.** `python scripts/arch/compare_oracle.py` against the base 84097a3e22: `=== ARCH-ORACLE: IDENTICAL — 22647 cases ===`
after the rosters and again after the tier. `NameTableOwnershipDriftTests` (both baseline reds seen first: STALE entries naming exactly
the moved findings), `MethodRosterLookupDriftTests`, `OoNameResolutionDriftTests` (shown red on a planted group-wide reader in
`OoDriver.cs`), `UserWordDeclarationDriftTests`, `OoDataBinderBoundaryDriftTests`: green. `string-parameter-members.json` 125 -> 123 ->
125, `name-comparisons.json` 86 -> 81. 36 inventory rows name the tier's members with their retired witnesses
(`audit_witness_loss.py --check`: 0 unexcused, 39 retired - GREEN).

**What the OO class leaves.** The parameterized definitions' expansion (`OoExpansion.Expand`, 18 rule (5) sites) runs before the
class table exists; where its names live is the next OO sub-slice's design question (kb/Work PB5932's dated file
`2026-11-19-step-6b-oo`). Lead filed: kb/Work PB5945 (an inherited method's identity is method-name in the interface surface and the
SR5 choice, the externalized name in every roster and the SR5 grouping).

**Section 9.** The `None` row of the front end and bind, twice on the branch (f6cd542052) and twice on the parent 84097a3e22, alternating (parent, branch, branch, parent), Release: every branch `None` row lies inside the parent's two runs or within 0.09 MB of them (IX113A 69.07 and 69.02 MB against 69.05 and 68.99; NC105A 35.20 and 35.04 against 35.11 and 35.06; NC218A 30.94 and 31.08 against 30.96 and 31.56; SM201A 15.41 and 15.21 against 15.35 and 15.40), so the lane moves no measurable allocation; the `None` budget holds (the benchmark README's step-6b table, the OO lane).

**Review round 1.** R1-1: the design now names the compilation group tier's recording point, `SymbolTable.ClassNames.Lookup`, beside `Identify` (DESIGN-listing.md section 6.7.1), and `OoNameResult` is made only by the table (`NameTableOwnershipDriftTests`, shown red on a planted one). R1-2: a COBOL2692 at a RAISING class-name now quotes the word as written, as every other site does (it quoted the upper-folded word before); `RaisingPhraseTupleTests.ContainedHeaderRaising_ClassNameSpelledLikeOwnData_2692_NamesTheWordAsWritten` pins it. R1-3: a doc comment the rename cut is whole again. Each line is in kb/Work PB5932's dated file `2026-11-19-listing-step-6b-oo-review-r1`.
