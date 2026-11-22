      *>--- conformance-case
      *> id: pb5926_when_compiled_listing_heading_85
      *> rules: [1989-RV-A.2.46.3-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.46.3 RV3: "The returned value must denote the same
      *> time as the compilation date and time if provided in the
      *> listing of the source program and in the generated object code
      *> for the source program, although their representations and
      *> precisions may differ." At 85 a listing is provided:
      *> §1985:IV.7.1, each compiler "produces an output listing of the
      *> source program", and WiseOwl's page heading prints the
      *> compilation instant as YYYY-MM-DD HH:MM:SS +HH:MM (kb/Work
      *> PB5923, PB5926 ruling D-B). The test compiles with its listing
      *> (wiseowl/options/<id>.json, listing=on), which the harness
      *> writes beside the assembly in the run directory as <id>.lst;
      *> the program reads its first line, the first page's heading,
      *> and finds in it WHEN-COMPILED's own instant (positions 1-14 and
      *> 17-21 of §1989:A.2.46.3 RV1, at the heading's precision of one
      *> second: RV3 lets the precisions differ). A heading that prints
      *> another instant, or none, prints FAIL; a missing listing makes
      *> the OPEN fail.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5926WC.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT LISTING-FILE
               ASSIGN TO "pb5926_when_compiled_listing_heading_85.lst".
       DATA DIVISION.
       FILE SECTION.
       FD  LISTING-FILE.
       01  LISTING-LINE PIC X(132).
       WORKING-STORAGE SECTION.
       01  WC PIC X(21).
       01  INSTANT.
           05  I-YEAR    PIC X(4).
           05  FILLER    PIC X VALUE "-".
           05  I-MONTH   PIC X(2).
           05  FILLER    PIC X VALUE "-".
           05  I-DAY     PIC X(2).
           05  FILLER    PIC X VALUE SPACE.
           05  I-HOUR    PIC X(2).
           05  FILLER    PIC X VALUE ":".
           05  I-MINUTE  PIC X(2).
           05  FILLER    PIC X VALUE ":".
           05  I-SECOND  PIC X(2).
           05  FILLER    PIC X VALUE SPACE.
           05  I-SIGN    PIC X.
           05  I-OFF-H   PIC X(2).
           05  FILLER    PIC X VALUE ":".
           05  I-OFF-M   PIC X(2).
       01  INSTANT-TEXT REDEFINES INSTANT PIC X(26).
       01  FOUND PIC 99 VALUE 0.
       PROCEDURE DIVISION.
       P0.
           MOVE FUNCTION WHEN-COMPILED TO WC.
           MOVE WC (1:4) TO I-YEAR.
           MOVE WC (5:2) TO I-MONTH.
           MOVE WC (7:2) TO I-DAY.
           MOVE WC (9:2) TO I-HOUR.
           MOVE WC (11:2) TO I-MINUTE.
           MOVE WC (13:2) TO I-SECOND.
           MOVE WC (17:1) TO I-SIGN.
           MOVE WC (18:2) TO I-OFF-H.
           MOVE WC (20:2) TO I-OFF-M.
           OPEN INPUT LISTING-FILE.
           READ LISTING-FILE AT END MOVE SPACES TO LISTING-LINE.
           CLOSE LISTING-FILE.
           INSPECT LISTING-LINE TALLYING FOUND
               FOR ALL INSTANT-TEXT.
           IF FOUND = 1
               DISPLAY "OK   THE HEADING PRINTS WHEN-COMPILED'S INSTANT"
           ELSE
               DISPLAY "FAIL THE HEADING PRINTS " LISTING-LINE
               DISPLAY "     WHEN-COMPILED IS " WC
           END-IF.
           STOP RUN.
