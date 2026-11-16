      *>--- conformance-case
      *> id: g85_bf18_02
      *> rules: [1985-SR-VII.4.2.3-1, 1985-GR-VII.4.2.4-8]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.4.2.3 SR1 - "The files referenced in the CLOSE
      *> statement need not all have the same organization or access."
      *> Each CLOSE below names two sequential files and a relative file
      *> in the random access mode; the program must translate at 85.
      *> §1985:VII.4.2.4 GR8 - a CLOSE naming several files is the same
      *> as a separate CLOSE for each, in the order written. A separate
      *> CLOSE updates that file's own I-O status (§1985:VII.4.2.4 GR4)
      *> and removes that file from the open mode (§1985:VII.4.2.4 GR7).
      *> So each status, set to 99 before the statement, reads 00 after
      *> it (§1985:VII.1.3.5 GRL2.1 item a), and each file can be OPENed
      *> again with 00, never the "OPEN statement is attempted for a
      *> file in the open mode" status 41 (§1985:VII.1.3.5 GRL2.4 item
      *> a). The records read back show each file was closed whole. Each
      *> leg fails if any one named file is skipped: its status stays
      *> 99, or its re-OPEN reports 41, or its record is lost.
      *> The order clause of §1985:VII.4.2.4 GR8: FA is never opened, so
      *> its CLOSE gets status 42, "A CLOSE statement is attempted for a
      *> file not in the open mode" (§1985:VII.1.3.5 GRL2.4 item b). 42
      *> is unsuccessful, so FA's USE AFTER STANDARD EXCEPTION procedure
      *> runs (§1985:VII.4.6.4 GR5) and shows the other files' status at
      *> that moment. "CLOSE FA S1" must close FA first: S1's status is
      *> still 99 in the procedure. "CLOSE S2 FA" must close S2 first:
      *> its status is already 00. A 4x status is critical, and the
      *> implementor chooses the action only after the USE procedure has
      *> run (§1985:VII.1.3.5), so the procedure's line is fixed by the
      *> 1985 text. Reaching the second leg relies on WiseOwl COBOL's
      *> choice to continue with the next statement
      *> (docs/conformance/3.5), and the second leg closes a different
      *> file so that nothing in it depends on how the first CLOSE
      *> ended.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF1802.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT S1 ASSIGN TO "G85BF1802A.DAT"
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS ST1.
           SELECT S2 ASSIGN TO "G85BF1802B.DAT"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS ST2.
           SELECT RL ASSIGN TO "G85BF1802C.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS RK
               FILE STATUS IS ST3.
           SELECT FA ASSIGN TO "G85BF1802F.DAT"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS STA.
       DATA DIVISION.
       FILE SECTION.
       FD  S1.
       01  S1-REC PIC X(4).
       FD  S2.
       01  S2-REC PIC X(4).
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
       PROCEDURE DIVISION.
       DECLARATIVES.
       FA-ERR SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON FA.
       FA-PARA.
           DISPLAY "USE-A " STA
               " S1=" ST1
               " S2=" ST2.
       END DECLARATIVES.
       MAIN-SECT SECTION.
       MAIN-PARA.
           OPEN OUTPUT S1 S2 RL.
           MOVE "AAAA" TO S1-REC.
           WRITE S1-REC.
           MOVE "BBBB" TO S2-REC.
           WRITE S2-REC.
           MOVE 1 TO RK.
           MOVE "CCCC" TO RL-REC.
           WRITE RL-REC INVALID KEY DISPLAY "WRITE INVALID".
           MOVE "99" TO ST1 ST2 ST3.
           CLOSE S1 RL S2.
           DISPLAY "CLOSE-1 " ST1 " " ST3 " " ST2.
           OPEN INPUT S1 RL S2.
           DISPLAY "REOPEN " ST1 " " ST3 " " ST2.
           READ S1 AT END DISPLAY "S1 EOF".
           DISPLAY "S1 " S1-REC.
           READ S2 AT END DISPLAY "S2 EOF".
           DISPLAY "S2 " S2-REC.
           MOVE 1 TO RK.
           READ RL INVALID KEY DISPLAY "RL INVALID".
           DISPLAY "RL " RL-REC.
           MOVE "99" TO ST1 ST2 ST3.
           CLOSE RL S2 S1.
           DISPLAY "CLOSE-2 " ST3 " " ST2 " " ST1.
           OPEN INPUT S2 RL S1.
           DISPLAY "REOPEN-2 " ST2 " " ST3 " " ST1.
           CLOSE S2 RL S1.
           DISPLAY "CLOSE-3 " ST2 " " ST3 " " ST1.
           OPEN INPUT S1.
           OPEN INPUT S2.
           MOVE "99" TO STA ST1 ST2.
           CLOSE FA S1.
           MOVE "99" TO STA ST1 ST2.
           CLOSE S2 FA.
           STOP RUN.
