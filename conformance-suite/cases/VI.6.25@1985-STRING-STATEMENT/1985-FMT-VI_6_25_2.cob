      *>--- conformance-case
      *> id: 1985-FMT-VI_6_25_2
      *> rules: [1985-FMT-VI.6.25.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.25.2 (THE STRING STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. STRTST.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  SRC PIC X(8) VALUE "ABC,DEF ".
       01  DST PIC X(10) VALUE SPACES.
       01  PTR PIC 99 VALUE 1.
       PROCEDURE DIVISION.
       MAIN-PARA.
           STRING SRC DELIMITED BY ","
                  "XY" DELIMITED BY SIZE
               INTO DST WITH POINTER PTR
               ON OVERFLOW DISPLAY "OVERFLOW"
               NOT ON OVERFLOW DISPLAY "OK"
           END-STRING
           DISPLAY "[" DST "]"
           DISPLAY PTR
           STOP RUN.
