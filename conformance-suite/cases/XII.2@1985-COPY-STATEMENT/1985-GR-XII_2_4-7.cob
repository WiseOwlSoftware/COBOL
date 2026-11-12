      *>--- conformance-case
      *> id: 1985-GR-XII_2_4-7
      *> rules: [1985-GR-XII.2.4-7]
      *> accept: [85]
      *> files: [g7dbg.cpy, g7lib.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XII.2.4 GR7 (THE COPY STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the witness-drafting pass (a rule the
      *> audit never drafted); run with its library texts (drafts-85n,
      *> kb/Work PB5067) by scripts/spec/place_witnesses.py (kb/Work
      *> PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G7C.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. X.
       OBJECT-COMPUTER. X.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-A PIC 9 VALUE 0.
       01  WS-B PIC 9 VALUE 0.
       01  WS-C PIC 9 VALUE 0.
       01  WS-D PIC 9 VALUE 0.
       01  WS-E PIC 9 VALUE 0.
       01  WS-F PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-P.
           COPY g7lib REPLACING
               ==WS-A MOVE 1 TO WS-B MOVE== BY ==WS-D MOVE==
               ==MOVE 1
      D             TO
                    WS-E== BY ==MOVE 1 TO WS-F==.
           DISPLAY "A=" WS-A " B=" WS-B " C=" WS-C.
           DISPLAY "D=" WS-D " E=" WS-E " F=" WS-F.
           CALL "G7CDBG".
           STOP RUN.
       END PROGRAM G7C.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G7CDBG.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. X WITH DEBUGGING MODE.
       OBJECT-COMPUTER. X.
       PROCEDURE DIVISION.
       MAIN-P.
           COPY g7dbg REPLACING ==TAG-1== BY =="REPLACED"==.
       G7-EXIT.
           EXIT PROGRAM.
       END PROGRAM G7CDBG.
