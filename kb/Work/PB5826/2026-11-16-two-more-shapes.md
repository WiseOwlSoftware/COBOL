**2026-11-16, two more shapes join this note (the Mythos lane's batch-10 code reads, the fifteenth item, a lead with no row; source `kb/Work/PB5683/2026-11-16-rulings-code-reads-batch-10.md`).**

**Ruling: UPHELD (a), harm under_rejects if this note decides that 1985 needs a refusal; otherwise none beyond the documentation.** `1 = A OR 2` (whose inserted relation is `1 = 2`) and `IF 1 = 2` compile at 85 with warning COBOL3146 only. It JOINS this note: the same 1985 sentence (`§1985:VI.6.3.1.1`, "must contain at least one reference to a variable", cite.py OK) and the same site (`ConditionBinder.WrittenRelational` to `StatementValidation.CheckRelationHasNonLiteralOperand`, which applies the 2023 "operand that is not a literal" test at every edition). The checker's confidence: high on JOINS. There is no catalog row for the sentence (this note records the gap), so no row is named.

**The checker's evidence (rulings.json, copied).** This note's half is the relation with no variable that the 2023 test lets through silently (`IF 1 + 1 = 2`); this lead is the literal-literal half, which the 2023 test flags with warning COBOL3146 under the documented determination D-RELLITERAL (`docs/conformance/3/D-RELLITERAL.md`, grounded only in the 2023 text). Whether 1985 needs a refusal or a flag suffices is this note's open choice ("a refusal (or at minimum a flag)"); its 85 arm decides both halves, and D-RELLITERAL then needs a 1985 sentence. Not a new note. The choice between a refusal and the warning is this note's.

**Where:** `src/WiseOwl.Cobol.Compiler/Binding/Procedure/Verbs/ConditionBinder.cs#WrittenRelational` and `StatementValidation.CheckRelationHasNonLiteralOperand`, as this note.

**Rule (checked again with `python scripts/spec/cite.py --check`, verdict OK).**
- `§1985:VI.6.3.1.1`: "must contain at least one reference to a variable"
  `OK  §1985:VI.6.3.1.1  (6.3.1.1 Relation Condition)  the text is in the clause`

**Repro** (`E:/COBOL-coord/scratch/my-20261115/contradictions-b10/src/relvar.cob`; exit 0 at `--std 85` and at `--std 2023`, the same output):

```cobol
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RELVAR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       P0.
           IF 1 = A OR 2 DISPLAY "T1" ELSE DISPLAY "F1".
           IF 1 = 2 DISPLAY "T2" ELSE DISPLAY "F2".
           STOP RUN.
```

**Output:** `T1` and `F2`, with warning COBOL3146 on both IFs (the message says both operands of the relation condition are literals and quotes the 2023 requirement of an operand that is not a literal; the citations in it are elided here).

**Expected:** a refusal, or at minimum a flag that cites the 1985 sentence, of each of the two conditions at `--std 85` (this note's open choice). **Observed:** both compile and run with the 2023 warning only.
