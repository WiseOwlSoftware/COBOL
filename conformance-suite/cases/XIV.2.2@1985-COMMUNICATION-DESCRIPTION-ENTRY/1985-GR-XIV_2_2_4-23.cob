      *>--- conformance-case
      *> id: 1985-GR-XIV_2_2_4-23
      *> rules: [1985-GR-XIV.2.2.4-23]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.2.2.4 GR23 (THE COMMUNICATION DESCRIPTION ENTRY). An
      *> output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the confirmation pass's redraft (WEAK
      *> original) by scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CDIO.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT DEF-FILE ASSIGN TO "mcs.json".
       DATA DIVISION.
       FILE SECTION.
       FD  DEF-FILE.
       01  DEF-REC PIC X(60).
       WORKING-STORAGE SECTION.
       01  MSG PIC X(10).
       COMMUNICATION SECTION.
       CD  TERM-CD FOR I-O
           SYMBOLIC TERMINAL IS TERM-NAME
           TEXT LENGTH IS TERM-LEN
           END KEY IS TERM-END
           STATUS KEY IS TERM-STATUS.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT DEF-FILE.
           MOVE
           "{""terminals"":[{""name"":""TERM01"","
               TO DEF-REC.
           WRITE DEF-REC.
           MOVE
           """messages"":[""HELLO""]}]}"
               TO DEF-REC.
           WRITE DEF-REC.
           CLOSE DEF-FILE.
           MOVE "TERM01" TO TERM-NAME.
           MOVE ALL "*" TO MSG.
           RECEIVE TERM-CD MESSAGE INTO MSG
               NO DATA DISPLAY "NO DATA".
           DISPLAY "MESSAGE  [" MSG "]".
           DISPLAY "TERMINAL [" TERM-NAME "]".
           DISPLAY "LENGTH " TERM-LEN " END " TERM-END
               " STATUS " TERM-STATUS.
           STOP RUN.
