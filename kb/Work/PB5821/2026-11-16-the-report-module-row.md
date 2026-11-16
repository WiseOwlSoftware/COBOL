**2026-11-16, the Report Writer's USE format joins this note (the Mythos lane's batch-8 code reads, mechanism 5; source `kb/Work/PB5683/2026-11-16-rulings-code-reads-batch-8.md`).**

**Ruling: UPHELD (b), harm wrong_answer.** The row `1985-GR-XIII.4.8.4-4` is the mechanism of this note in the report module: a container's USE GLOBAL ... ON OUTPUT runs for a file its own non-GLOBAL USE names. This note's mechanism was INPUT in the sequential and indexed module; here it is OUTPUT in the Report Writer USE format. The row is named here and is not in this note's `inventory_rows` (`work.py check` refuses a 1985 rule id there, kb/Work PB5094). The checker's confidence: high. Same text and same output at 2023 (cause b).

**The checker's evidence.** `§1985:XIII.4.8.4 GR4` says that when file-name-1 is specified explicitly no other USE statement applies to it, and GR5b excludes the files named in another USE statement of the same condition. For the contained program's failing OPEN OUTPUT of RFX the container's USE GLOBAL ON OUTPUT therefore does not qualify, and the container's non-GLOBAL RFX USE is not visible from the contained program, so no declarative runs. Observed: `CONTAINER-GLOBAL-OUTPUT 41`. Same at 2023.

**2023 rows whose stored verdict is wrong.** `GR-14.9.49.4-5` and `GR-14.9.49.4-6` (the same two rows this note names); a registrar re-records them non-conforming.

**Where** (the checker's reading): `EmitScopeTiers` filters the non-GLOBAL file-name USE statements out (`globalOnly`) before the exclusion, and `EmitRunGlobalUse` reaches it:
- `src/WiseOwl.Cobol.Compiler/CodeGen/UseTierEmitter.cs#EmitScopeTiers`
- `src/WiseOwl.Cobol.Compiler/CodeGen/ProgramEmitter.cs#EmitRunGlobalUse`

**Rules (each citation checked again with `python scripts/spec/cite.py --check`, verdict OK).**
- `§1985:XIII.4.8.4 GR4`: "When file-name-1 is specified explicitly, no other USE statement applies to file-name-1"
  `OK  §1985:XIII.4.8.4 GR4  (4.8.4 General Rules)  the text is in GR4`
- `§1985:XIII.4.8.4 GR5b`: "for any file open in the output mode or in the process of being opened in the output mode"
  `OK  §1985:XIII.4.8.4 GR5b  (4.8.4 General Rules)  the text is in GR5b`

**Repro** (`E:/COBOL-coord/scratch/my-20261115/contradictions-b8/src/m5out.cob`):

```cobol
       IDENTIFICATION DIVISION.
       PROGRAM-ID. M5OUT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RFX ASSIGN TO "m5rf.txt" FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  RFX IS GLOBAL REPORT IS RP.
       WORKING-STORAGE SECTION.
       01  FS PIC XX VALUE "99" GLOBAL.
       REPORT SECTION.
       RD  RP.
       01  DL TYPE DE LINE PLUS 1.
           02  COLUMN 1 PIC XX VALUE "DL".
       PROCEDURE DIVISION.
       DECLARATIVES.
       G-OUT SECTION.
           USE GLOBAL AFTER STANDARD ERROR PROCEDURE ON OUTPUT.
       G-OUT-P.
           DISPLAY "CONTAINER-GLOBAL-OUTPUT " FS.
       L-RFX SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON RFX.
       L-RFX-P.
           DISPLAY "CONTAINER-LOCAL-RFX " FS.
       END DECLARATIVES.
       M SECTION.
       M-P.
           OPEN OUTPUT RFX.
           DISPLAY "OPEN-1 " FS.
           OPEN OUTPUT RFX.
           DISPLAY "OPEN-2-IN-CONTAINER " FS.
           CALL "INNER".
           CLOSE RFX.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. INNER.
       PROCEDURE DIVISION.
       I-P.
           OPEN OUTPUT RFX.
           DISPLAY "OPEN-3-IN-INNER " FS.
           EXIT PROGRAM.
       END PROGRAM INNER.
       END PROGRAM M5OUT.
```

At `--std 85` (exit 0) and at `--std 2023` (exit 0, the same output, plus warning COBOL0903 that EXIT PROGRAM is archaic):

```text
OPEN-1 00
CONTAINER-LOCAL-RFX 41
OPEN-2-IN-CONTAINER 41
CONTAINER-GLOBAL-OUTPUT 41
OPEN-3-IN-INNER 41
```

The `CONTAINER-GLOBAL-OUTPUT 41` line is the declarative that should not run: it is printed for the contained program's failing OPEN OUTPUT of RFX.
