      *>--- conformance-case
      *> id: pb5815_print_file_open_no_use_no_status_85
      *> rules: [1985-GR-VII.2.6.3-2, 1985-GR-VII.4.3.4-17,
      *>     1985-GR-VII.4.7.4-12]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5815 - the exception path of the print file's OPEN
      *> 37 with neither a USE declarative nor a FILE STATUS item. A
      *> record sequential file written AFTER ADVANCING carries its
      *> print controls between its records, so it does not support
      *> read operations (docs/CONFORMANCE.md DOC-A.1-146 (c')) and
      *> OPEN INPUT is status 37
      *> (§1985:VII.1.3.5: "The INPUT phrase is specified but the file
      *> will not support read operations").
      *> With nothing to receive it, the permanent error ends the run
      *> unit (WiseOwl: EC-I-O-PERMANENT-ERROR, fatal), so BEFORE OPEN
      *> is the only line. Fails if the OPEN succeeds or the error is
      *> passed over (AFTER OPEN printed).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5815NS85.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PA ASSIGN TO "pb581585n.dat".
       DATA DIVISION.
       FILE SECTION.
       FD  PA.
       01  RA PIC X(3).
       PROCEDURE DIVISION.
       M1.
           OPEN OUTPUT PA.
           MOVE "A01" TO RA. WRITE RA AFTER ADVANCING 2 LINES.
           CLOSE PA.
           DISPLAY "BEFORE OPEN".
           OPEN INPUT PA.
           DISPLAY "AFTER OPEN".
           STOP RUN.
