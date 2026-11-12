      *>--- conformance-case
      *> id: 1985-SR-XIII_3_17_3-2
      *> rules: [1985-SR-XIII.3.17.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.17.3 SR2 (THE SIGN CLAUSE): "The numeric data
      *> description entries to which the SIGN clause applies must be
      *> described, implicitly or explicitly, as USAGE IS DISPLAY."
      *> A refusal: the SIGN clause below applies to an entry whose
      *> usage is COMPUTATIONAL, and WiseOwl refuses it at 85 with
      *> COBOL2422, the diagnostic of THIS rule, at that entry's line.
      *> Every such entry in a report group also breaks §1985:XIII.3.21.3
      *> SR3 ("The USAGE clause for a report group item can specify only
      *> USAGE IS DISPLAY"), so COBOL2198 is reported there too: the
      *> harness pins a code at a line without requiring it to be the
      *> only one, and pins COBOL2422 so that a compiler which reported
      *> only the other rule's code would fail this test (kb/Work PB5373
      *> class 6). SEPARATE CHARACTER is written because §1985:XIII.3.17.3
      *> SR3 requires it in a report group entry, and PIC S9(3) because
      *> SR1 requires an S, so those two rules are kept.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SIGNUSG.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT-FILE ASSIGN TO PRINTER.
       DATA DIVISION.
       FILE SECTION.
       FD  PRT-FILE
           REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01  WS-N PIC S9(3) VALUE -5.
       REPORT SECTION.
       RD  RPT
           PAGE LIMIT IS 60.
       01  DTL TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN 1 PIC S9(3) COMPUTATIONAL
               SIGN IS LEADING SEPARATE CHARACTER
               SOURCE WS-N.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT-FILE.
           INITIATE RPT.
           GENERATE DTL.
           TERMINATE RPT.
           CLOSE PRT-FILE.
           STOP RUN.
