      *>--- conformance-case
      *> id: 1985-GR-VI_6_22_4-3
      *> rules: [1985-GR-VI.6.22.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.22.4 GR3 (THE SEARCH STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SRCHALL.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  TBL-VALUES.
           05  FILLER PIC X(4) VALUE "10AA".
           05  FILLER PIC X(4) VALUE "20BB".
           05  FILLER PIC X(4) VALUE "30CC".
           05  FILLER PIC X(4) VALUE "40DD".
       01  TBL REDEFINES TBL-VALUES.
           05  ENT OCCURS 4 TIMES
                   ASCENDING KEY IS ENT-KEY
                   INDEXED BY IX.
               10  ENT-KEY  PIC 99.
               10  ENT-DATA PIC XX.
       PROCEDURE DIVISION.
       MAIN-PARA.
           SEARCH ALL ENT
               AT END DISPLAY "NOT FOUND"
               WHEN ENT-KEY (IX) = 30
                   DISPLAY ENT-DATA (IX)
           END-SEARCH.
           SEARCH ALL ENT
               AT END DISPLAY "NOT FOUND"
               WHEN ENT-KEY (IX) = 25
                   DISPLAY ENT-DATA (IX)
           END-SEARCH.
           STOP RUN.
