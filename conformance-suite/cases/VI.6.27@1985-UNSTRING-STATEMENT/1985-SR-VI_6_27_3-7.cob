      *>--- conformance-case
      *> id: 1985-SR-VI_6_27_3-7
      *> rules: [1985-SR-VI.6.27.3-7]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.27.3 SR7 (THE UNSTRING STATEMENT). A refusal: the
      *> rule forbids line 10 of the program below, and WiseOwl refuses
      *> it at 85 with COBOL0900 there; a compiler that accepts it
      *> fails. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. UNSRM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 SRC PIC X(7) VALUE "AB,CD".
       01 T1 PIC X(5).
       01 T2 PIC X(5).
       PROCEDURE DIVISION.
       P1.
           UNSTRING SRC (1:5) DELIMITED BY ","
               INTO T1 T2.
           DISPLAY T1.
           DISPLAY T2.
           STOP RUN.
