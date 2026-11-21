      *>--- conformance-case
      *> id: pb6019_argument_breach_text_85
      *> rules: [1989-AR-A.2.9.3-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB6019 - at COBOL-85 a function whose result is
      *> alphanumeric takes the same one path as a numeric one when
      *> its argument breaks the function's argument rules: the run
      *> unit is terminated, never continued with a made-up space.
      *>
      *> §1989:A.2.9.3 AR2 (cite.py --check): "The value of
      *> argument-1 must be greater than zero and less than or equal
      *> to the number of positions in the collating sequence."
      *> N - 1 is 0. §1989:A.2.2 p3 makes the returned value
      *> undefined, and the Correction Amendment's clarification
      *> "allows an implementation of amended third Standard COBOL
      *> to terminate processing" (§1985:p.B-46).
      *> EXPECTED: CHAR (66) is "A" in the native collating sequence
      *> (position 66 is code 65, as ORD ("A") is 66), shown as
      *> CHAR A; BEFORE is shown; CHAR (0) ends the run, so AFTER is
      *> never shown.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6019C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N  PIC S99 VALUE 1.
       01  P  PIC 99 VALUE 66.
       01  C  PIC X.
       PROCEDURE DIVISION.
       P1.
           MOVE FUNCTION CHAR (P) TO C.
           DISPLAY "CHAR " C.
           DISPLAY "BEFORE".
           MOVE FUNCTION CHAR (N - 1) TO C.
           DISPLAY "AFTER " C.
           STOP RUN.
