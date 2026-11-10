      *>--- conformance-case
      *> id: 1985-FMT-VIII_2_6_2
      *> rules: [1985-FMT-VIII.2.6.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.2.6.2 (THE I-O-CONTROL PARAGRAPH). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SAMEAR.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT REL-A ASSIGN TO DISK
               ORGANIZATION IS RELATIVE.
           SELECT REL-B ASSIGN TO DISK
               ORGANIZATION IS RELATIVE.
       I-O-CONTROL.
           SAME AREA FOR REL-A REL-B.
       DATA DIVISION.
       FILE SECTION.
       FD  REL-A LABEL RECORDS ARE STANDARD.
       01  REC-A PIC X(10).
       FD  REL-B LABEL RECORDS ARE STANDARD.
       01  REC-B PIC X(10).
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "SAME AREA OK".
           STOP RUN.
