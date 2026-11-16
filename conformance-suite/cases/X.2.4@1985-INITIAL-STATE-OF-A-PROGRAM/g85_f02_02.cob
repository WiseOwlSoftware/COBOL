      *>--- conformance-case
      *> id: g85_f02_02
      *> rules: [1985-GR-X.2.4.2-1, 1985-GR-X.2.4.2-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.2.4.2 GR1 - first call of G85F02NI shows its VALUEs
      *> (NI INIT 1); the second call is not initial (NI USED 2).
      *> §1985:X.2.4.2 GR3 - G85F02IA is INITIAL: every call shows its
      *> VALUEs (IA INIT 1, three times). A carried value fails a leg.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F0202.
       PROCEDURE DIVISION.
       P-MAIN.
           CALL "G85F02NI".
           CALL "G85F02NI".
           CALL "G85F02IA".
           CALL "G85F02IA".
           CALL "G85F02IA".
           STOP RUN.
       END PROGRAM G85F0202.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F02NI.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  TXT PIC X(4) VALUE "INIT".
       01  N   PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       P-NI.
           ADD 1 TO N.
           DISPLAY "NI " TXT " " N.
           MOVE "USED" TO TXT.
           EXIT PROGRAM.
       END PROGRAM G85F02NI.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F02IA IS INITIAL PROGRAM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  TXT PIC X(4) VALUE "INIT".
       01  N   PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       P-IA.
           ADD 1 TO N.
           DISPLAY "IA " TXT " " N.
           MOVE "USED" TO TXT.
           EXIT PROGRAM.
       END PROGRAM G85F02IA.
