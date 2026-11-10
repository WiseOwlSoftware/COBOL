      *>--- conformance-case
      *> id: 1985-FMT-VI_6_18_2
      *> rules: [1985-FMT-VI.6.18.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.18.2 (THE INSPECT STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. INSPT1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-TEXT     PIC X(9) VALUE "BANANAXAA".
       01  WS-CNT      PIC 9(2) VALUE ZERO.
       01  WS-CN2      PIC 9(2) VALUE ZERO.
       01  WS-FROM     PIC X(3) VALUE "AM*".
       01  WS-TO       PIC X(3) VALUE "EN+".
       PROCEDURE DIVISION.
       P1.
           INSPECT WS-TEXT TALLYING WS-CNT FOR ALL "A"
               BEFORE INITIAL "X".
           DISPLAY WS-CNT.
           INSPECT WS-TEXT REPLACING ALL "A" BY "O"
               AFTER INITIAL "N".
           DISPLAY WS-TEXT.
           MOVE ZERO TO WS-CNT.
           INSPECT WS-TEXT TALLYING
               WS-CNT FOR CHARACTERS AFTER INITIAL "X"
               WS-CN2 FOR LEADING "B" ALL "O" BEFORE INITIAL "X".
           DISPLAY WS-CNT " " WS-CN2.
           INSPECT WS-TEXT REPLACING
               FIRST "N" BY "M"
               LEADING "B" BY "C"
               CHARACTERS BY "*" AFTER INITIAL "X".
           DISPLAY WS-TEXT.
           MOVE ZERO TO WS-CNT.
           INSPECT WS-TEXT
               TALLYING WS-CNT FOR ALL "O"
               REPLACING ALL "O" BY "A" BEFORE INITIAL "X".
           DISPLAY WS-CNT " " WS-TEXT.
           INSPECT WS-TEXT CONVERTING WS-FROM TO WS-TO
               AFTER INITIAL "C" BEFORE INITIAL "X".
           DISPLAY WS-TEXT.
           INSPECT WS-TEXT CONVERTING "X*" TO "Y+".
           DISPLAY WS-TEXT.
           STOP RUN.
