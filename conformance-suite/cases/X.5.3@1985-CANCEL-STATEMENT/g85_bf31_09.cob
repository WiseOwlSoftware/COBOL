      *>--- conformance-case
      *> id: g85_bf31_09
      *> rules: [1985-GR-X.5.3.4-9]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.5.3.4 GR9 - "The contents of data items in external
      *> data records described by a program are not changed when that
      *> program is cancelled." S describes external BF31-SHR and
      *> counts its calls in BF31-CTR (VALUE 0). Call 1: S sees ALPHA,
      *> CTR=1, stores OMEGA. After the CANCEL the external record
      *> still holds OMEGA (GR9) and the recalled S sees OMEGA, while
      *> its own working-storage is back in its initial state, CTR=1
      *> again (§1985:X.5.3.4 GR2: "that program is in its initial
      *> state"). The counter half shows the CANCEL took effect, so an
      *> unchanged record is not a no-op CANCEL.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF3109.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  BF31-SHR EXTERNAL.
           05  BF31-M-TEXT PIC X(5).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "ALPHA" TO BF31-M-TEXT.
           CALL "G85BF3109S".
           DISPLAY "AFTER CALL   " BF31-M-TEXT.
           CANCEL "G85BF3109S".
           DISPLAY "AFTER CANCEL " BF31-M-TEXT.
           CALL "G85BF3109S".
           DISPLAY "AFTER RECALL " BF31-M-TEXT.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF3109S.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  BF31-SHR EXTERNAL.
           05  BF31-S-TEXT PIC X(5).
       01  BF31-CTR PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       S-PARA.
           ADD 1 TO BF31-CTR.
           DISPLAY "SUB SEES " BF31-S-TEXT " CTR=" BF31-CTR.
           MOVE "OMEGA" TO BF31-S-TEXT.
           EXIT PROGRAM.
       END PROGRAM G85BF3109S.
       END PROGRAM G85BF3109.
