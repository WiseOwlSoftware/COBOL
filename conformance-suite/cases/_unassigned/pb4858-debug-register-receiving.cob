      *>--- conformance-case
      *> id: pb4858-debug-register-receiving
      *> rules: []
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4858, review round 2 R2-3. X3.23-1985 USE FOR
      *> DEBUGGING general rule 11 says how the facility updates the
      *> members of DEBUG-ITEM "immediately before control is passed to
      *> that debugging section"; no rule bars a program's own store, and
      *> syntax rule 11 restricts only where the register is referenced
      *> (within a debugging section). With the standard silent a store
      *> is accepted and lands in the register (GnuCOBOL 3.2 does so).
      *> Why it can fail: a code generator that has no store for the
      *> register dies with an unhandled exception on a reference-
      *> modified DEBUG-CONTENTS, and a binder that refuses the store
      *> rejects legal source.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4858RR.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. IBM-PC WITH DEBUGGING MODE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC X(4) VALUE "ABCD".
       01 W5 PIC X(5).
       PROCEDURE DIVISION.
       DECLARATIVES.
       DB1 SECTION.
           USE FOR DEBUGGING ON ALL REFERENCES OF A.
       DB1-B.
           DISPLAY "[" DEBUG-CONTENTS (1:4) "]".
           MOVE "Z" TO DEBUG-CONTENTS (1:1).
           DISPLAY "[" DEBUG-CONTENTS (1:4) "]".
           MOVE "Q" TO DEBUG-NAME.
           DISPLAY "[" DEBUG-NAME (1:3) "]".
           MOVE "AB" TO DEBUG-LINE.
           DISPLAY "[" DEBUG-LINE "]".
           MOVE 5 TO DEBUG-SUB-1.
           MOVE DEBUG-SUB-1 TO W5.
           DISPLAY "[" W5 "]".
           MOVE ALL "X" TO DEBUG-ITEM.
           DISPLAY "[" DEBUG-NAME (1:3) "]" DEBUG-LINE.
       END DECLARATIVES.
       MAIN SECTION.
       M1.
           DISPLAY A.
           STOP RUN.
