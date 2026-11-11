**Addendum (2026-11-10): 1 more rule(s) from the 1985 code read.** The 1985 code read (2026-11-10) found the format 1 SEARCH start rule this note cites: §1985:VI.6.22.4 GR2 ends the search at once only when the index exceeds the highest permissible occurrence number, and EmitSerialScan emits the 2023 start test (index < 1 or > bound) at every edition, so a search entered at occurrence 0 goes to AT END without evaluating a WHEN condition.

**Found by.** The 1985 code read (2026-11-10; its records are `E:/COBOL-coord/scratch/coderead-85/for-register.json`): each rule below was read against the code sites it names and probed at `--std 85`, and found non-conforming. Verdict, harm, evidence, repro and observed text are the records'; the rows' verdicts are recorded on another branch.

**Inventory.** kb/Work PB5094 still refuses a 1985 rule id in `inventory_rows`, so these rows are listed here, beside the note's own; this note claims them.

**Rules added (1; every citation checked with `scripts/spec/cite.py --check`).**

- `1985-GR-VI.6.22.4-2` - `§1985:VI.6.22.4 GR2` (record verdict: DIVERGES, harm: wrong-answer). Checked: §1985:VI.6.22.4 GR2: "is greater than the highest permissible occurrence number for identifier-1, the search is terminated immediately". Code sites read: src/WiseOwl.Cobol.Compiler/CodeGen/Verbs/ControlFlowEmitter.cs#EmitSerialScan; src/WiseOwl.Cobol.Compiler/CodeGen/Verbs/ControlFlowEmitter.cs#EmitSearchScan; src/WiseOwl.Cobol.Compiler/CodeGen/Verbs/ControlFlowEmitter.cs#EmitSearchWhens; src/WiseOwl.Cobol.Compiler/Binding/Procedure/Verbs/SearchBinder.cs#BindSearch. Code read: §1985:VI.6.22.4 GR2 ends a format 1 search at once only when the index "is greater than the highest permissible occurrence number for identifier-1, the search is terminated immediately"; an index that "corresponds to an occurrence number that is not greater than the highest permissible occurrence number for identifier-1" starts the serial search, and 1985 lets a PERFORM leave an index below its table (§1985:VI.6.23.4 GR2: "The value of the index associated with an index-name after the execution of a PERFORM or SEARCH statement may be set to an occurrence number that is outside the range of its associated table."). EmitSerialScan emits the 2023 start test (index < 1 or index > bound, §2023:14.9.37.4 GR4) at every edition with no edition arm, so at --std 85 a search entered with the index at occurrence 0 goes to AT END without evaluating a WHEN condition: a wrong answer, kb/Work PB5121. The rest of the rule conforms, read and probed at 85: the upper test uses the table's maximum or its OCCURS DEPENDING count (EmitSearchScan), EmitSearchWhens evaluates the conditions in written order at each setting, the index is incremented by one and the search ends one past the table (AT END, or the end of the statement without it), a satisfied condition leaves the index at its occurrence, and NEXT SENTENCE passes control past the next separator period (Amendment 2's sentence). The witness starts two searches at occurrence 3 of 5, one unsuccessful and one successful.

**Repro (`1985-GR-VI.6.22.4-2`).** `cobol.exe <file> --std 85 --source-format fixed --run`

```cobol
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  TBL-INIT PIC X(5) VALUE "ABCDE".
       01  TBL REDEFINES TBL-INIT.
           05  ELT PIC X OCCURS 5 TIMES INDEXED BY IX.
       01  N PIC 9.
       PROCEDURE DIVISION.
       MAIN-PARA.
           PERFORM NULL-PARA VARYING IX FROM 2 BY -1 UNTIL IX < 1.
           SEARCH ELT AT END DISPLAY "AT END"
               WHEN IX = 2 DISPLAY "FOUND AT 2".
           STOP RUN.
       NULL-PARA.
           EXIT.
```

**Expected (from the 1985 text, the record's).** FOUND AT 2. The PERFORM leaves IX at occurrence 0, which is not greater than the highest permissible occurrence number 5, so §1985:VI.6.22.4 GR2 b applies: "the SEARCH statement operates by evaluating the conditions in the order that they are written", and "If none of the conditions is satisfied, the index-name for identifier-1 is incremented to obtain reference to the next occurrence." The condition compares the index's occurrence number with 2 (§1985:VI.6.3.1.1.3: "The occurrence number that corresponds to the value of the index-name is compared to the data item or literal."); it is false at 0 and 1 and true at 2, and no table element is referenced.

**Observed (the record's).** exit 0, no diagnostic; the program displays AT END.
