      *>--- conformance-case
      *> id: pb4713-move-cd-name
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4713. A cd-name is no data item (ISO/IEC 1989:2002 A.1.2), so MOVE CM-IN TO T names no
      *> resource a MOVE can read (ISO/IEC 1989:2023 8.4.2.1: "a statement shall contain a reference that
      *> uniquely identifies that resource"). Why it can fail: the resolver says 'CM-IN' is not defined, which
      *> sends the user hunting a declaration that is right there.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4713MOVECD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T     PIC X(5).
       COMMUNICATION SECTION.
       CD  CM-IN FOR INPUT.
       01  IN-SPECS.
           02  IN-Q PIC X(12).
           02  FILLER PIC X(75).
       PROCEDURE DIVISION.
       MAIN SECTION.
       P1.
           MOVE CM-IN TO T.
           STOP RUN.
