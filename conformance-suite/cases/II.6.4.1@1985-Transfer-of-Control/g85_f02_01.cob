      *>--- conformance-case
      *> id: g85_f02_01
      *> rules: [1985-SR-II.6.4.1.2-1, 1985-SR-II.6.4.1.2-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:II.6.4.1.2 SR1 - G85F0201 and its contained G85F02IN
      *> each call the separately compiled G85F02SC (SEP 1, SEP 2).
      *> §1985:II.6.4.1.2 SR2 - G85F0201 calls its directly contained
      *> G85F02IN (INNER 1). A refusal or a missing line fails a leg.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F0201.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WHO PIC X(5) VALUE "MAIN ".
       PROCEDURE DIVISION.
       P-MAIN.
           DISPLAY "MAIN START".
           CALL "G85F02SC" USING WHO.
           CALL "G85F02IN".
           DISPLAY "MAIN END".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F02IN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WHO PIC X(5) VALUE "INNER".
       01  N   PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       P-IN.
           ADD 1 TO N.
           DISPLAY "INNER " N.
           CALL "G85F02SC" USING WHO.
           EXIT PROGRAM.
       END PROGRAM G85F02IN.
       END PROGRAM G85F0201.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F02SC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N   PIC 9 VALUE 0.
       LINKAGE SECTION.
       01  CALLER PIC X(5).
       PROCEDURE DIVISION USING CALLER.
       P-SC.
           ADD 1 TO N.
           DISPLAY "SEP " N " FROM " CALLER.
           EXIT PROGRAM.
       END PROGRAM G85F02SC.
