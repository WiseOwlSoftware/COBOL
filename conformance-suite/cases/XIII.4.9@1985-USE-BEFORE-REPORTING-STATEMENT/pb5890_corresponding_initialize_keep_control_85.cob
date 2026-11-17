      *>--- conformance-case
      *> id: pb5890_corresponding_initialize_keep_control_85
      *> rules: [1985-SR-XIII.4.9.3-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.4.9.3 SR4: "A USE BEFORE REPORTING procedure must
      *> not alter the value of any control data item." CTL, the
      *> control data item of R1, is the first item of GRP, and no
      *> statement of the procedure stores into it (kb/Work PB5890):
      *> MOVE, ADD and SUBTRACT CORRESPONDING store only the pairs
      *> OTH and OTA (§1985:VI.6.19.4 GR1: "selected items within
      *> identifier-1 are moved to selected items within
      *> identifier-2"); SRC has no CTL. INITIALIZE GRP REPLACING
      *> NUMERIC DATA initializes an item "only if it belongs to the
      *> category specified" (§1985:VI.6.17.4 GR2), and CTL is
      *> alphanumeric. A compiler that refuses lines 55 to 59 fails.
      *> Expected, per GENERATE: CNT + 1; MOVE CORR sets OTH 0005 and
      *> OTA QQQ; ADD CORR adds 5 to the numeric pair (0010); SUBTRACT
      *> CORR takes it back (0005); INITIALIZE BY 7 then BY 0 leaves
      *> OTH 0000; CTL keeps AA, then BB after the MOVE of M-1. The
      *> procedure "is invoked just before the named report group is
      *> produced" (§1985:XIII.4.9.4 GR1), once per GENERATE DE1, so
      *> CNT ends at 2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5890K85.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "PB5890K85.RPT".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT REPORT IS R1.
       WORKING-STORAGE SECTION.
       01  GRP.
           05  CTL   PIC X(2) VALUE "AA".
           05  OTH   PIC 9(4) VALUE 0.
           05  OTA   PIC X(3) VALUE "ZZZ".
       01  SRC.
           05  OTH   PIC 9(4) VALUE 5.
           05  OTA   PIC X(3) VALUE "QQQ".
       01  CNT   PIC 9 VALUE 0.
       REPORT SECTION.
       RD  R1 CONTROL IS CTL PAGE LIMIT 20.
       01  DE1 TYPE DE LINE PLUS 1.
           03  COLUMN 1 PIC X(2) SOURCE CTL.
           03  COLUMN 4 PIC 9(4) SOURCE OTH OF GRP.
       PROCEDURE DIVISION.
       DECLARATIVES.
       U1 SECTION.
           USE BEFORE REPORTING DE1.
       U1-P.
           ADD 1 TO CNT.
           MOVE CORRESPONDING SRC TO GRP.
           ADD CORRESPONDING SRC TO GRP.
           SUBTRACT CORRESPONDING SRC FROM GRP.
           INITIALIZE GRP REPLACING NUMERIC DATA BY 7.
           INITIALIZE GRP REPLACING NUMERIC DATA BY 0.
           DISPLAY "USE " CTL " " OTH OF GRP " " OTA OF GRP.
       END DECLARATIVES.
       MAIN SECTION.
       M-1.
           OPEN OUTPUT RPT.
           INITIATE R1.
           GENERATE DE1.
           MOVE "BB" TO CTL.
           GENERATE DE1.
           TERMINATE R1.
           CLOSE RPT.
           DISPLAY "END " CNT " " CTL.
           STOP RUN.
