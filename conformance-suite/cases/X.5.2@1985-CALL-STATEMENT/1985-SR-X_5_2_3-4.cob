      *>--- conformance-case
      *> id: 1985-SR-X_5_2_3-4
      *> rules: [1985-SR-X.5.2.3-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.5.2.3 SR4 (THE CALL STATEMENT). A refusal: the rule
      *> forbids line 8 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL0900 there; a compiler that accepts it fails.
      *> Placed from written and run by the witnesses-85m drafting pass
      *> (a first draft for a 1985 rule no pass had drafted, its
      *> expectation derived from the rule's text and its citations
      *> checked with cite.py) by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CALLFN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W PIC X(3) VALUE "abc".
       PROCEDURE DIVISION.
       MAIN-PARA.
           CALL "SUBFN" USING BY CONTENT FUNCTION UPPER-CASE (W).
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SUBFN.
       DATA DIVISION.
       LINKAGE SECTION.
       01  L PIC X(3).
       PROCEDURE DIVISION USING L.
       SUB-PARA.
           DISPLAY L.
           EXIT PROGRAM.
       END PROGRAM SUBFN.
       END PROGRAM CALLFN.
