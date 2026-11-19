## Entry 3854 — 2026-11-19 02:54 PST — PB5932: the source listing, step 6b slice 3 - the constant-names move into SymbolTable with their resolution

Step 6b of `docs/rearchitecture/DESIGN-listing.md` (kb/Work PB5932) makes `SymbolTable` the one store of the program's names, landing
by name class behind a temporary shrink-only baseline (kb/Work/PB5932/2026-11-18-land-by-class.md). This entry lands slice 3, from
`listing-step-6b-3`: the constant-name class of the progress table, shown behaviour-neutral by the architecture oracle, and the
baseline lowered from 308 to 294.

**The constant-names.** `SymbolTable.Constants.cs` owns what `ConstantEntryBinder.ConstantScope` and `InheritedGlobals` held: each
source element's declared and bound constant entries (the binder's own element and each method's, which §2023:11.7.4 GR5 makes a
source element of its own), the words a method's data division defines before its entries bind, and the containers' GLOBAL constants
with their depths, all keyed by `CobolName`. `ConstantScope`, `FindConstant`, `ConstantScopeOf`, `GlobalConstantsToInherit`,
`InheritedGlobals.Constants`, `AddContainerConstants` and the constant half of `InheritedGlobals.DropShadowed` are deleted; the
inheritance writes the table (`InheritConstants`, `DropShadowedConstants`) and `InheritedGlobals.HideShadowedSymbolics` keeps the
symbolic-character half. Every constant-name reference is `SymbolTable.Identify(written, NameClass.Constant, report)`, whose arm
binds a declared entry on demand (§2023:13.10.3 SR4 and SR5 forbid only a circular dependence); `ConstantEntryBinder` declares the
entries, binds them and joins each value through `SymbolTable.BindConstant`, which answers §2023:13.10.3 SR9's earlier duplicate.
`DeclaredConstant` and `ConstantBindState` are top-level types the two share.

**What the code proved about the design** (section 6.7.1, "Step 6b, slice 3"):

- A name no parse rule holds alone. An integer constant-name in a PICTURE repetition (`X(K)`, §2023:13.10.3 SR2 "it may also be
  used to specify repetition in a picture character-string") is part of the one PICTURE token, and a LENGTH OF operand's subscripts
  (§2023:13.10.3 SR3) are read from a token list. `Identify(WrittenWord, ...)` takes the token and the word's span in it, so step 7b
  records the word at its own column. The compile-time evaluator's name callback takes the `cobolWord` context it already held, so
  no string reaches the table from a written name.
- Before the data bind there is no table. The AS phrases read before the unit's binder exists (PROGRAM-ID, FUNCTION-ID, CLASS-ID,
  INTERFACE-ID, METHOD-ID, a REPOSITORY class or interface specifier) may name a constant (kb/Work PB2588); that reading is the
  table's static `IdentifyConstantBeforeBind` (`PreBindConstants` deleted), comparing with the table's one equality.
- `GlobalNameClassDriftTests` walks `SymbolTable` for name indexes and counts a table keyed by `CobolName`, so a class moved into the
  table stays under it.

**Evidence.** `python scripts/arch/compare_oracle.py` against the parent 9654bb00ec: `=== ARCH-ORACLE: IDENTICAL — 22642 cases ===`
after the move and after the last source change. `NameTableOwnershipDriftTests` was red on exactly the 14 entries the slice moved
(rule 1: 4, rule 3: 5, rule 5: 5) before the baseline was lowered; a planted string-keyed `ConstantDef` table with
`CobolNames.Comparer` was red under rules (1) and (5), and a planted `Dictionary<CobolName, DataItem>` in the table red in
`GlobalNameClassDriftTests`. Unit 42142 of 42142, Conformance 28182 of 28182, RepoDrift 187 of 187, Characterization 37 of 37.
`audit_witness_loss.py` asked for retired-witness marks on the six inventory rows whose code-location named a moved member
(GR-8.4.6.2.1-1, GR-8.4.6.2.1-3, SR-13.10.3-2, SR-13.10.3-5, 2002-SR-13.9.2-2, 2002-SR-13.9.2-5); no verdict changed.

**Section 9.** The `None` row of the front end and bind, measured against the parent 9654bb00ec on one host, alternating: IX113A
0.2 to 0.4 MB below the parent on every run, NC218A straddling it, SM201A equal; NC105A's first pair sat 0.4 MB above, and two
more pairs of that row alone put it at 34.90 to 35.64 MB on the branch against 35.15 to 35.78 MB on the parent, overlapping. Slice
3 moves no measurable allocation (`tests/WiseOwl.Cobol.Benchmarks/README.md`, the step-6b table, slice 3).

**What remains** (kb/Work PB5932's dated file `2026-11-19-step-6b-slice-3`): the data-name unit, the report class, the CONFIGURATION
SECTION names, the program and function names, and the OO names. **Lead filed:** kb/Work PB5925's dated file
`2026-11-19-as-phrase-constant-slot` (an AS phrase's word is labelled `nn` by step 7a but names a constant when one is declared, and
the pre-bind reader records nothing; step 7b decides).

