      *>--- conformance-case
      *> id: g85_f16_07
      *> rules: [1985-GR-IX.4.6.4-13, 1985-GR-IX.4.6.4-16,
      *>    1985-GR-IX.4.6.4-17]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.4.6.4 GR13 - sequential access: the record replaced
      *> is the one whose prime key equals the last record read. S1
      *> reads A001, changes its data and REWRITEs: 00, and the file
      *> then holds A001NEW1 (CHK lines).
      *> §1985:IX.4.6.4 GR16 - the invalid key condition exists:
      *>  S2  a. sequential access, the prime key (B002 read) changed
      *>      to C003 before the REWRITE (21);
      *>  D1  b. dynamic access, the prime key K009 is in no record
      *>      of the file (23);
      *>  D2  c. the alternate key X001, DUPLICATES not specified,
      *>      equals that of record K001 (22).
      *>  D3  the contrast: K002 with a new alternate key, 00.
      *> §1985:IX.4.6.4 GR17 - on each invalid key the REWRITE is
      *> unsuccessful, nothing is updated (CHK: B002OLD2 and
      *> K002X002D2 are read back unchanged as long as D3 has not
      *> run, no C003 or K009 record exists), the record area keeps
      *> what the program put there (AREA lines) and the I-O status
      *> shows the cause; the INVALID KEY branch runs (IK).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F1607.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SQF ASSIGN TO "g85f1607s.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS SQ-KEY
               FILE STATUS IS FS.
           SELECT DYNF ASSIGN TO "g85f1607d.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS DY-KEY
               ALTERNATE RECORD KEY IS DY-ALT
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  SQF
           LABEL RECORDS ARE STANDARD.
       01  SQREC.
           05 SQ-KEY           PIC X(4).
           05 SQ-DATA          PIC X(6).
       FD  DYNF
           LABEL RECORDS ARE STANDARD.
       01  DYREC.
           05 DY-KEY           PIC X(4).
           05 DY-ALT           PIC X(4).
           05 DY-DATA          PIC X(4).
       WORKING-STORAGE SECTION.
       01  FS                  PIC XX.
       01  BR                  PIC X(3).
       PROCEDURE DIVISION.
       MAIN-LOGIC SECTION.
       M-LOAD.
           OPEN OUTPUT SQF.
           MOVE "A001OLD1" TO SQREC.
           WRITE SQREC INVALID KEY DISPLAY "LOAD IK " FS.
           MOVE "B002OLD2" TO SQREC.
           WRITE SQREC INVALID KEY DISPLAY "LOAD IK " FS.
           CLOSE SQF.
           OPEN OUTPUT DYNF.
           MOVE "K001X001D1" TO DYREC.
           WRITE DYREC INVALID KEY DISPLAY "LOAD IK " FS.
           MOVE "K002X002D2" TO DYREC.
           WRITE DYREC INVALID KEY DISPLAY "LOAD IK " FS.
           CLOSE DYNF.
       M-SEQ.
           OPEN I-O SQF.
           READ SQF AT END DISPLAY "UNEXPECTED AT END".
           MOVE "NEW1" TO SQ-DATA.
           MOVE "NO" TO BR.
           REWRITE SQREC INVALID KEY MOVE "IK" TO BR
               NOT INVALID KEY MOVE "NIK" TO BR
           END-REWRITE.
           DISPLAY "S1 " FS " " BR.
           READ SQF AT END DISPLAY "UNEXPECTED AT END".
           DISPLAY "READ " FS " " SQREC.
           MOVE "C003" TO SQ-KEY.
           MOVE "BAD2" TO SQ-DATA.
           MOVE "NO" TO BR.
           REWRITE SQREC INVALID KEY MOVE "IK" TO BR
               NOT INVALID KEY MOVE "NIK" TO BR
           END-REWRITE.
           DISPLAY "S2 " FS " " BR.
           DISPLAY "AREA " SQREC.
           CLOSE SQF.
           OPEN INPUT SQF.
           PERFORM 3 TIMES
               READ SQF AT END DISPLAY "CHK AT END " FS
                   NOT AT END DISPLAY "CHK " SQREC
               END-READ
           END-PERFORM.
           CLOSE SQF.
       M-DYN.
           OPEN I-O DYNF.
           MOVE "K009X009D9" TO DYREC.
           MOVE "NO" TO BR.
           REWRITE DYREC INVALID KEY MOVE "IK" TO BR
               NOT INVALID KEY MOVE "NIK" TO BR
           END-REWRITE.
           DISPLAY "D1 " FS " " BR.
           DISPLAY "AREA " DYREC.
           MOVE "K002X001N2" TO DYREC.
           MOVE "NO" TO BR.
           REWRITE DYREC INVALID KEY MOVE "IK" TO BR
               NOT INVALID KEY MOVE "NIK" TO BR
           END-REWRITE.
           DISPLAY "D2 " FS " " BR.
           DISPLAY "AREA " DYREC.
           MOVE "K002" TO DY-KEY.
           READ DYNF INVALID KEY DISPLAY "CHK IK " FS
               NOT INVALID KEY DISPLAY "CHK " DYREC
           END-READ.
           MOVE "K002X003D3" TO DYREC.
           MOVE "NO" TO BR.
           REWRITE DYREC INVALID KEY MOVE "IK" TO BR
               NOT INVALID KEY MOVE "NIK" TO BR
           END-REWRITE.
           DISPLAY "D3 " FS " " BR.
           CLOSE DYNF.
           OPEN INPUT DYNF.
           PERFORM 3 TIMES
               READ DYNF NEXT RECORD
                   AT END DISPLAY "CHK AT END " FS
                   NOT AT END DISPLAY "CHK " DYREC
               END-READ
           END-PERFORM.
           CLOSE DYNF.
           STOP RUN.
