      *>--- conformance-case
      *> id: 1985-FMT-VII_4_7_2-end-of-page-pair
      *> rules: [1985-FMT-VII.4.7.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.4.7.2: WRITE record-name-1 [ FROM identifier-1 ]
      *> [ AFTER ADVANCING identifier-2 LINE ] [ AT END-OF-PAGE
      *> imperative-statement-1 ] [ NOT AT END-OF-PAGE
      *> imperative-statement-2 ] [ END-WRITE ], in that order. The
      *> branch each WRITE takes is derived from the 1985 text (each
      *> cite.py --check OK): OPEN OUTPUT sets LINAGE-COUNTER to one
      *> (§1985:VII.3.7.4 GR9d); AFTER ADVANCING 1 LINE adds one
      *> (GR9c2). With no FOOTING phrase "no end-of-page condition
      *> independent of the page overflow condition exists" (GR1), and
      *> an automatic page overflow occurs when a WRITE "would cause
      *> the LINAGE-COUNTER to exceed the value specified by
      *> integer-1" (§1985:VII.4.7.4 GR18), after which the device is
      *> at the first line of the next page and the END-OF-PAGE
      *> imperative runs. LINAGE IS 3: WRITE 1 takes the counter to
      *> 2, WRITE 2 to 3, WRITE 3 would take it to 4 (overflow: EOP 3,
      *> then the counter is one on the new page, GR9c4), WRITE 4 to 2.
      *> So the program prints NOEOP 1, NOEOP 2, EOP 3, NOEOP 4. A
      *> compiler that refuses the format fails at translation; one
      *> that runs the wrong phrase fails the comparison (registrar
      *> re-read, 2026-11-15).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RR85W1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRTF ASSIGN TO "rr85w1.prt"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  PRTF LINAGE IS 3 LINES.
       01  PR PIC X(10).
       WORKING-STORAGE SECTION.
       01  N PIC 9 VALUE 1.
       01  I PIC 9 VALUE 0.
       01  L PIC X(4) VALUE "LINE".
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT PRTF
           PERFORM 4 TIMES
             ADD 1 TO I
             WRITE PR FROM L AFTER ADVANCING N LINE
               AT END-OF-PAGE DISPLAY "EOP " I
               NOT AT END-OF-PAGE DISPLAY "NOEOP " I
             END-WRITE
           END-PERFORM
           CLOSE PRTF
           STOP RUN.
