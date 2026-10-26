      *>--- conformance-case
      *> id: pb1515-io-warning-07-below-2023
      *> rules: [GR-9.1.13.2-6, GR-7.3.25.4-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB1515 - the edition floor of the positive golden
      *> 2023/pb1515_io_warning_07_raises_ec: I-O status '07' is a
      *> successful completion with a nonzero status, the exception
      *> condition EC-I-O-WARNING (ISO 9.1.13.1), and EC-I-O-WARNING is
      *> new in COBOL-2023 (Annex E.3.3 item 18), so enabling it by
      *> >>TURN at 2002 or 2014 is COBOL0878, never a silent no-op:
      *> before 2023 the '07' of the OPEN below raised nothing a
      *> program could handle. (At --std 85 the >>TURN directive itself
      *> is refused first, COBOL0900.)
       >>TURN EC-I-O-WARNING CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1515NG.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb1515ng.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 F-REC PIC X(4).
       PROCEDURE DIVISION.
       DECLARATIVES.
       WARN SECTION.
           USE AFTER EXCEPTION CONDITION EC-I-O-WARNING.
       WARN-P.
           DISPLAY "UNREACHABLE".
       END DECLARATIVES.
       MAIN SECTION.
       M1.
           OPEN OUTPUT F WITH NO REWIND
           CLOSE F
           STOP RUN.
