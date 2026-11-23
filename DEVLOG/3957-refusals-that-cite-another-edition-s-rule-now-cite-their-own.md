## Entry 3957 — 2026-11-22 22:30 PST — Refusals that cite another edition's rule now cite their own (PB6072)

Seven sites named in kb/Work PB6072 cited the 2023 clause, or 2023 words, at `--std 85`, 2002 or 2014. Each was re-probed on the branch build first.

**What no longer reproduced.** `CounterResolver.LinageFileOf` (both arms and the qualified arm cite the running edition through `LinageCounterRule`), the level-66 arm of the RENAMES level-number refusal (it cites §1985:VI.5.11.3 SR4 at 85), and the `DataBinder` comment that put SR3 text under SRL2.1 (the method moved; `UniqueOrReportAmbiguous` quotes SR3 under §1985:IV.4.3.8.1 SR3, which does hold it).

**What was fixed, and the shape.**
- `EditionCite` gained `Iso2002` and `Iso2014`, chosen by `EditionInfo.Cite`, so a FROM-phrase row carries one citation per edition instead of the 2023 locator with a 2002 number in parentheses. WRITE, REWRITE and RELEASE now name §2002:14.8.47 / 14.8.33 / 14.8.30 and §2014:14.9.47 / 14.9.33 / 14.9.30 with that edition's quotation (the 2002 REWRITE and WRITE FILE rules restrict "an intrinsic function"; the 2002 WRITE rule reads "alphanumeric, or national"). The zero-length-literal rule became an `EditionCite` (its 2014 SR4 is the first text that has it).
- The RENAMES refusals of `ResolveRedefinesPass` (SR3, SR4, SR5, SR6, the range rule, the integral-bytes rule and the window rule) take §2002:13.16.43.2 and §2014:13.18.45.2, with the 2002 wording of the OCCURS and range rules.
- `FileControlBinder.ScreenRecordDependingOccurs` cites §2002:8.4.1.2.2 SR5 and §2014:8.4.1.2.2 SR5 for the unsubscripted table element.
- `FileControlKeyRules`: the `Of1985` rows restated kind, role, precondition and predicate of the 2023 rows they replace. `Restating` now reads them from the superseded row, so they cannot drift, and the same helper builds `Rules2014`: at 2014 the key clauses say "shall not be subscripted", so a key naming a table element is §2014:8.4.1.2.2 SR5's omitted subscript, not a 2023 "subject to any OCCURS clauses" ban. `FileControlKeyRuleDriftTests` re-derives each 2014 row from the 2014 text.
- `ReservedWordSet.UserWordViolationMessage` says "user-defined word or system-name" (every edition's rule bars both: §2023:8.3.2.1 SR1, §2002:8.3.1.1, §2014:8.3.1.1, §1985:IV.4.2.2.1.3) and cites the running edition's clause.
- Comments: `ConditionRenderer` quoted §2023:8.8.4.2.4 and §2002:8.8.4.1.1.3 as one sentence and said a float "lifts to its exact value" where an approximation lifts to its shortest round-trip decimal; `IntrinsicArgumentRules` and `EvaluateBinder` cited GR1 for ZERO's numeric value, which is GR4 (GR1 governs the character-value representation).

**Tests.** `EditionCiteTests` pins the per-edition FROM-phrase function and MOVE rules; `RefusalCitesItsEditionTests` compiles a RENAMES level-1 operand, a RENAMES window, and a RECORD DEPENDING table element at 2002, 2014 and 2023 and asserts the clause each message cites; `ClosedFormatReservedWordTests` pins the reserved-word sentence at the four editions; `FileControlKeyRuleDriftTests.Every2014Row_…` checks the 2014 rows against the 2014 text.

**Not fixed (registered in the wave report).** The other `FileControlKeyRules` rows (clause numbers 12.4.5.x at 2014 are 12.4.4.x), the FILE STATUS, LINAGE and ASSIGN USING messages of `FileControlBinder.Resolve.cs`, and about a thousand `(ISO §2023:` message literals in `Binding/` that a refusal can print at an earlier edition.

