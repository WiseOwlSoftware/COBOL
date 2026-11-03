## Entry 3021 — 2026-11-02 23:42 PST — OoMethodSymbol carries its origin as a type, and a conformance error about a method no METHOD-ID wrote is positioned (PB4459)

**Why.** The PB2104 branch review found `OoMethodSymbol.Ctx` null for three kinds of method (the standard class BASE's, a PROPERTY
clause's generated accessor, one imported from the external repository) while `OoClassSymbol` modelled the same question as an explicit
`OoSymbolOrigin` with a throwing `Ctx`. `edition.At(ParserRuleContext?)` takes the null silently and leaves the diagnostic cursor where an
earlier construct put it, so a reader that reported a position through such a method printed an error with no position.

**Re-probe.** On the pre-change tree a class whose PROPERTY clause generates `GET PROPERTY BAL` over `PIC 9(3)`, implementing an interface
prototype `PIC 9(5)`, printed `error COBOL0841: class 'PB1449A', method 'GET PROPERTY BAL': ...` with no `file(line,col):`; so did a factory
inheriting BASE's `New` against a prototype `NEW USING P`. Both now print `probe.cob(73,18)` and `(12,11)`, the class name.

**Fix.** `OoMethodSymbol` has two constructors, one from the METHOD-ID definition (`Source`) and one from a non-`Source` `OoSymbolOrigin`
(`Standard`, `Imported`, and a new `Generated` for the PROPERTY accessor, ISO 13.18.42); `Ctx` throws on every origin but `Source`; the
`Definition` accessor and the `Imported` flag are deleted (`HasFixedClrName` asks `Origin`). `OoClassSymbol` and `OoInterfaceSymbol` take
the same two constructors, closing the `Source`-without-a-definition state their init-only `Origin` allowed. The readers that tested `Ctx`
for null ask `Origin`. `OoConformance.ValidateImplements` positions the diagnostic at the implementing class when the implementation is not
`Source`. Behaviour neutral otherwise (no emitted code changes). `docs/OO_DESIGN.md` and `DESIGN-external-repository.md` section 10.1
carry the shape.

**Witnesses.** `OoClassTablePositionTests` gains IMPLEMENTS-GENERATED-ACCESSOR and IMPLEMENTS-STANDARD-METHOD (every error positioned, COBOL0841
on the class line); `OoMethodSymbolOriginTests` pins the constructor and `Ctx` contract. No conformance-suite golden: the refusals already
have goldens and a diagnostic's position is not a conformance-suite property. No inventory row closes.

**Review round 1.** The reviewer found `OoConformance.ValidatePropertyAccessorPairs` still positioned its SR7 (COBOL2727) error through `(set.Owner == cls ? set : get).Ctx ?? accessor.Ctx`, a null-fallback the sweep had missed: with `Ctx` throwing off `Source`, a class whose own half of the pair is a PROPERTY clause's generated accessor (a generated GET over an inherited SET, or the reverse) crashed the compiler with an `InvalidOperationException` instead of reporting COBOL2727 (on `main` the error printed with no position). The error now stands at the METHOD-ID the class wrote (the later of the pair when it wrote both), else at the class name, as `ValidateImplements` does. `OoClassTablePositionTests` gains PAIR-GENERATED-GET-INHERITED-SET and PAIR-GENERATED-SET-INHERITED-GET (every error positioned, COBOL2727 on the CLASS-ID line). The remaining `.Ctx` readers over a method symbol (`ValidateOverrideSignatures`, `MarkRoster`, which skips PROPERTY subjects, and the Source-guarded binder readers) were re-checked: none can meet a generated method.

**Oracle.** `compare_oracle.py` against the base `b126084cc9f3`: `=== ARCH-ORACLE: IDENTICAL — 16164 cases (baseline b126084cc9f3, new 7ebf4c64e9cc) ===`. The stream carries each diagnostic's line and column, so no corpus program's emitted C# or diagnostics changed; the positioned COBOL0841 and COBOL2727 cases are the unit witnesses above.

