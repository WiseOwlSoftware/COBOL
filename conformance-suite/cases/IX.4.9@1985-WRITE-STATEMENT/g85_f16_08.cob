      *>--- conformance-case
      *> id: g85_f16_08
      *> rules: [1985-GR-IX.4.9.4-17, 1985-GR-IX.4.9.4-18]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.4.9.4 GR17 - the invalid key condition of WRITE:
      *>  SQ21  a. sequential access, open OUTPUT, A001 is not greater
      *>        than the previous key A002 (21);
      *>  EX21  a. the same in the extend mode: A001 is not greater
      *>        than A002 (21), while A003 then succeeds;
      *>  D22PO b. open OUTPUT, the prime key K001 already exists (22);
      *>  D22PI b. the same with the file open I-O (22);
      *>  D22AO c. open OUTPUT, the alternate key X001 (no DUPLICATES)
      *>        already exists (22);
      *>  D22AI c. the same with the file open I-O (22).
      *>  Item d (the externally defined boundary) is the host's file
      *>  size; a run that reaches it is not made here.
      *> §1985:IX.4.9.4 GR18 - each of those WRITEs is unsuccessful
      *> (CHK: only A002, A003 and K001X001 are in the files), the
      *> record area keeps what the program put there (AREA lines),
      *> the I-O status shows the cause, and control goes by the
      *> invalid key rules: with INVALID KEY that branch runs (IK)
      *> and the USE procedure does not (§1985:IX.1.3.5 GR2); NOPH
      *> has no INVALID KEY phrase (§1985:IX.1.3.5 GR3), so
      *> the USE procedure runs (USE 22) and
      *> control returns to the next statement.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F1608.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SQF ASSIGN TO "g85f1608s.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS SQ-KEY
               FILE STATUS IS FS.
           SELECT DYNF ASSIGN TO "g85f1608d.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS DY-KEY
               ALTERNATE RECORD KEY IS DY-ALT
               FILE STATUS IS FS2.
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
       WORKING-STORAGE SECTION.
       01  FS                  PIC XX.
       01  FS2                 PIC XX.
       01  BR                  PIC X(3).
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-DY SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON DYNF.
       D-DY-P.
           DISPLAY "USE " FS2.
       END DECLARATIVES.
       MAIN-LOGIC SECTION.
       M-SEQ.
           OPEN OUTPUT SQF.
           MOVE "A002TWO" TO SQREC.
           PERFORM W-SQ.
           DISPLAY "W1 " FS " " BR.
           MOVE "A001ONE" TO SQREC.
           PERFORM W-SQ.
           DISPLAY "SQ21 " FS " " BR.
           DISPLAY "AREA " SQREC.
           CLOSE SQF.
           OPEN EXTEND SQF.
           DISPLAY "OPEN-EXT " FS.
           MOVE "A001EXT" TO SQREC.
           PERFORM W-SQ.
           DISPLAY "EX21 " FS " " BR.
           DISPLAY "AREA " SQREC.
           MOVE "A003THREE" TO SQREC.
           PERFORM W-SQ.
           DISPLAY "W2 " FS " " BR.
           CLOSE SQF.
       M-DYN.
           OPEN OUTPUT DYNF.
           MOVE "K001X001" TO DYREC.
           PERFORM W-DY.
           DISPLAY "W3 " FS2 " " BR.
           MOVE "K001X002" TO DYREC.
           PERFORM W-DY.
           DISPLAY "D22PO " FS2 " " BR.
           DISPLAY "AREA " DYREC.
           MOVE "K002X001" TO DYREC.
           PERFORM W-DY.
           DISPLAY "D22AO " FS2 " " BR.
           DISPLAY "AREA " DYREC.
           CLOSE DYNF.
           OPEN I-O DYNF.
           MOVE "K001X009" TO DYREC.
           PERFORM W-DY.
           DISPLAY "D22PI " FS2 " " BR.
           MOVE "K003X001" TO DYREC.
           PERFORM W-DY.
           DISPLAY "D22AI " FS2 " " BR.
           MOVE "K001X008" TO DYREC.
           WRITE DYREC.
           DISPLAY "NOPH " FS2.
           DISPLAY "AREA " DYREC.
           CLOSE DYNF.
       M-CHK.
           OPEN INPUT SQF.
           PERFORM 3 TIMES
               READ SQF AT END DISPLAY "CHK AT END " FS
                   NOT AT END DISPLAY "CHK " SQREC
               END-READ
           END-PERFORM.
           CLOSE SQF.
           OPEN INPUT DYNF.
           PERFORM 2 TIMES
               READ DYNF NEXT RECORD
                   AT END DISPLAY "CHK AT END " FS2
                   NOT AT END DISPLAY "CHK " DYREC
               END-READ
           END-PERFORM.
           CLOSE DYNF.
           STOP RUN.
       W-SQ.
           MOVE "NO" TO BR.
           WRITE SQREC INVALID KEY MOVE "IK" TO BR
               NOT INVALID KEY MOVE "NIK" TO BR
           END-WRITE.
       W-DY.
           MOVE "NO" TO BR.
           WRITE DYREC INVALID KEY MOVE "IK" TO BR
               NOT INVALID KEY MOVE "NIK" TO BR
           END-WRITE.
