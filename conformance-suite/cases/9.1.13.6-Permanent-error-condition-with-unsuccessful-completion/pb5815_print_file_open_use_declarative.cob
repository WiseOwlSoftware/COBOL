      *>--- conformance-case
      *> id: pb5815_print_file_open_use_declarative
      *> rules: [GR-9.1.13.6-6, GR-14.9.27.4-16, GR-14.9.27.4-4]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5815 - the exception paths of the print file's OPEN
      *> 37, with a USE declarative and with an OPTIONAL file. A record
      *> sequential file written AFTER ADVANCING carries its print
      *> controls between its records, so it does not support read
      *> operations (docs/CONFORMANCE.md DOC-A.1-146 (c')) and OPEN
      *> INPUT is status 37
      *> (§2023:9.1.13.6 GR6a.3: "the INPUT phrase is specified but the
      *> file will not support read operations").
      *> Why each leg can fail:
      *>   PA  no FILE STATUS, a USE AFTER STANDARD ERROR on PA: the 37
      *>       executes the USE procedure (DECL PA) and execution goes
      *>       on (AFTER OPEN). Fails if the OPEN succeeds (no DECL PA)
      *>       or the 37 bypasses the declarative (a fatal stop, no
      *>       AFTER OPEN).
      *>   PO  an OPTIONAL file described with LINAGE, absent: it is a
      *>       print file by its description, and the read capability
      *>       is asked before presence, so OPEN INPUT is 37, not the
      *>       successful open of an absent optional file. Fails if
      *>       the absent file's open answers first.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5815US.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PA ASSIGN TO "pb5815u.dat".
           SELECT OPTIONAL PO ASSIGN TO "pb5815o-absent.dat"
               FILE STATUS IS SO.
       DATA DIVISION.
       FILE SECTION.
       FD  PA.
       01  RA PIC X(3).
       FD  PO LINAGE IS 5 LINES.
       01  RO PIC X(3).
       WORKING-STORAGE SECTION.
       01  SO PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON PA.
       D1. DISPLAY "DECL PA".
       END DECLARATIVES.
       M SECTION.
       M1.
           OPEN OUTPUT PA.
           MOVE "A01" TO RA. WRITE RA AFTER ADVANCING 2 LINES.
           CLOSE PA.
           OPEN INPUT PA.
           DISPLAY "AFTER OPEN".
           OPEN INPUT PO.
           DISPLAY "PO OPEN INPUT " SO.
           STOP RUN.
