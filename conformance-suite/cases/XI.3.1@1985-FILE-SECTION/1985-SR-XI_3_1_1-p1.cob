      *>--- conformance-case
      *> id: 1985-SR-XI_3_1_1-p1
      *> rules: [1985-SR-XI.3.1.1-p1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XI.3.1.1 p1 (FILE SECTION). A refusal: the rule forbids
      *> line 10 of the program below, and WiseOwl refuses it at 85 with
      *> COBOL0307 there; a compiler that accepts it fails. Placed from
      *> written and run by the PB5453 reader of
      *> lineage-reads-1985/s-16.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. X11L.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "x11l.tmp".
       DATA DIVISION.
       FILE SECTION.
       SD SF RECORD CONTAINS 1 CHARACTERS
       01 SR PIC X.
       PROCEDURE DIVISION.
       MAIN-S SECTION.
       P-0.
           SORT SF ON ASCENDING KEY SR
               INPUT PROCEDURE IS IN-P
               OUTPUT PROCEDURE IS OUT-P.
           DISPLAY "SORTED".
           STOP RUN.
       IN-P SECTION.
       IN-1.
           MOVE "Z" TO SR. RELEASE SR.
       OUT-P SECTION.
       OUT-1.
           RETURN SF AT END DISPLAY "END".
           DISPLAY "GOT " SR.
