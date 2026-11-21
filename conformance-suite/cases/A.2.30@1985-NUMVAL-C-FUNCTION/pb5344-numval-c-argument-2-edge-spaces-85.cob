      *>--- conformance-case
      *> id: pb5344-numval-c-argument-2-edge-spaces-85
      *> rules: [1989-AR-A.2.30.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5344. "cs is the string of one or more characters
      *> specified by argument-2" (§1989:A.2.30.3 AR1, cite.py: OK), so
      *> CS PIC X(3) VALUE "$" is the string "$  ". AR1's formats put
      *> [space] around cs, so "$  123.45" conforms whether cs is "$  "
      *> or the trimmed "$", and its value is 123.45 at either reading;
      *> the readings differ only on an argument-1 that conforms to
      *> neither, whose result is undefined (§1989:A.1.2.2). WiseOwl
      *> trims argument-2's edge spaces at every edition (docs/conformance,
      *> the PB5344 entry). This case pins the conforming value only.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5344N.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 CS PIC X(3) VALUE "$".
       01 A1 PIC X(10) VALUE "$  123.45".
       01 N PIC S9(5)V99 SIGN LEADING SEPARATE.
       PROCEDURE DIVISION.
       P0.
           COMPUTE N = FUNCTION NUMVAL-C (A1, CS).
           DISPLAY "A1 " N.
           STOP RUN.
