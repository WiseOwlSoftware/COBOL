      *>--- conformance-case
      *> id: 1985-SR-VI_6_4_4-2
      *> rules: [1985-SR-VI.6.4.4-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.4.4 SR2 (COMMON OPTIONS AND RULES FOR STATEMENTS). A
      *> refusal: the rule forbids line 10 of the program below, and
      *> WiseOwl refuses it at 85 with COBOL0805 there; a compiler that
      *> accepts it fails. Placed from written and run by the 1985
      *> audit's drafts pass (a draft that mismatched on an older build,
      *> re-run to MATCH with its expectation unchanged, witnesses-85g)
      *> by scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. COMPT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9(18) VALUE 1.
       01 B PIC V9 VALUE 0.
       01 C PIC 9(18) VALUE 2.
       PROCEDURE DIVISION.
       P1.
           ADD A TO B.
           DISPLAY "DONE".
           STOP RUN.
