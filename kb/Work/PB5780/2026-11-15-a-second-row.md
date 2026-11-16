**2026-11-15, a second row joins this mechanism (the Mythos lane's batch-5 code reads, mechanism 2; source `kb/Work/PB5683/2026-11-15-rulings-code-reads-batch-5.md`).**

**Ruling: UPHELD (b), harm wrong_answer.** The row `1985-GR-IV.4.3.8.4.4-2` is the mechanism of this note: a reference-modified numeric item is compared as a number. The checker's reasoning: `OperandText.IsStringVisitor` reads the inner item's numeric category for a `RefModPlace`, so the relation takes the numeric arm. This note cites GR5 and claims `1985-GR-IV.4.3.8.4.4-5`; GR2's "as if it were redefined as an alphanumeric item" is the other half of the same requirement. Re-checked present at the new pin d7d3f99539 (the MOVE binder change does not reach conditions): `IF N5 (1:3) > 5` with N5 = 123 prints TRUE, where "123" against "5  " is false. Evidence not rebuilt beyond that one run. GR2's position mapping (slices, receiving stores) conforms per the refuter's probes. This row joins this note. The 2023 row `GR-8.4.3.3.4-2` is the checker's pointer. The checker's confidence: high.

**Row added.** `1985-GR-IV.4.3.8.4.4-2`. `work.py check` refuses a 1985 rule id in `inventory_rows` (kb/Work PB5094), so the row is named here.

**Where** (the checker's reading, the same as the body's): `src/WiseOwl.Cobol.Compiler/CodeGen/Emit/OperandText.cs#IsStringVisitor.Visit(BoundFieldOperand)`.

**Rules (each checked again with `python scripts/spec/cite.py --check`, verdict OK).**
- `§1985:IV.4.3.8.4.4 GR2`: "as if it were redefined"
  `OK  §1985:IV.4.3.8.4.4 GR2  (4.3.8.4.4 General Rules)  the text is in GR2`
- `§1985:IV.4.3.8.4.4 GR5`: "the categories numeric, numeric edited, and alphanumeric edited are considered class and category alphanumeric"
  `OK  §1985:IV.4.3.8.4.4 GR5  (4.3.8.4.4 General Rules)  the text is in GR5`
- `§1985:VI.6.3.1.1.2`: "For nonnumeric operands, or one numeric and one nonnumeric operand, a comparison is made with respect to a specified collating sequence of characters"
  `OK  §1985:VI.6.3.1.1.2  (6.3.1.1.2 Comparison of Nonnumeric Operands)  the text is in the clause`

**Repro** (`E:/COBOL-coord/scratch/my-20261115/contradictions-b5/src/refcmp.cob`), run at `--std 85` on a compiler built at main d7d3f99539: exit 0, `TRUE` (expected `FALSE`).

```cobol
       IDENTIFICATION DIVISION.
       PROGRAM-ID. REFCMP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N5 PIC 9(3) VALUE 123.
       PROCEDURE DIVISION.
       P.
           IF N5 (1:3) > 5 DISPLAY "TRUE" ELSE DISPLAY "FALSE".
           STOP RUN.
```
