      *>--- conformance-case
      *> id: g85_bf12_04
      *> rules: [1985-GR-XIII.3.21.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.21.4 GR2 - in the Report Writer module the USAGE
      *> clause "specifies the manner in which a data item is
      *> represented in the storage of a computer. It does not affect
      *> the use of the data item".
      *> The detail group carries USAGE IS DISPLAY at group level
      *> (§1985:XIII.3.21.4 GR1 applies it to each elementary item);
      *> its three printable items are written with USAGE IS DISPLAY,
      *> with USAGE DISPLAY and with no USAGE clause (implicitly
      *> DISPLAY, §1985:XIII.3.21.4 GR4; §1985:XIII.3.21.3 SR3 allows
      *> only DISPLAY). Their SOURCE items hold the same value, -1234,
      *> in BINARY, PACKED-DECIMAL and DISPLAY usage.
      *> DERIVED: neither the report items' written-or-implied usage nor
      *> the SOURCE items' usage affects the use, so each PIC -9999
      *> column prints "-1234": columns 1, 7 and 13 give the line
      *> "-1234 -1234 -1234", read back from the report file.
      *> A column that differs shows a usage changing the item's use.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF1204.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO "g85bf1204.rpt".
           SELECT IN-FILE ASSIGN TO "g85bf1204.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT.
       FD  IN-FILE.
       01  IN-REC  PIC X(17).
       WORKING-STORAGE SECTION.
       01  WB  PIC S9(4) USAGE IS BINARY VALUE -1234.
       01  WP  PIC S9(4) USAGE IS PACKED-DECIMAL VALUE -1234.
       01  WD  PIC S9(4) VALUE -1234.
       REPORT SECTION.
       RD  RPT
           PAGE LIMIT IS 10 LINES.
       01  DETL TYPE IS DETAIL LINE NUMBER IS 1 USAGE IS DISPLAY.
           05  COLUMN 1  PIC -9999 USAGE IS DISPLAY SOURCE WB.
           05  COLUMN 7  PIC -9999 USAGE DISPLAY SOURCE WP.
           05  COLUMN 13 PIC -9999 SOURCE WD.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT.
           GENERATE DETL.
           TERMINATE RPT.
           CLOSE RPT-FILE.
           OPEN INPUT IN-FILE.
           READ IN-FILE AT END MOVE "NO LINE" TO IN-REC.
           DISPLAY "LINE=[" IN-REC "]".
           CLOSE IN-FILE.
           STOP RUN.
