      *>--- conformance-case
      *> id: g2sum07_sum_repository_intrinsic_unprefixed
      *> rules: [2002-SR-13.16.52.2-1, 2002-GR-12.2.7.3-13]
      *> accept: [2002]
      *> source-format: fixed
      *>---
      *> A REPOSITORY-declared SUM intrinsic written without FUNCTION
      *> inside report clause operands (kb/Work PB5652).
      *> §2002:12.2.7.3 GR13 - "Within the scope of the containing
      *> environment division, intrinsic-function-name-1 may be specified
      *> in a function-identifier without being preceded by the word
      *> FUNCTION."  COBOL 2002 has no rule that makes a bare SUM in a
      *> report entry the SUM clause keyword (that is SR9 of the 2014
      *> SUM clause), so where the entry owes an operand SUM(...) is the
      *> function.
      *> §2002:13.16.52.2 SR1 - "Each data-name-1, identifier-1 or
      *> arithmetic-expression-1 is an addend.  The whole clause is
      *> referred to as a SUM clause even though the SUM keyword may
      *> appear more than once."
      *> DERIVATION.  WA = 1, WB = 10, WC = 100, so SUM(WB WC) = 110.
      *> One GENERATE DET, no UPON: each addend is added when the
      *> GENERATE executes (§2002:13.16.52.3 GR7c), then the line prints.
      *>  A  SUM OF WA + SUM(WB WC)        1 + 110       => 0111
      *>  B  SOURCE WA + SUM(WB WC)        1 + 110       => 0111
      *>  C  SOURCE SUM(1 2 3)             6             => 0006
      *>  D  SUM OF SUM(WB WC)             110           => 0110
      *>  E  SUM WA SUM (WB * WC)          a SUM written after a
      *>     complete addend begins a second SUM group (SR1's repeated
      *>     keyword), whose addend is (WB * WC):  1 + 1000  => 1001
      *>  F  PRESENT WHEN SUM(WB WC) = 110 true, present   => Y
      *>  G  PRESENT WHEN SUM(WB WC) > 110 false, absent   => space
      *>     (§2002:13.16.39.3 GR2b: as though the entry were omitted)
      *>  H  VALUE "Z"                                     => Z
      *>  I  SOURCE FUNCTION MAX(WA SUM(WB WC))  max(1,110) => 110
      *> A leg fails if the unprefixed SUM is read as the SUM clause
      *> (the entry does not compile), if E's second SUM is read as the
      *> function, or if a sum is formed wrongly.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SUM07.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-X0A SYM-X0D
               ARE 11 14.
       REPOSITORY.
           FUNCTION SUM INTRINSIC.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "g2sum07.txt".
           SELECT CHK ASSIGN TO "g2sum07.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R1.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LINE PIC X(40) VALUE SPACES.
       01  WA      PIC 9(3)  VALUE 1.
       01  WB      PIC 9(3)  VALUE 10.
       01  WC      PIC 9(3)  VALUE 100.
       REPORT SECTION.
       RD  R1 PAGE LIMIT 20 LINES.
       01  DET TYPE DE LINE PLUS 1.
           03  COLUMN 1  PIC 9(4) SUM OF WA + SUM(WB WC).
           03  COLUMN 6  PIC 9(4) SOURCE WA + SUM(WB WC).
           03  COLUMN 11 PIC 9(4) SOURCE SUM(1 2 3).
           03  COLUMN 16 PIC 9(4) SUM OF SUM(WB WC).
           03  COLUMN 21 PIC 9(4) SUM WA SUM (WB * WC).
           03  COLUMN 26 PIC X VALUE "Y"
                   PRESENT WHEN SUM(WB WC) = 110.
           03  COLUMN 28 PIC X VALUE "N"
                   PRESENT WHEN SUM(WB WC) > 110.
           03  COLUMN 30 PIC X VALUE "Z".
           03  COLUMN 32 PIC 9(3)
                   SOURCE FUNCTION MAX(WA SUM(WB WC)).
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT PRT.
           INITIATE R1.
           GENERATE DET.
           TERMINATE R1.
           CLOSE PRT.
           OPEN INPUT CHK.
           PERFORM UNTIL WS-EOF = "Y"
               READ CHK
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE CHK.
           PERFORM SHOW-LINE.
           STOP RUN.
       TAKE-BYTE.
           IF CHK-REC = SYM-X0A
               PERFORM SHOW-LINE
           ELSE
               IF CHK-REC NOT = SYM-X0D
                   ADD 1 TO WS-I
                   MOVE CHK-REC TO WS-LINE(WS-I:1)
               END-IF
           END-IF.
       SHOW-LINE.
           IF WS-I > 0
               DISPLAY "[" WS-LINE(1:WS-I) "]"
               MOVE SPACES TO WS-LINE
               MOVE 0 TO WS-I
           END-IF.
