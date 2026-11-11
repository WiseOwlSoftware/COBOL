**Addendum (2026-11-10): 1 more rule(s) from the 1985 code read.** The 1985 code read (2026-11-10) recorded 1985-SR-VI.6.12.3-1 at NEEDS-OWNER-DECISION: it is the determination this note already asks of 1985-GR-VI.6.12.4-1, -2 and -3. At --std 85, which language-names does WiseOwl specify for ENTER: every word, the statement doing nothing (what the compiler does today: ENTER COBOL., ENTER FORTRAN SUBRTN. and ENTER WS-A. compile and execute nothing), or none, the ENTER statement refused (what GnuCOBOL 3.2 does)? §1985:VI.6.12.3 SR1 leaves the language-name to the implementor and §1985:I.1.5.2.5.5 makes the statement optional. The only statement of the choice today is docs/VERSION_CHANGE_REFERENCE.md Table 7 row 7.16; docs/CONFORMANCE.md, where CLAUDE.md rule 1 puts an implementor choice, has none. The routine-name defects of the same statement are kb/Work PB5178.

**Found by.** The 1985 code read (2026-11-10; its records are `E:/COBOL-coord/scratch/coderead-85/for-register.json`): each rule below was read against the code sites it names and probed at `--std 85`, and found non-conforming. Verdict, harm, evidence, repro and observed text are the records'; the rows' verdicts are recorded on another branch.

**Inventory.** kb/Work PB5094 still refuses a 1985 rule id in `inventory_rows`, so these rows are listed here, beside the note's own; this note claims them.

**Rules added (1; every citation checked with `scripts/spec/cite.py --check`).**

- `1985-SR-VI.6.12.3-1` - `§1985:VI.6.12.3 SR1` (record verdict: NEEDS-OWNER-DECISION, harm: under-rejects). Checked: §1985:VI.6.12.3 SR1: "Language-name-1 may refer to any programming language which the implementor specifies may be entered through COBOL. Language-name-1 is specified by the implementor.". Code sites read: src/WiseOwl.Cobol.Frontend/Grammar/CobolParserCore.g4#enterStatement; src/WiseOwl.Cobol.Frontend/Grammar/CobolParserCore.g4#enterOperand; src/WiseOwl.Cobol.Compiler/Binding/Bound/StatementBinder.cs#BindStatementCore; src/WiseOwl.Cobol.Compiler/Validation/VersionConformancePass.cs#VisitEnterStatement. Code read: At --std 85, which language-names does WiseOwl specify for ENTER: every word, with the statement doing nothing (what the compiler does today), or none, with the ENTER statement refused (what GnuCOBOL 3.2 does)? §1985:VI.6.12.3 SR1 leaves the answer to the implementor: "Language-name-1 may refer to any programming language which the implementor specifies may be entered through COBOL. Language-name-1 is specified by the implementor." §1985:I.1.5.2.5.5 makes the statement itself optional: "An implementation of Standard COBOL may include the ENTER statement or not, at the option of the implementor." What the code does: enterOperand admits any IDENTIFIER token (and LINKAGE) as language-name-1, BindStatementCore binds the statement as BoundNop, and VisitEnterStatement refuses it from 2002 (COBOL0902). So at 85 ENTER COBOL., ENTER FORTRAN., ENTER FORTRAN SUBRTN. and ENTER WS-A. all compile and execute nothing, with no diagnostic and no COBOL2894 under --flag-extensions; ENTER 123., ENTER "FORTRAN". and ENTER MOVE. are syntax errors. Why an agent cannot close it: the only statement of the choice is docs/VERSION_CHANGE_REFERENCE.md Table 7 row 7.16 ("comment-equivalent when only COBOL is supported", from DEVLOG 599 of 2026-07-04); docs/CONFORMANCE.md, where CLAUDE.md rule 1 puts an implementor choice, has no ENTER entry; that rule's precedence for implementor latitude is GnuCOBOL first, and GnuCOBOL 3.2 at -std=cobol85 refuses the same programs (measured: "'ENTER' is a reserved word, but isn't supported"); and kb/Work PB4995 already asks the owner how the ENTER general rules close, calling the position "WiseOwl specifies none". If no language may be entered, a program that writes ENTER FORTRAN SUBRTN. expects a routine that is silently never executed. The witness is ENTER COBOL. alone, which the present choice accepts.

**Repro (`1985-SR-VI.6.12.3-1`).** `cobol.exe <file> --std 85 --source-format fixed --run`

```cobol
       IDENTIFICATION DIVISION.
       PROGRAM-ID. E3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-A PIC X.
       PROCEDURE DIVISION.
       P1.
           ENTER FORTRAN SUBRTN.
           ENTER COBOL.
           DISPLAY "END".
           STOP RUN.
```

**Expected (from the 1985 text, the record's).** The owner's answer decides. §1985:VI.6.12.3 SR1 "Language-name-1 is specified by the implementor." If WiseOwl specifies no language that may be entered, FORTRAN is not a language-name and line 8 is refused (GnuCOBOL 3.2 -std=cobol85 refuses it); if every word is specified as a language-name with no effect, the program is accepted and docs/CONFORMANCE.md must say so.

**Observed (the record's).** exit 0, no diagnostic (none under --flag-extensions either); the program displays END and the routine SUBRTN is never executed.
