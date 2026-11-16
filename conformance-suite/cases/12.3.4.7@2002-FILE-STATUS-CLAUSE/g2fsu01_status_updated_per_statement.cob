      *>--- conformance-case
      *> id: g2fsu01_status_updated_per_statement
      *> rules: [2002-GR-12.3.4.7.3-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:12.3.4.7.3 GR1 - "the data item referenced by data-name-1
      *> is updated to contain the value of the I-O status for the file
      *> connector referenced by the subject of the entry when the I-O
      *> status associated with that file connector is updated as a
      *> result of an input-output statement." NOTE: "In the case where a
      *> file-name is global and data-name-1 is not, data-name-1 is
      *> updated by references to file-name in contained programs".
      *>   cite.py --check "§2002:12.3.4.7.3 GR1" -> OK
      *> The values are the 2002 list (cite.py OK for each clause):
      *>   00 §2002:9.1.12.1 item 1; 10 §2002:9.1.12.3 item 1 a);
      *>   38 §2002:9.1.12.5 item 6 "an OPEN statement is attempted on a
      *>   file connector previously closed with lock"; 46 §2002:9.1.12.6
      *>   item 6 b) (the preceding READ was unsuccessful); 42
      *>   §2002:9.1.12.6 item 2 (CLOSE of a connector not open).
      *> EC-I-O checking is not enabled, so no exception links to the
      *> status (§2002:14.5.12.1) and  each statement continues.
      *> DERIVATION OF THE EXPECTED OUTPUT (LFS reset to XX before each
      *> statement whose update is pinned, so a missing update shows XX):
      *>   OPEN OUTPUT GF                        => OPEN OUTPUT 00
      *>   OPEN INPUT, READ ONE                  => READ 1 00 ONE
      *>   contained G2FSU01I READs GF at end; LFS is the outer
      *>   program's local item                  => AFTER INNER 10
      *>   READ after the unsuccessful READ      => READ 3 46
      *>   CLOSE                                 => CLOSE 00
      *>   CLOSE again (not open)                => CLOSE AGAIN 42
      *>   LF: CLOSE WITH LOCK, then OPEN INPUT  => CLOSE LOCK 00 /
      *>                                            REOPEN 38
      *> A leg fails if a status is not stored (XX), if the contained
      *> program's READ does not update the outer local item, or if a
      *> 2023 list is applied (no 38 at 2002 would be 41 or 00).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2FSU01.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT GF ASSIGN TO "G2FSU01G.DAT"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS LFS.
           SELECT LF ASSIGN TO "G2FSU01L.DAT"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS KFS.
       DATA DIVISION.
       FILE SECTION.
       FD  GF IS GLOBAL.
       01  GR          PIC X(4).
       FD  LF.
       01  LR          PIC X(4).
       WORKING-STORAGE SECTION.
       01  LFS         PIC XX.
       01  KFS         PIC XX.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE "XX" TO LFS.
           OPEN OUTPUT GF.
           DISPLAY "OPEN OUTPUT " LFS.
           MOVE "ONE " TO GR. WRITE GR.
           CLOSE GF.
           OPEN INPUT GF.
           MOVE "XX" TO LFS.
           READ GF AT END CONTINUE END-READ.
           DISPLAY "READ 1 " LFS " " GR.
           MOVE "XX" TO LFS.
           CALL "G2FSU01I".
           DISPLAY "AFTER INNER " LFS.
           MOVE "XX" TO LFS.
           READ GF AT END CONTINUE END-READ.
           DISPLAY "READ 3 " LFS.
           MOVE "XX" TO LFS.
           CLOSE GF.
           DISPLAY "CLOSE " LFS.
           MOVE "XX" TO LFS.
           CLOSE GF.
           DISPLAY "CLOSE AGAIN " LFS.
           OPEN OUTPUT LF.
           MOVE "AAAA" TO LR. WRITE LR.
           MOVE "XX" TO KFS.
           CLOSE LF WITH LOCK.
           DISPLAY "CLOSE LOCK " KFS.
           MOVE "XX" TO KFS.
           OPEN INPUT LF.
           DISPLAY "REOPEN " KFS.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2FSU01I.
       PROCEDURE DIVISION.
       MAIN-P.
           READ GF AT END CONTINUE END-READ.
           GOBACK.
       END PROGRAM G2FSU01I.
       END PROGRAM G2FSU01.
