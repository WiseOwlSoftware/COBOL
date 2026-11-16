      *>--- conformance-case
      *> id: g85_bf18_09
      *> rules: [1985-SR-XIII.4.2.3-1, 1985-GR-XIII.4.2.4-7]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.4.2.3 SR1 - "The files referenced in the CLOSE
      *> statement need not all have the same organization or access."
      *> The first CLOSE names two report files and a relative file in
      *> the random access mode; the program must translate at 85.
      *> §1985:XIII.4.2.4 GR7 - a CLOSE naming several files is the same
      *> as a separate CLOSE for each, in the order written. A separate
      *> CLOSE of a report file updates its own I-O status
      *> (§1985:XIII.4.2.4 GR4) and removes it from the open mode
      *> (§1985:XIII.4.2.4 GR6); the relative file's CLOSE does the same
      *> (§1985:VIII.4.2.4 GR3, GR6). So each status, set to 99 before
      *> the statement, reads 00 after it (§1985:VII.1.3.5 GRL2.1 item
      *> a, §1985:VIII.1.3.4 GRL2.1 item a), and each file can be OPENed
      *> again with 00, never the "OPEN statement is attempted for a
      *> file in the open mode" status 41. Each report was ended by
      *> TERMINATE before the CLOSE (§1985:XIII.4.2.4 GR5), and each can
      *> be INITIATEd and GENERATEd again after the re-OPEN. Each leg
      *> fails if any one named file is skipped: its status stays 99, or
      *> its re-OPEN reports 41.
      *> The order clause of §1985:XIII.4.2.4 GR7: the relative file FA
      *> is never opened, so its CLOSE gets status 42, "A CLOSE
      *> statement is attempted for a file not in the open mode"
      *> (§1985:VIII.1.3.4 GRL2.5 item b). 42 is unsuccessful, so FA's
      *> USE AFTER STANDARD EXCEPTION procedure runs
      *> (§1985:VIII.4.8.4 GR5) and shows the other files' status at
      *> that moment. "CLOSE FA PA" must close FA first: PA's status is
      *> still 99 in the procedure. "CLOSE PB FA" must close PB first:
      *> its status is already 00. A 4x status is critical, and the
      *> implementor chooses the action only after the USE procedure has
      *> run (§1985:VIII.1.3.4), so the procedure's line is fixed by the
      *> 1985 text. Reaching the second leg relies on WiseOwl COBOL's
      *> choice to continue with the next statement
      *> (docs/conformance/3.5), and the second leg closes a different
      *> file so that nothing in it depends on how the first CLOSE
      *> ended.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF1809.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PA ASSIGN TO "G85BF1809A.TXT"
               FILE STATUS IS ST1.
           SELECT PB ASSIGN TO "G85BF1809B.TXT"
               FILE STATUS IS ST2.
           SELECT RL ASSIGN TO "G85BF1809C.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS RK
               FILE STATUS IS ST3.
           SELECT FA ASSIGN TO "G85BF1809F.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS STA.
       DATA DIVISION.
       FILE SECTION.
       FD  PA REPORT IS REP-A.
       FD  PB REPORT IS REP-B.
       FD  RL.
       01  RL-REC PIC X(4).
       FD  FA.
       01  FA-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01  ST1 PIC XX.
       01  ST2 PIC XX.
       01  ST3 PIC XX.
       01  STA PIC XX.
       01  RK PIC 9(4).
       REPORT SECTION.
       RD  REP-A.
       01  DTL-A TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN 1 PIC X(5) VALUE "ALPHA".
       RD  REP-B.
       01  DTL-B TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN 1 PIC X(4) VALUE "BETA".
       PROCEDURE DIVISION.
       DECLARATIVES.
       FA-ERR SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON FA.
       FA-PARA.
           DISPLAY "USE-A " STA
               " PA=" ST1
               " PB=" ST2.
       END DECLARATIVES.
       MAIN-SECT SECTION.
       MAIN-PARA.
           OPEN OUTPUT PA PB RL.
           INITIATE REP-A REP-B.
           GENERATE DTL-A.
           GENERATE DTL-B.
           TERMINATE REP-A REP-B.
           MOVE 1 TO RK.
           MOVE "CCCC" TO RL-REC.
           WRITE RL-REC INVALID KEY DISPLAY "RL INVALID".
           MOVE "99" TO ST1 ST2 ST3.
           CLOSE PB RL PA.
           DISPLAY "CLOSE-1 " ST2 " " ST3 " " ST1.
           OPEN OUTPUT PA PB.
           DISPLAY "REOPEN " ST1 " " ST2.
           OPEN INPUT RL.
           DISPLAY "REOPEN-RL " ST3.
           MOVE 1 TO RK.
           READ RL INVALID KEY DISPLAY "RL MISSING".
           DISPLAY "RL " RL-REC.
           INITIATE REP-A REP-B.
           GENERATE DTL-B.
           GENERATE DTL-A.
           TERMINATE REP-B REP-A.
           MOVE "99" TO ST1 ST2 ST3.
           CLOSE PA PB RL.
           DISPLAY "CLOSE-2 " ST1 " " ST2 " " ST3.
           OPEN OUTPUT PA.
           OPEN OUTPUT PB.
           MOVE "99" TO STA ST1 ST2.
           CLOSE FA PA.
           MOVE "99" TO STA ST1 ST2.
           CLOSE PB FA.
           STOP RUN.
