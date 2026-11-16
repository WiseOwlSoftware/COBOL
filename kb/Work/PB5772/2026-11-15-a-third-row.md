**2026-11-15, a third row joins this mechanism (the Mythos lane's batch-3 code reads, mechanism 6; source `kb/Work/PB5683/2026-11-15-rulings-code-reads-batch-3.md`).**

**Ruling: UPHELD (a), harm under_rejects.** The row `1985-SR-VI.5.11.3-4` is the mechanism of this note: a level 66 entry renames a level 66 entry at `--std 85`. The checker's reasoning, by the reader's own record: the PB5165 whole-scope candidate arm (`DataBinder.RegionClauseCandidates` below 2002) admits the record's level-66 alias, and `ResolveRedefinesPass.ScreenRenamesOperand` tests only levels 1 and 77; the program is refused `COBOL1655` at 2023. The checker did not rebuild the program: this note's own repro `m1ren.cob` re-run at `--std 85` still prints `R2=[ABCD]`. This row joins this note; the rows `1985-FMT-VI.5.11.2` and `1985-GR-IV.4.3.2.1-1` named in the body stay.

**Row added.** `1985-SR-VI.5.11.3-4`. `work.py check` refuses a 1985 rule id in `inventory_rows` (kb/Work PB5094), so the row is named here. The checker's confidence: high.

**Where** (the checker's reading, the same as the body's): `src/WiseOwl.Cobol.Compiler/Binding/Passes/ResolveRedefinesPass.cs#ScreenRenamesOperand`; `src/WiseOwl.Cobol.Compiler/Binding/DataBinder.cs#RegionClauseCandidates` (`ClauseOperandsTakeImplicitQualifiers`, `DialectLevel >= 2002`).

**Rule (checked again with `python scripts/spec/cite.py --check`, verdict OK).**
- `§1985:VI.5.11.3 SR4`: "A 66 level entry cannot rename another 66 level entry"
  `OK  §1985:VI.5.11.3 SR4  (5.11.3 Syntax Rules)  the text is in SR4`

**Repro:** this note's own `m1ren.cob` (copy: `E:/COBOL-coord/scratch/my-20261115/contradictions-b3/src/m1ren.cob`), run at `--std 85`: exit 0, `R2=[ABCD]`.
