      *>--- conformance-case
      *> id: pb3960-refmod-zero-start
      *> rules: [GR-8.4.3.3.4-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3960. ISO 8.4.3.3.4 5) b): leftmost-position is a
      *> position of the item, 1 through its size (cite.py --check OK).
      *> The figurative ZERO is the integer 0 (8.8.1.1; 8.3.3.6.4 GR4),
      *> so T(ZERO:2) over PIC X(4) starts at position 0 exactly as T(0:2)
      *> does: the fatal EC-BOUND-REF-MOD, which this compiler refuses at
      *> compile time as COBOL2670 when the positions are literals (owner
      *> decision R60, 14.6.13.1.3 8) last paragraph). Before the fix the
      *> ZERO was bound as a computed temporary, the screen never saw it,
      *> and the program compiled and aborted at run time.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3960N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T    PIC X(4) VALUE "ABCD".
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY T(ZERO:2)
           STOP RUN.
