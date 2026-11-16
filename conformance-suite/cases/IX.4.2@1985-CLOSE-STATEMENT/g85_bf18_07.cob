      *>--- conformance-case
      *> id: g85_bf18_07
      *> rules: [1985-SR-IX.4.2.3-1, 1985-GR-IX.4.2.4-7]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.4.2.3 SR1 - "The files referenced in the CLOSE
      *> statement need not all have the same organization or access."
      *> Each CLOSE below names indexed files in the dynamic, random and
      *> sequential access modes, a relative file and a sequential file;
      *> the program must translate at 85.
      *> §1985:IX.4.2.4 GR7 - a CLOSE naming several files is the same
      *> as a separate CLOSE for each, in the order written. A separate
      *> CLOSE updates that file's own I-O status (§1985:IX.4.2.4 GR3)
      *> and removes that file from the open mode (§1985:IX.4.2.4 GR6).
      *> So each status, set to 99 before the statement, reads 00 after
      *> it (§1985:IX.1.3.4 GRL2.1 item a), and each file can be OPENed
      *> again with 00, never the "OPEN statement is attempted for a
      *> file in the open mode" status 41. The records read back show
      *> each file was closed whole. Each leg fails if any one named
      *> file is skipped: its status stays 99, its re-OPEN reports 41,
      *> or its record is lost.
      *> The order clause of §1985:IX.4.2.4 GR7: FA is never opened, so
      *> its CLOSE gets status 42, "A CLOSE statement is attempted for a
      *> file not in the open mode" (§1985:IX.1.3.4 GRL2.5 item b). 42
      *> is unsuccessful, so FA's USE AFTER STANDARD EXCEPTION procedure
      *> runs (§1985:IX.4.8.4 GR5) and shows the other files' status at
      *> that moment. "CLOSE FA XD" must close FA first: XD's status is
      *> still 99 in the procedure. "CLOSE XR FA" must close XR first:
      *> its status is already 00. A 4x status is critical, and the
      *> implementor chooses the action only after the USE procedure has
      *> run (§1985:IX.1.3.4), so the procedure's line is fixed by the
      *> 1985 text. Reaching the second leg relies on WiseOwl COBOL's
      *> choice to continue with the next statement
      *> (docs/conformance/3.5), and the second leg closes a different
      *> file so that nothing in it depends on how the first CLOSE
      *> ended.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF1807.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT XD ASSIGN TO "G85BF1807A.DAT"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS XD-KEY
               FILE STATUS IS ST1.
           SELECT XR ASSIGN TO "G85BF1807B.DAT"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS RANDOM
               RECORD KEY IS XR-KEY
               FILE STATUS IS ST2.
           SELECT XS ASSIGN TO "G85BF1807C.DAT"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS XS-KEY
               FILE STATUS IS ST3.
           SELECT RL ASSIGN TO "G85BF1807D.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS ST4.
           SELECT SQ ASSIGN TO "G85BF1807E.DAT"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS ST5.
           SELECT FA ASSIGN TO "G85BF1807F.DAT"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS FA-KEY
               FILE STATUS IS STA.
       DATA DIVISION.
       FILE SECTION.
       FD  XD.
       01  XD-REC.
           05  XD-KEY PIC XX.
           05  XD-VAL PIC X(4).
       FD  XR.
       01  XR-REC.
           05  XR-KEY PIC XX.
           05  XR-VAL PIC X(4).
       FD  XS.
       01  XS-REC.
           05  XS-KEY PIC XX.
           05  XS-VAL PIC X(4).
       FD  RL.
       01  RL-REC PIC X(4).
       FD  SQ.
       01  SQ-REC PIC X(4).
       FD  FA.
       01  FA-REC.
           05  FA-KEY PIC XX.
           05  FA-VAL PIC X(4).
       WORKING-STORAGE SECTION.
       01  ST1 PIC XX.
       01  ST2 PIC XX.
       01  ST3 PIC XX.
       01  ST4 PIC XX.
       01  ST5 PIC XX.
       01  STA PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       FA-ERR SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON FA.
       FA-PARA.
           DISPLAY "USE-A " STA
               " XD=" ST1
               " XR=" ST2.
       END DECLARATIVES.
       MAIN-SECT SECTION.
       MAIN-PARA.
           OPEN OUTPUT XD XR XS RL SQ.
           MOVE "01AAAA" TO XD-REC.
           WRITE XD-REC INVALID KEY DISPLAY "XD INVALID".
           MOVE "02BBBB" TO XR-REC.
           WRITE XR-REC INVALID KEY DISPLAY "XR INVALID".
           MOVE "03CCCC" TO XS-REC.
           WRITE XS-REC INVALID KEY DISPLAY "XS INVALID".
           MOVE "DDDD" TO RL-REC.
           WRITE RL-REC INVALID KEY DISPLAY "RL INVALID".
           MOVE "EEEE" TO SQ-REC.
           WRITE SQ-REC.
           MOVE "99" TO ST1 ST2 ST3 ST4 ST5.
           CLOSE SQ XS XD RL XR.
           DISPLAY "CLOSE-1 " ST5 " " ST3 " " ST1 " " ST4 " " ST2.
           OPEN INPUT XD XR XS RL SQ.
           DISPLAY "REOPEN " ST1 " " ST2 " " ST3 " " ST4 " " ST5.
           MOVE "01" TO XD-KEY.
           READ XD INVALID KEY DISPLAY "XD MISSING".
           DISPLAY "XD " XD-REC.
           MOVE "02" TO XR-KEY.
           READ XR INVALID KEY DISPLAY "XR MISSING".
           DISPLAY "XR " XR-REC.
           READ XS NEXT RECORD AT END DISPLAY "XS EOF".
           DISPLAY "XS " XS-REC.
           READ RL NEXT RECORD AT END DISPLAY "RL EOF".
           DISPLAY "RL " RL-REC.
           READ SQ AT END DISPLAY "SQ EOF".
           DISPLAY "SQ " SQ-REC.
           MOVE "99" TO ST1 ST2 ST3 ST4 ST5.
           CLOSE XR RL XD XS SQ.
           DISPLAY "CLOSE-2 " ST2 " " ST4 " " ST1 " " ST3 " " ST5.
           OPEN I-O XD XR XS RL.
           DISPLAY "REOPEN-2 " ST1 " " ST2 " " ST3 " " ST4.
           CLOSE XD XR XS RL.
           DISPLAY "CLOSE-3 " ST1 " " ST2 " " ST3 " " ST4.
           OPEN INPUT XD.
           OPEN INPUT XR.
           MOVE "99" TO STA ST1 ST2.
           CLOSE FA XD.
           MOVE "99" TO STA ST1 ST2.
           CLOSE XR FA.
           STOP RUN.
