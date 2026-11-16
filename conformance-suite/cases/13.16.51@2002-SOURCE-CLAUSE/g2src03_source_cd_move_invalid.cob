      *>--- conformance-case
      *> id: g2src03_source_cd_move_invalid
      *> rules: [2002-SR-13.16.51.2-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.51.2 SR2 - "If identifier-1 is specified without the
      *> ROUNDED phrase, identifier-1 shall be described such that a MOVE
      *> statement is valid with identifier-1 as the sending operand and
      *> the printable item as the receiving operand." CA is an
      *> alphabetic PIC A(3) item of a record under a CD (§2002:13.16.51.2
      *> SR4, any section; §2002:A.1.1.2); the printable item is numeric
      *> PIC 9(3), and Table 17 (§2002:14.8.24.2 SR8) gives Alphabetic
      *> into Numeric "No": refused (COBOL2770) at line 37. The valid
      *> moves are g2src02_source_cd_move_rounded.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SRC05.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "g2src03.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R1.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CDREC.
           05 CA   PIC A(3).
       REPORT SECTION.
       RD  R1 PAGE LIMIT 20 LINES.
       01  DET TYPE DE LINE PLUS 1.
           03  COLUMN 1  PIC 9(3) SOURCE IS CA.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT PRT.
           INITIATE R1.
           GENERATE DET.
           TERMINATE R1.
           CLOSE PRT.
           STOP RUN.
