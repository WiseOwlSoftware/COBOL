      *>--- conformance-case
      *> id: g85_bf18_05
      *> rules: [1985-SR-VIII.4.2.3-1, 1985-GR-VIII.4.2.4-7]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.4.2.3 SR1 - "The files referenced in the CLOSE
      *> statement need not all have the same organization or access."
      *> Each CLOSE below names relative files in the sequential, random
      *> and dynamic access modes and a sequential file; the program
      *> must translate at 85.
      *> §1985:VIII.4.2.4 GR7 - a CLOSE naming several files is the same
      *> as a separate CLOSE for each, in the order written. A separate
      *> CLOSE updates that file's own I-O status (§1985:VIII.4.2.4 GR3)
      *> and removes that file from the open mode (§1985:VIII.4.2.4
      *> GR6). So each status, set to 99 before the statement, reads 00
      *> after it (§1985:VIII.1.3.4 GRL2.1 item a), and each file can be
      *> OPENed again with 00, never the "OPEN statement is attempted
      *> for a file in the open mode" status 41 (§1985:VIII.1.3.4 GRL2.5
      *> item a). The records read back show each file was closed whole.
      *> Each leg fails if any one named file is skipped: its status
      *> stays 99, or its re-OPEN reports 41, or its record is lost.
      *> The order clause of §1985:VIII.4.2.4 GR7: FA is never opened,
      *> so its CLOSE gets status 42, "A CLOSE statement is attempted
      *> for a file not in the open mode"
      *> (§1985:VIII.1.3.4 GRL2.5 item b). 42 is unsuccessful, so FA's
      *> USE AFTER STANDARD EXCEPTION procedure runs
      *> (§1985:VIII.4.8.4 GR5) and shows the other files' status at
      *> that moment. "CLOSE FA FRR" must close FA first: FRR's status
      *> is still 99 in the procedure. "CLOSE FRD FA" must close FRD
      *> first: its status is already 00. A 4x status is critical, and
      *> the implementor chooses the action only after the USE procedure
      *> has run (§1985:VIII.1.3.4), so the procedure's line is fixed by
      *> the 1985 text. Reaching the second leg relies on WiseOwl
      *> COBOL's choice to continue with the next statement
      *> (docs/conformance/3.5), and the second leg closes a different
      *> file so that nothing in it depends on how the first CLOSE
      *> ended.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF1805.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FRS ASSIGN TO "G85BF1805A.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS ST1.
           SELECT FRR ASSIGN TO "G85BF1805B.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS RR-KEY
               FILE STATUS IS ST2.
           SELECT FRD ASSIGN TO "G85BF1805C.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS RD-KEY
               FILE STATUS IS ST3.
           SELECT SQ ASSIGN TO "G85BF1805D.DAT"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS ST4.
           SELECT FA ASSIGN TO "G85BF1805F.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS STA.
       DATA DIVISION.
       FILE SECTION.
       FD  FRS.
       01  RS-REC PIC X(4).
       FD  FRR.
       01  RR-REC PIC X(4).
       FD  FRD.
       01  RD-REC PIC X(4).
       FD  SQ.
       01  SQ-REC PIC X(4).
       FD  FA.
       01  FA-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01  ST1 PIC XX.
       01  ST2 PIC XX.
       01  ST3 PIC XX.
       01  ST4 PIC XX.
       01  STA PIC XX.
       01  RR-KEY PIC 9(4).
       01  RD-KEY PIC 9(4).
       PROCEDURE DIVISION.
       DECLARATIVES.
       FA-ERR SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON FA.
       FA-PARA.
           DISPLAY "USE-A " STA
               " FRR=" ST2
               " FRD=" ST3.
       END DECLARATIVES.
       MAIN-SECT SECTION.
       MAIN-PARA.
           OPEN OUTPUT FRS FRR FRD SQ.
           MOVE "AAAA" TO RS-REC.
           WRITE RS-REC INVALID KEY DISPLAY "FRS INVALID".
           MOVE 3 TO RR-KEY.
           MOVE "BBBB" TO RR-REC.
           WRITE RR-REC INVALID KEY DISPLAY "FRR INVALID".
           MOVE 5 TO RD-KEY.
           MOVE "CCCC" TO RD-REC.
           WRITE RD-REC INVALID KEY DISPLAY "FRD INVALID".
           MOVE "DDDD" TO SQ-REC.
           WRITE SQ-REC.
           MOVE "99" TO ST1 ST2 ST3 ST4.
           CLOSE FRD SQ FRS FRR.
           DISPLAY "CLOSE-1 " ST3 " " ST4 " " ST1 " " ST2.
           OPEN INPUT FRS FRR FRD SQ.
           DISPLAY "REOPEN " ST1 " " ST2 " " ST3 " " ST4.
           READ FRS NEXT RECORD AT END DISPLAY "FRS EOF".
           DISPLAY "FRS " RS-REC.
           MOVE 3 TO RR-KEY.
           READ FRR INVALID KEY DISPLAY "FRR MISSING".
           DISPLAY "FRR " RR-REC.
           MOVE 5 TO RD-KEY.
           READ FRD INVALID KEY DISPLAY "FRD MISSING".
           DISPLAY "FRD " RD-REC.
           READ SQ AT END DISPLAY "SQ EOF".
           DISPLAY "SQ " SQ-REC.
           MOVE "99" TO ST1 ST2 ST3 ST4.
           CLOSE SQ FRR FRD FRS.
           DISPLAY "CLOSE-2 " ST4 " " ST2 " " ST3 " " ST1.
           OPEN I-O FRS FRR FRD.
           DISPLAY "REOPEN-2 " ST1 " " ST2 " " ST3.
           CLOSE FRS FRR FRD.
           DISPLAY "CLOSE-3 " ST1 " " ST2 " " ST3.
           OPEN INPUT FRR.
           OPEN INPUT FRD.
           MOVE "99" TO STA ST2 ST3.
           CLOSE FA FRR.
           MOVE "99" TO STA ST2 ST3.
           CLOSE FRD FA.
           STOP RUN.
