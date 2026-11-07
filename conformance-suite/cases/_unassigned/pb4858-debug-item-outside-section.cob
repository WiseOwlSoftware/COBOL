      *>--- conformance-case
      *> id: pb4858-debug-item-outside-section
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4858. X3.23-1985 USE FOR DEBUGGING syntax rule 11:
      *> "References to the special register DEBUG-ITEM are restricted
      *> to references from within a debugging section." The MOVE in
      *> M1 is outside the debugging section DB1 and references
      *> DEBUG-CONTENTS, so the program is refused (COBOL6442). The
      *> reference in DB1-B, inside the section, is legal. Why it can
      *> fail: a binder that resolves the register by name alone
      *> accepts the reference wherever it is written, and the
      *> program reads a register that holds spaces.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4858OS.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. IBM-PC WITH DEBUGGING MODE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC X(4) VALUE "ABCD".
       01 W PIC X(4).
       PROCEDURE DIVISION.
       DECLARATIVES.
       DB1 SECTION.
           USE FOR DEBUGGING ON ALL REFERENCES OF A.
       DB1-B.
           DISPLAY DEBUG-CONTENTS (1:4).
       END DECLARATIVES.
       MAIN SECTION.
       M1.
           MOVE DEBUG-CONTENTS (1:4) TO W.
           DISPLAY A.
           STOP RUN.
