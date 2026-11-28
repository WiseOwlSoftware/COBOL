## Later finding (2026-11-28): the SET binders' own rule tables print 2023 clauses at 2002 and 2014

Source: `E:/COBOL-coord/scratch/reports/w20262181a-PB6356-report.md`, New defects 1 (PB6356's implementer, after the PB6356 fix). Same root as this note (a rule cited from one edition at all four, the PB4950 rule). This note's sites do not include the following; the fix lane sweeps the rule's siblings and takes them with it. Sites at origin/main on 2026-11-28:

- `src/WiseOwl.Cobol.Compiler/Binding/Procedure/Verbs/SetFormatSelection.cs:218-254` (the format table): rule strings `ISO §2023:14.9.39.3 SR29`, `SR33`, `SR17`, `SR20`, `SR21`, `SR8`, `SR35`, `SR23` and `SR1`;
- `src/WiseOwl.Cobol.Compiler/Binding/Procedure/Verbs/SetCapacityBinder.cs:82` and `:142` (`SR30`, `SR34`);
- `src/WiseOwl.Cobol.Compiler/Binding/Procedure/Verbs/SetLocaleBinder.cs:121` (`SR26`);
- `src/WiseOwl.Cobol.Compiler/Binding/Procedure/Verbs/SetPointerBinder.cs:60`, `:89`, `:203` and `:216` (lines 60 and 89 already name the 1985 clause through `CiteRule` and `Cite`, and still print the 2023 rule at 2002 and 2014).

Harm: a diagnostic at `--std 2002` or `--std 2014` names a rule of another edition (misleads the diagnosis). No wrong answer observed. Expected: each message cites its own edition's SET rule through the edition-citation mechanism, each citation checked with `cite.py --check`, with this note's drift net covering the tables. PB6356 (landed) fixed only the SET condition-name SR7 screen. The report's item 2 (the `RejectSetConditionName` nothing-resolves arm) is this note's item 2 and is not repeated here.
