      *>--- conformance-case
      *> id: g2src01_source_cd_compute_and_operands
      *> rules: [2002-GR-13.16.51.3-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.51.3 GR2 - the implicit COMPUTE of a SOURCE
      *>   arithmetic-expression-1, with and without ROUNDED, whose
      *>   operands are items of a record under a CD (SR4: "Identifier-1
      *>   specifies a data item defined in any section of the data
      *>   division"), executed again at every print.
      *> §2002:13.16.51.3 GR4 (first two sentences only; the row stays
      *>   open on its third sentence, a suppressed item ahead of a
      *>   printed one, whose shape the compiler refuses at 2002) -
      *>   multi-operand SOURCE distribution:
      *>   horizontally then vertically, the wrap to the first operand,
      *>   and a suppressed (OCCURS DEPENDING) trailing repetition.
      *> The communication facility is obsolete at 2002 (COBOL0903 is a
      *> warning and does not fail the case).
      *>
      *> DERIVATION (2002 text only).  CN = 7, CFR = 1.55 at the first
      *> GENERATE DET, CN = 8 at the second.
      *>  col 1  PIC 9(3) SOURCE CFR ROUNDED: SR5 "it is considered to
      *>         be an arithmetic-expression", GR2 "the implicit COMPUTE
      *>         statement has a ROUNDED phrase"; 14.6.3 raises 1 by one
      *>         because the excess digit 5 >= 5             => 002
      *>  col 5  PIC 9(3) SOURCE CFR: GR1 implicit MOVE, fraction
      *>         truncated                                   => 001
      *>  col 9  PIC 9(3) SOURCE CN * 2 + CFR ROUNDED: 15.55 => 016,
      *>         and at the second print 17.55               => 018
      *>  col 13 PIC 9(3) SOURCE CN * 2 + CFR: COMPUTE without
      *>         ROUNDED truncates (14.6.3)       => 015, then 017
      *>  col 17 PIC 9(3).99 SOURCE CN * 2 + CFR => 015.55, 017.55
      *>  GR2 "executed implicitly whenever the associated item is
      *>  printed": the second line shows the new CN, so the COMPUTE
      *>  ran again (a value frozen at INITIATE prints 016 twice).
      *> DMULTI (one GENERATE, CA..CE = AAA BBB CCC DDD, CN = 8):
      *>  line 1 COLUMNS 1 5 SOURCES CA CB             => AAA BBB
      *>  lines 2-3 LINE OCCURS 2 / COLUMN OCCURS 2, SOURCES CA CB CC CE:
      *>         GR4 "successive operands are assigned to successive
      *>         repeating printable items, horizontally and then
      *>         vertically"                   => AAA BBB / CCC DDD
      *>         (vertical-first would print AAA CCC / BBB DDD)
      *>  lines 4-5 the same 2x2 shape, SOURCES CC CE (SR6: 2 operands =
      *>         the 2 column repetitions): GR4 "If no further operands
      *>         remain, assignment begins again from the first operand"
      *>                                       => CCC DDD / CCC DDD
      *>  line 6 COLUMNS 1 5 SOURCES (CN) (CN + 1), SR7 parentheses
      *>                                       => 008 009
      *>  line 7 LINE OCCURS 1 TO 2 DEPENDING ON WS-N (= 1) over COLUMN
      *>         OCCURS 2, SOURCES CA CB CC CE: the first repetition
      *>         prints AAA BBB, the second is suppressed; GR4's third
      *>         sentence assigns CC CE to it "even though they are not
      *>         printed".  Here the suppressed repetition is the last,
      *>         so this leg shows only that the suppression leaves the
      *>         printed operands in place.
      *> cite.py --check: OK §2002:13.16.51.3 GR2, GR4, GR1;
      *>   §2002:13.16.51.2 SR4, SR5, SR6, SR7; §2002:14.6.3.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SRC01.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-X0A SYM-X0D
               ARE 11 14.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "g2src01.txt".
           SELECT CHK ASSIGN TO "g2src01.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R1.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-I    PIC 99    VALUE 0.
       01  WS-N    PIC 9     VALUE 1.
       01  WS-LINE PIC X(40) VALUE SPACES.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SDEST.
       01  CDREC.
           05  CN   PIC 9(3).
           05  CFR  PIC 9V99.
           05  CA   PIC X(3).
           05  CB   PIC X(3).
           05  CC   PIC X(3).
           05  CE   PIC X(3).
       REPORT SECTION.
       RD  R1.
       01  DET TYPE DE LINE PLUS 1.
           03  COLUMN 1  PIC 9(3) SOURCE IS CFR ROUNDED.
           03  COLUMN 5  PIC 9(3) SOURCE IS CFR.
           03  COLUMN 9  PIC 9(3) SOURCE IS CN * 2 + CFR ROUNDED.
           03  COLUMN 13 PIC 9(3) SOURCE IS CN * 2 + CFR.
           03  COLUMN 17 PIC 9(3).99 SOURCE IS CN * 2 + CFR.
       01  DMULTI TYPE DE.
           02  LINE PLUS 1.
               03  COLUMNS 1 5 PIC X(3) SOURCES ARE CA CB.
           02  LINE PLUS 1 OCCURS 2 TIMES.
               03  COLUMN 1 PIC X(3) OCCURS 2 TIMES STEP 4
                   SOURCES ARE CA CB CC CE.
           02  LINE PLUS 1 OCCURS 2 TIMES.
               03  COLUMN 1 PIC X(3) OCCURS 2 TIMES STEP 4
                   SOURCES ARE CC CE.
           02  LINE PLUS 1.
               03  COLUMNS 1 5 PIC 9(3)
                   SOURCES ARE (CN) (CN + 1).
           02  LINE PLUS 1 OCCURS 1 TO 2 TIMES DEPENDING ON WS-N.
               03  COLUMN 1 PIC X(3) OCCURS 2 TIMES STEP 4
                   SOURCES ARE CA CB CC CE.
       PROCEDURE DIVISION.
       MAIN.
           MOVE 7 TO CN
           MOVE 1.55 TO CFR
           MOVE "AAA" TO CA
           MOVE "BBB" TO CB
           MOVE "CCC" TO CC
           MOVE "DDD" TO CE
           OPEN OUTPUT PRT.
           INITIATE R1.
           GENERATE DET.
           MOVE 8 TO CN
           GENERATE DET.
           GENERATE DMULTI.
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
