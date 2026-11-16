      *>--- conformance-case
      *> id: g2-program-id-common-sibling-call
      *> rules: [2002-SR-11.9.2-4]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:11.9.2 SR4 - "The COMMON clause may be specified only if
      *> the program is contained within another program": the granted
      *> form, IS COMMON PROGRAM on a CONTAINED program, is accepted.
      *> §2002:11.9.3 GR2 - "A common program is contained within another
      *> program but may be called from programs other than that
      *> containing it"; §2002:8.4.5.2 rule 2 - its name "may be
      *> referenced only by statements included in that containing
      *> program and any programs directly or indirectly contained
      *> within that containing program".
      *> G2COMB is COMMON and contained in G2COM01. G2COM01 calls it
      *> (its container), and G2COMA, a SIBLING of G2COMB, calls it too:
      *> that second CALL resolves only because G2COMB is common
      *> (§2002:8.4.5.2 rule 1 would forbid it otherwise). Each leg
      *> fails if SR4's form is refused, or if the sibling CALL does not
      *> reach G2COMB (the line "B FROM SIBA" and the count 2 would
      *> be missing or wrong).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2COM01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-COUNT        PIC 9 VALUE 0.
       01  WS-WHO          PIC X(4).
       PROCEDURE DIVISION.
           MOVE "OUTR" TO WS-WHO
           CALL "G2COMB" USING WS-WHO WS-COUNT
           CALL "G2COMA" USING WS-COUNT
           DISPLAY "COUNT " WS-COUNT
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2COMA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WA-WHO          PIC X(4) VALUE "SIBA".
       LINKAGE SECTION.
       01  LA-COUNT        PIC 9.
       PROCEDURE DIVISION USING LA-COUNT.
           CALL "G2COMB" USING WA-WHO LA-COUNT
           GOBACK.
       END PROGRAM G2COMA.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2COMB IS COMMON PROGRAM.
       DATA DIVISION.
       LINKAGE SECTION.
       01  LB-WHO          PIC X(4).
       01  LB-COUNT        PIC 9.
       PROCEDURE DIVISION USING LB-WHO LB-COUNT.
           ADD 1 TO LB-COUNT
           DISPLAY "B FROM " LB-WHO
           GOBACK.
       END PROGRAM G2COMB.
       END PROGRAM G2COM01.
