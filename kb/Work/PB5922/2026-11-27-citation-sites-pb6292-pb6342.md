**2026-11-27, two more sites of this note's class, registered by tonight's fix-lane implementers (reports w20262177i-PB6292 and w20262177h-PB6342).**

Both cite §2023 clauses at every edition, including `--std 2002` and `--std 2014`, where the clause numbers differ:

- COBOL1637 (PB6292, index-name in an arithmetic subscript): the shared two-arm helpers `CiteRule` / `NotArithmetic` choose only 85 versus later. Sites: `Binding/IndexNameRule.cs` (`NotArithmetic`, `AdmitsIndexNameOnly`) and `SubscriptBinder.cs` (`IndexNameInArithmeticSubscriptError`). The 2002 and 2014 clauses (§2002:8.8.1, §2002:8.4.1.2.3) carry the same words.
- `PtrBinder.AddressConformanceReason` (PB6342, `Binding/Procedure/Verbs/PtrBinder.cs` near line 188), data and program arms: "ISO §2023:14.8.2.3.3 GR2, which applies the SET rules; §2023:14.9.39.3 SR22; §2023:8.4.3.13.4 GR3" and the BY REFERENCE text "§2023:14.8.2.3.2 GR4b". At 2002 the SET rule is SR21. The edition in force should name its own clause through `EditionInfo.Designated`. Repro: the two refused goldens of PB6342 at `--std 2002`.

Harm: misleads_diagnosis only. PB5659 and PB6149 (landed) fixed single sites of the same class; this note's 2002/2014 sweep is the owner of the rest, and its inventory of sites should take these two.
