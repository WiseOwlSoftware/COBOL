*>--- conformance-case
*> id: pb4235-pointer-operand-screen-word-below-2002
*> rules: [FMT-13.18.60.2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO/IEC 1989:2023 §13.18.60.2 — USAGE POINTER is a COBOL-2002 usage. kb/Work PB4235 made a TO-less
      *> operand spelled with a §8.10 screen word (AUTO, context-sensitive in the "screen description entry",
      *> a user-defined word elsewhere by §8.3.2.1 3)) the operand of a data description entry; this pins that
      *> the reading does not leak below the edition that introduced the usage — the introduction gate still
      *> names POINTER, and the entry is never refused as "'AUTO' is not a clause" (the PB4235 COBOL1941).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4235P8.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 AUTO IS TYPEDEF STRONG.
          05 A PIC X(2).
       01 DPT IS TYPEDEF USAGE POINTER AUTO.
       01 DP TYPE DPT.
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
       END PROGRAM PB4235P8.
