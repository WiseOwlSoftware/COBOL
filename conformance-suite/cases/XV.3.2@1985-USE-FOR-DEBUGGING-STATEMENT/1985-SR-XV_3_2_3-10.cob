      *>--- conformance-case
      *> id: 1985-SR-XV_3_2_3-10
      *> rules: [1985-SR-XV.3.2.3-10]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XV.3.2.3 SR10 (THE USE FOR DEBUGGING STATEMENT). A
      *> refusal: the rule forbids line 14 of the program below, and
      *> WiseOwl refuses it at 85 with COBOL6334 there; a compiler that
      *> accepts it fails. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DBGTST.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. XCOMP WITH DEBUGGING MODE.
       OBJECT-COMPUTER. XCOMP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  TBL.
           05  ELEM PIC X OCCURS 3 TIMES.
       PROCEDURE DIVISION.
       DECLARATIVES.
       DBG SECTION.
           USE FOR DEBUGGING ON ELEM (1).
       DBG-PARA.
           DISPLAY "D".
       END DECLARATIVES.
       MAIN-SEC SECTION.
       P1.
           MOVE "A" TO ELEM (1).
           STOP RUN.
