      *>--- conformance-case
      *> id: pb5815_print_file_open_no_use_no_status
      *> rules: [GR-9.1.13.6-6, GR-14.9.27.4-16, GR-14.9.27.4-4]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5815 - the exception path of the print file's OPEN
      *> 37 with neither a USE declarative nor a FILE STATUS item. A
      *> record sequential file written AFTER ADVANCING carries its
      *> print controls between its records, so it does not support
      *> read operations (docs/CONFORMANCE.md DOC-A.1-146 (c')) and
      *> OPEN INPUT is status 37
      *> (§2023:9.1.13.6 GR6a.3: "the INPUT phrase is specified but the
      *> file will not support read operations").
      *> With nothing to receive it, the permanent error ends the run
      *> unit (WiseOwl: EC-I-O-PERMANENT-ERROR, fatal), so BEFORE OPEN
      *> is the only line. Fails if the OPEN succeeds or the error is
      *> passed over (AFTER OPEN printed).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5815NS.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PA ASSIGN TO "pb5815n.dat".
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
