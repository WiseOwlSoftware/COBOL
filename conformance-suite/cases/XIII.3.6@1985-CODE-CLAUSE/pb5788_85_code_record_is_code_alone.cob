      *>--- conformance-case
      *> id: pb5788_85_code_record_is_code_alone
      *> rules: [1985-GR-XIII.3.6.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5788. §1985:XIII.3.6.4 GR2 and §2023:13.18.12.4 GR2: the
      *> CODE characters "are not included in the descriptions of the lines
      *> in the report, but are included in the logical record size". The
      *> FD's RECORD CONTAINS 2 makes every record 2 positions, and CODE "AB"
      *> takes both: the line has no position left, so each record is AB.
      *> A compiler that gives the line one position anyway writes ABW, a
      *> record of 3 characters: read back with RECORD CONTAINS 3, the third
      *> character of the first record is the W of the line, where it must be
      *> the first character of the host's line end. (The line end, one
      *> character or two, is the host's: the test reads only whether the
      *> third character is the W.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5788C.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO "PB5788C.DAT".
           SELECT IN-FILE ASSIGN TO "PB5788C.DAT".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           RECORD CONTAINS 2 CHARACTERS
           REPORT IS RPT.
       FD  IN-FILE
           RECORD CONTAINS 3 CHARACTERS.
       01  IN-REC.
           05  IN-CODE PIC XX.
           05  IN-THIRD PIC X.
       REPORT SECTION.
       RD  RPT
           CODE "AB".
       01  DET TYPE DETAIL LINE PLUS 1.
           05  COLUMN 1 PIC X(4) VALUE "WXYZ".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT.
           GENERATE DET.
           GENERATE DET.
           TERMINATE RPT.
           CLOSE RPT-FILE.
           OPEN INPUT IN-FILE.
           READ IN-FILE
               AT END DISPLAY "NO RECORD"
           END-READ.
           DISPLAY "[" IN-CODE "]".
           IF IN-THIRD = "W"
               DISPLAY "LONG"
           ELSE
               DISPLAY "SHORT"
           END-IF.
           CLOSE IN-FILE.
           STOP RUN.
