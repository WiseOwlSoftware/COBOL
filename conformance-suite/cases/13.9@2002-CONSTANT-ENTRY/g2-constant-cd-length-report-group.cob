      *>--- conformance-case
      *> id: g2-constant-cd-length-report-group
      *> rules: [2002-SR-13.9.2-11]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.9.2 SR11 - "Data-name-1 and data-name-2, if defined in
      *> the report section, shall reference elementary report items".
      *> KG is a constant entry under a CD (§2002:13.1.1) whose LENGTH
      *> OF operand DL is a report group (it has the subordinate entry
      *> EL), not an elementary report item; the program is refused at
      *> the KG entry (it fails if the group's length is taken and the
      *> program compiles).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CON06.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPF ASSIGN TO "G2CON06.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  RPF REPORT IS RP.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  KG CONSTANT AS LENGTH OF DL.
       01  CDREC PIC X(10).
       REPORT SECTION.
       RD  RP PAGE LIMIT IS 10 LINES.
       01  DL TYPE DE LINE PLUS 1.
           03  EL COLUMN 1 PIC X(5) VALUE "ABCDE".
       PROCEDURE DIVISION.
           DISPLAY KG
           STOP RUN.
