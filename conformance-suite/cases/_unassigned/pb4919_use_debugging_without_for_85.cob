      *>--- conformance-case
      *> id: pb4919_use_debugging_without_for_85
      *> rules: []
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4919 - X3.23-1985 USE FOR DEBUGGING with its optional
      *> words omitted. The printed general format (§1985:XV.3.2.2,
      *> page XV-5) underlines USE and DEBUGGING only: FOR and ON are
      *> optional words, so USE DEBUGGING ALL PROCEDURES and USE
      *> DEBUGGING ON W-A are the same statement as their spelled-out
      *> forms. ISO/IEC 1989:2023 prints no debug format; the 1985
      *> page is the format the clause is read from.
      *> Why it can fail: the grammar demanded FOR, and refused both
      *> USE statements with COBOL5506 ("expected ... FOR").
      *> Expected output: SOURCE-COMPUTER carries no WITH DEBUGGING
      *> MODE, so each debugging section is compiled as if it were
      *> comment lines and never runs (VCR row 7.17's '85 posture):
      *> HITS stays 0 and the main body's MOVE is all that changes
      *> the data.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P4919DB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-A   PIC 9(3) VALUE 0.
       01  HITS  PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       DBG-1 SECTION.
           USE DEBUGGING ALL PROCEDURES.
       DBG-1-PARA.
           ADD 1 TO HITS.
       DBG-2 SECTION.
           USE DEBUGGING ON W-A.
       DBG-2-PARA.
           ADD 1 TO HITS.
       END DECLARATIVES.
       MAIN SECTION.
       P0.
           MOVE 7 TO W-A.
           DISPLAY "W-A=" W-A " HITS=" HITS.
           STOP RUN.
