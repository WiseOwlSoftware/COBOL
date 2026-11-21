      *>--- conformance-case
      *> id: g2-global-cd-constant-nested
      *> rules: [2002-GR-13.16.25.3-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.25.3 GR2 - "A statement in a program contained
      *> directly or indirectly within a program that describes a
      *> global name may reference that name without describing it
      *> again." KCONST and KNUM are constant entries (GLOBAL may be
      *> specified in a constant entry, §2002:13.16.25.2 SR1a written
      *> in the communication section after a CD (§2002:A.1.1 general
      *> format: constant-entry or record-description-entry after the
      *> communication-description-entry). G2GLI1 is contained directly
      *> and G2GLI2 indirectly in G2GLO; neither describes KCONST or
      *> KNUM. By §2002:13.9.3 GR1 each reference is as if the literal
      *> were written there: "OUTER GLOBALK", "INNER GLOBALK 07",
      *> "INNER2 GLOBALK 07". The communication facility draws the
      *> obsolete warning COBOL0903. Why it can fail: constants of the
      *> communication section are not entered as global names, or a
      *> global name reaches only directly contained programs.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2GLO.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  KCONST CONSTANT IS GLOBAL AS "GLOBALK".
       01  KNUM CONSTANT IS GLOBAL AS 7.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "OUTER " KCONST.
           CALL "G2GLI1".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2GLI1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WN PIC 99.
       PROCEDURE DIVISION.
       P1.
           MOVE KNUM TO WN.
           DISPLAY "INNER " KCONST " " WN.
           CALL "G2GLI2".
           EXIT PROGRAM.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2GLI2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WN2 PIC 99.
       PROCEDURE DIVISION.
       P1.
           MOVE KNUM TO WN2.
           DISPLAY "INNER2 " KCONST " " WN2.
           EXIT PROGRAM.
       END PROGRAM G2GLI2.
       END PROGRAM G2GLI1.
       END PROGRAM G2GLO.
