      *>--- conformance-case
      *> id: 1985-GR-VI_6_18_4-17
      *> rules: [1985-GR-VI.6.18.4-17]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.18.4 GR17 (THE INSPECT STATEMENT). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the witnesses-85m drafting pass (a
      *> first draft for a 1985 rule no pass had drafted, its
      *> expectation derived from the rule's text and its citations
      *> checked with cite.py) by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. INSPF3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T-TAB.
           05  T PIC X(4) OCCURS 5 TIMES.
       01  SUB1 PIC 9 VALUE 1.
       01  W PIC X(5) VALUE "AAXAA".
       01  CNT PIC 99 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "AAXA" TO T (1).
           MOVE "AXXX" TO T (2).
           MOVE "XAXX" TO T (3).
           MOVE "XXXX" TO T (4).
           MOVE "XXXX" TO T (5).
           INSPECT T (SUB1) TALLYING SUB1 FOR ALL "A"
               REPLACING ALL "A" BY "B".
           DISPLAY "SUB1 " SUB1.
           DISPLAY T (1) " " T (2) " " T (3) " " T (4).
           INSPECT W TALLYING CNT FOR CHARACTERS BEFORE "X"
               REPLACING ALL "A" BY "Z" AFTER "X".
           DISPLAY CNT " " W.
           STOP RUN.
