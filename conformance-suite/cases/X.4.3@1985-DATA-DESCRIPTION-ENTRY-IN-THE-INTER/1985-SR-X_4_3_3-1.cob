      *>--- conformance-case
      *> id: 1985-SR-X_4_3_3-1
      *> rules: [1985-SR-X.4.3.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.4.3.3 SR1 (THE DATA DESCRIPTION ENTRY IN THE
      *> INTER-PROGRAM COMMUNICATION MODULE). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DDIPC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G-NUM IS GLOBAL PIC S9(3)
           SIGN IS LEADING SEPARATE CHARACTER VALUE -12.
       01  G-NAME IS GLOBAL PIC X(5) JUSTIFIED RIGHT.
       01  G-AMT IS GLOBAL PIC ZZ9 BLANK WHEN ZERO.
       01  G-BIN IS GLOBAL PIC 9(4) USAGE IS BINARY
           SYNCHRONIZED RIGHT VALUE 7.
       01  G-OUT IS GLOBAL PIC 9(4).
       01  G-TXT IS GLOBAL PIC X(4) VALUE "WXYZ".
       01  G-ALT REDEFINES G-TXT IS GLOBAL PIC XX.
       01  W-PARM PIC S9(3)
           SIGN IS TRAILING SEPARATE CHARACTER VALUE +45.
       PROCEDURE DIVISION.
       MAIN-PARA.
           CALL "SUBPGM" USING W-PARM.
           DISPLAY W-PARM.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SUBPGM.
       DATA DIVISION.
       LINKAGE SECTION.
       01  LK-PARM PIC S9(3) SIGN IS TRAILING SEPARATE CHARACTER.
       PROCEDURE DIVISION USING LK-PARM.
       SUB-PARA.
           DISPLAY G-NUM.
           MOVE "AB" TO G-NAME.
           DISPLAY G-NAME.
           MOVE 0 TO G-AMT.
           DISPLAY ">" G-AMT "<".
           ADD 1 TO G-BIN.
           MOVE G-BIN TO G-OUT.
           DISPLAY G-OUT.
           DISPLAY G-ALT.
           SUBTRACT 50 FROM LK-PARM.
           EXIT PROGRAM.
       END PROGRAM SUBPGM.
       END PROGRAM DDIPC.
