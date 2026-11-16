      *>--- conformance-case
      *> id: g2call12_returning_report_refused
      *> rules: [2002-SR-14.8.4.2-7]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.4.2 SR7 - "Identifier-3 shall reference a data item
      *> defined in the file, working-storage, local-storage, linkage, or
      *> communication section." SC is the sum counter of a report
      *> section entry, which "behaves as a data item"
      *> (§2002:13.16.52.3 GR1) of the report section, a section SR7
      *> does not admit, so RETURNING SC shall be refused at the CALL
      *> line. The admitted sections are g2call08_operand_sections.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2C12M.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPF ASSIGN TO "R1.RPT".
       DATA DIVISION.
       FILE SECTION.
       FD RPF REPORT IS RR.
       WORKING-STORAGE SECTION.
       01 W PIC 9(4) VALUE 1.
       01 WX PIC X(4) VALUE "AAAA".
       REPORT SECTION.
       RD RR CONTROL FINAL PAGE LIMIT 20.
       01 TYPE DETAIL LINE 1.
          05 COLUMN 1 PIC 9(4) SOURCE W.
       01 TYPE CONTROL FOOTING FINAL LINE PLUS 1.
          05 SC COLUMN 1 PIC 9(4) SUM W.
       PROCEDURE DIVISION.
       MAIN.
           CALL "G2C12S" USING WX RETURNING SC
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2C12S.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LX PIC X(4).
       01 LR PIC 9(4).
       PROCEDURE DIVISION USING LX RETURNING LR.
       S1.
           MOVE 5 TO LR
           GOBACK.
       END PROGRAM G2C12S.
       END PROGRAM G2C12M.
