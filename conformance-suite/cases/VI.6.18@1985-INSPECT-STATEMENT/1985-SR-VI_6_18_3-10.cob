      *>--- conformance-case
      *> id: 1985-SR-VI_6_18_3-10
      *> rules: [1985-SR-VI.6.18.3-10]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.18.3 SR10 (THE INSPECT STATEMENT). A refusal: the
      *> rule forbids line 8 of the program below, and WiseOwl refuses
      *> it at 85 with COBOL6859 there; a compiler that accepts it
      *> fails. Placed from written and run by the 1985 audit's drafts
      *> pass (a draft that mismatched on an older build, re-run to
      *> MATCH with its expectation unchanged, witnesses-85g) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. INSPDUP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-X PIC X(4) VALUE "ABCA".
       PROCEDURE DIVISION.
       MAIN-PARA.
           INSPECT WS-X CONVERTING "ABA" TO "XYZ".
           DISPLAY WS-X.
           STOP RUN.
