**2026-11-15, INSPECT TALLYING joins this sweep (the Mythos lane's batch-3 code reads, mechanism 9; source `kb/Work/PB5683/2026-11-15-rulings-code-reads-batch-3.md`).**

**Ruling: UPHELD (b), harm under_rejects and misleads_diagnosis.** The row `1985-SR-VI.6.18.3-6` is the mechanism of this note on another verb. `INSPECT S TALLYING CN (1:2) FOR ALL "A"` is not refused: the compiler's C# backend then fails (`CS1503`, exit 70). There is no .NET exception and no stack trace: the compiler's own pipeline finishes, then reports `error: backend compilation failed (generated C# at in1.g.cs): (51,111): error CS1503: Argument 4: cannot convert from 'long' to 'string?'` and exits 70. The same symptom this note records for STRING and UNSTRING. The failing generated line is `CN = CobolNum.StoreImage(CobolString.SpliceInto(CobolNum.FormatImage(CN, _P_1), 1, 2, (long)(...)...`. It is classified as this note classifies its arms (crashes: false, under_rejects), with misleads_diagnosis, because the user sees a C# error instead of the COBOL rule; if the lane counts a backend refusal as a crash, the harm becomes crashes.

**Mechanism (the checker's reading).** `SR6` requires identifier-2 to reference an elementary numeric data item; a reference modification of a numeric DISPLAY item is class and category alphanumeric (`§1985:IV.4.3.8.4.4 GR5`), so the counter `CN (1:2)` is illegal and needs a COBOL diagnostic. `StatementValidation.CheckInspectTallyCounter` asks `counter.Item.Pic`, the BASE item's PICTURE, so the slice passes as numeric; the emitter then produces C# that Roslyn refuses. The same mechanism shape as this note: a binder screen reading the base item's PICTURE for a reference-modified operand. This row's mechanism belongs in this note's sweep.

**Where** (the checker's reading):
- `src/WiseOwl.Cobol.Compiler/Binding/Validation/StatementValidation.cs#CheckInspectTallyCounter` (`counter.Item.Pic`: the base item, not the reference-modified place)
- `InspectBinder.cs` under `src/WiseOwl.Cobol.Compiler/Binding/Procedure/Verbs/` (the call, line about 204)

**Row added.** `1985-SR-VI.6.18.3-6`. `work.py check` refuses a 1985 rule id in `inventory_rows` (kb/Work PB5094), so the row is named here. The checker's confidence: high.

**2023 row whose stored verdict is wrong.** `SR-14.9.22.3-5` (2023 says the same: SR5, with 8.4.3.3.4 GR6c); a registrar re-records it non-conforming. It is not in this note's `inventory_rows` today.

**Existing notes.** None for INSPECT (the checker searched kb/Work for `CheckInspectTallyCounter` and for INSPECT with reference modification: PB1127 and PB1830 are the USAGE INDEX counter, PB5216 the ref-modified GROUP operand refused, all landed).

**Rules (each citation checked again with `python scripts/spec/cite.py --check`, verdict OK).**
- `§1985:VI.6.18.3 SR6`: "Identifier-2 must reference an elementary numeric data item"
  `OK  §1985:VI.6.18.3 SR6  (6.18.3 Syntax Rules)  the text is in SR6`
- `§1985:IV.4.3.8.4.4 GR5`: "except that the categories numeric, numeric edited, and alphanumeric edited are considered class and category alphanumeric"
  `OK  §1985:IV.4.3.8.4.4 GR5  (4.3.8.4.4 General Rules)  the text is in GR5`
- `§2023:14.9.22.3 SR5`: "Identifier-2 shall reference an elementary numeric data item"
  `OK  §2023:14.9.22.3 SR5  (Syntax rules)  the text is in SR5`
- `§2023:8.4.3.3.4 GR6c`: "the categories numeric and numeric-edited are considered class and category national if the usage is national; otherwise they are considered class and category alphanumeric"
  `OK  §2023:8.4.3.3.4 GR6c  (General rules)  the text is in GR6c`

**Repro** (`E:/COBOL-coord/scratch/my-20261115/contradictions-b3/src/in1.cob`):

```cobol
       IDENTIFICATION DIVISION.
       PROGRAM-ID. IN1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S PIC X(8) VALUE "ABACADAE".
       01 CN PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
       P1.
           INSPECT S TALLYING CN (1:2) FOR ALL "A".
           STOP RUN.
```

**Expected:** a COBOL compile-time diagnostic (`§1985:VI.6.18.3 SR6` with `IV.4.3.8.4.4 GR5`; `§2023:14.9.22.3 SR5` with `8.4.3.3.4 GR6c`). **Observed** at `--std 85` and at `--std 2023`: exit 70, `backend compilation failed`, `CS1503`, no exception and no stack trace.
