      *>--- conformance-case
      *> id: g85_f08_09
      *> rules: [1985-GR-XIII.3.17.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.17.4 GR3 - a SIGN clause in a report group
      *> description entry has SEPARATE CHARACTER, so: a. the sign is
      *> the leading (or trailing) character position, not a digit
      *> position; b. 'S' is counted in the size; c. the signs are '+'
      *> and '-'.
      *> Expected (derived): the detail line holds COLUMN 1 PIC S9(3)
      *> SIGN LEADING SEPARATE SOURCE WN (-5) -> "-005" in columns 1-4,
      *> COLUMN 5 PIC S99 SIGN TRAILING SEPARATE SOURCE WP (+7) -> "07+"
      *> in columns 5-7, and COLUMN 8 "|".  The columns are contiguous,
      *> so the line reads "-00507+|" only if each signed item has its
      *> digits plus one sign position.  The report file is read back
      *> one character per record (the pb537 precedent), collecting from
      *> the first sign or digit to the "|", spaces included.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F0809.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "g85f0809.rpt".
           SELECT RDR ASSIGN TO "g85f0809.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-1.
       FD  RDR.
       01  R-CHAR             PIC X.
       WORKING-STORAGE SECTION.
       01  WN                 PIC S9(3) VALUE -5.
       01  WP                 PIC S99 VALUE 7.
       01  EOF-SW             PIC 9 VALUE 0.
       01  ON-SW              PIC 9 VALUE 0.
       01  NIMG               PIC 99 VALUE 0.
       01  IMG                PIC X(12) VALUE SPACES.
       REPORT SECTION.
       RD  R-1 PAGE LIMIT IS 10 LINES.
       01  DET-A TYPE DE LINE PLUS 1.
           02  COLUMN 1 PIC S9(3) SIGN IS LEADING SEPARATE SOURCE WN.
           02  COLUMN 5 PIC S99 SIGN IS TRAILING SEPARATE SOURCE WP.
           02  COLUMN 8 PIC X VALUE "|".
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT PRT.
           INITIATE R-1.
           GENERATE DET-A.
           TERMINATE R-1.
           CLOSE PRT.
           OPEN INPUT RDR.
           PERFORM UNTIL EOF-SW = 1
               READ RDR
                   AT END MOVE 1 TO EOF-SW
                   NOT AT END
                       IF R-CHAR IS NUMERIC OR R-CHAR = "-"
                          OR R-CHAR = "+"
                          OR (ON-SW = 1 AND R-CHAR = SPACE)
                           MOVE 1 TO ON-SW
                           ADD 1 TO NIMG
                           MOVE R-CHAR TO IMG (NIMG:1)
                       END-IF
                       IF R-CHAR = "|" AND ON-SW = 1
                           ADD 1 TO NIMG
                           MOVE R-CHAR TO IMG (NIMG:1)
                           MOVE 1 TO EOF-SW
                       END-IF
               END-READ
           END-PERFORM.
           CLOSE RDR.
           DISPLAY "IMG=[" IMG "]".
           STOP RUN.
