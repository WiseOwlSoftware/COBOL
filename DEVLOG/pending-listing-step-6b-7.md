## PB5932: the source listing, step 6b slice 7 - the function-prototype-names move into SymbolTable, and the listing's open review leads are fixed

Step 6b of `docs/rearchitecture/DESIGN-listing.md` (kb/Work PB5932) makes `SymbolTable` the one store of the program's names, landing
by name class behind a temporary shrink-only baseline (kb/Work/PB5932/2026-11-18-land-by-class.md). This entry lands slice 7, from
`listing-step-6b-7`: the function-prototype-names, shown behaviour-neutral by the architecture oracle, the baseline lowered from 223 to
201, and every open review lead of listing steps 2 to 7.

**The function-prototype-names.** `SymbolTable.Functions.cs` owns the words the REPOSITORY paragraph's user-defined-function specifiers
declare (§2023:12.3.8.2), the specifiers in the order written and the refused ones (§2023:12.3.8.3 SR2; kb/Work PB4822), inherited with
the configuration-section names (§2023:12.3.4 GR1), and the user-defined functions the element may name (`ResolveFunctionPrototypes`,
which was `BinderDriver.UserFunctionsOf`: §2023:12.3.8.4 GR11 with §2023:8.4.6.6's containing function definition registered last,
§2023:12.3.8.3 SR11). `NameClass.FunctionPrototype` is keyed by the word the >>COBOL-WORDS directive made of the token
(`FunctionWord`, §2023:7.3.10.4 GR2 to GR4), the name every function-name lookup already read; every written function-prototype-name is
`Identify`, and `UdfBinder.UdfBindCall` takes what it identified. A restricted function-pointer's USAGE TO name is resolved by the table
from the item (`FunctionSignatureOf`, the one signature reader of §2023:14.9.39.3 SR20). The containing-definition set of both prototype
classes (`DataBinder.ContainingDefinitionNames`, slice 6's review lead R1-1) is the table's, and `StatementBinder.UdfSelfName` is deleted:
COMMIT and ROLLBACK ask the unit's kind (§2023:14.9.7.3 SR1). The intrinsic specifiers are keyed by `StandardWord`, and the group's
functions by word are a sequence the table keys (`GroupRepositoryProvider.FunctionsVisibleAt`).

`python scripts/arch/compare_oracle.py --no-build` against 84097a3e22 after the move:
`=== ARCH-ORACLE: IDENTICAL — 22647 cases (baseline 84097a3e22da, new 84097a3e22da-dirty) ===`. `NameTableOwnershipDriftTests` showed
the baseline's STALE red first (3 tables, 2 lookups, 17 sites, no new finding); the baseline and its ceiling are 201 (28 tables, 38
lookups, 135 sites). What remains is the data-name class, and the OO names with the compilation group's tier (kb/Work PB5932's dated
file `2026-11-19-step-6b-slice-7`).

**The review leads**, each checked on main and fixed at its root (the dated file's "Leads carried" lists all fifteen): the listing
request's destination is one choice (`WithDestination`), so a `with` expression no longer depends on assignment order; a listing record's
column is a character position (`ReferenceFormatMap.CharacterPositionAt`), so a supplementary-plane character before a diagnosed word
no longer moves the column one place on; the listing's severity words are held equal to the compiler's (`Diagnostic.SeverityWord`);
`CompilerDriver.Result.Diagnostics` is required; the state-encounter comments, the 4b design and test text and the 2023 test shell are
current; `GrammarNameSlotDriftTests` compares slots and labels element by element; and the citation `§2023:14.7.8 rule 2`, which the
checker refuses, is `§2023:14.7.8 GR2` in all 91 places. That last change reaches output: the oracle at the lead commit differs in 19 of
22647 cases (8 diagnostic streams, 11 emitted C#), and each of the 23 differing line pairs differs only by that citation.

**Section 9.** The `None` row of the front end and bind, twice on the slice's commit (4525d45aae) and twice on the parent 84097a3e22, alternating, Release: IX113A and SM201A inside the parent's two runs or within 0.05 MB, one run of NC105A and of NC218A inside too, the other two 0.59 and 0.58 MB above this parent pair, within the run-to-run spread the same programs show on one tree (the slice's own two NC105A runs 0.63 MB apart, NC218A 0.57 MB apart on slice 6's parent); none of the four declares a function (the benchmark README's step-6b table, slice 7).
