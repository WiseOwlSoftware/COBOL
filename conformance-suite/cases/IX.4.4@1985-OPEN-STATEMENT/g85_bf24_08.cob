      *>--- conformance-case
      *> id: g85_bf24_08
      *> rules: [1985-GR-IX.4.4.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.4.4.4 GR3 - "When a file is not in an open mode, no
      *> statement may be executed which references the file ...
      *> except for a MERGE statement with the USING or GIVING phrase,
      *> an OPEN statement, or a SORT statement with the USING or
      *> GIVING phrase." The indexed file XF, dynamic access:
      *>   OPEN OUTPUT XF on a not-open file -> 00; K002 BBBB,
      *>   K001 CCCC, K003 AAAA are written (any order in dynamic
      *>   access, §1985:IX.4.9.4 GR15), XF is closed.
      *>   SORT ... ASCENDING KEY SW-VAL USING XF GIVING SQ with both
      *>   not open; it leaves both not open (§1985:XI.4.4.4 GR6), so
      *>   OPEN I-O XF and OPEN INPUT SQ each give 00 (an open file
      *>   gives 41); K002 still reads BBBB; SQ holds the records in
      *>   SW-VAL order: K003AAAA K002BBBB K001CCCC.
      *> Last, the forbidden form: a DELETE on XF after it is closed.
      *> §1985:IX.1.3.4 4h sets I-O status 49 (dynamic access, so the
      *> sequential-access 43 cannot also apply), shown by the USE
      *> procedure (USE-XF=49). A 4x status is critical, so what
      *> follows the USE procedure is the implementor's; nothing after
      *> it is displayed.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF2408.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT XF ASSIGN TO "g85bf2408x.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS XF-KEY
               FILE STATUS IS ST-X.
           SELECT SQ ASSIGN TO "g85bf2408s.dat"
               FILE STATUS IS ST-S.
           SELECT SW ASSIGN TO "g85bf2408w.tmp".
       DATA DIVISION.
       FILE SECTION.
       FD  XF.
       01  XF-REC.
           05 XF-KEY PIC X(4).
           05 XF-VAL PIC X(4).
       FD  SQ.
       01  SQ-REC PIC X(8).
       SD  SW.
       01  SW-REC.
           05 SW-K   PIC X(4).
           05 SW-VAL PIC X(4).
       WORKING-STORAGE SECTION.
       01  ST-X PIC XX.
       01  ST-S PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       X-ERR SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON XF.
       X-ERR-PARA.
           DISPLAY "USE-XF=" ST-X.
       END DECLARATIVES.
       MAIN-SEC SECTION.
       MAIN-PARA.
           OPEN OUTPUT XF.
           DISPLAY "OPEN-XF=" ST-X.
           MOVE "K002BBBB" TO XF-REC.
           WRITE XF-REC INVALID KEY DISPLAY "W1-INVALID".
           MOVE "K001CCCC" TO XF-REC.
           WRITE XF-REC INVALID KEY DISPLAY "W2-INVALID".
           MOVE "K003AAAA" TO XF-REC.
           WRITE XF-REC INVALID KEY DISPLAY "W3-INVALID".
           CLOSE XF.
           SORT SW ON ASCENDING KEY SW-VAL USING XF GIVING SQ.
           OPEN I-O XF.
           DISPLAY "OPEN-XF2=" ST-X.
           MOVE "K002" TO XF-KEY.
           READ XF INVALID KEY DISPLAY "R2-INVALID".
           DISPLAY "XF2=" XF-REC.
           CLOSE XF.
           OPEN INPUT SQ.
           DISPLAY "OPEN-SQ=" ST-S.
           READ SQ AT END DISPLAY "SQ-EOF-EARLY".
           DISPLAY "SQ1=" SQ-REC.
           READ SQ AT END DISPLAY "SQ-EOF-EARLY".
           DISPLAY "SQ2=" SQ-REC.
           READ SQ AT END DISPLAY "SQ-EOF-EARLY".
           DISPLAY "SQ3=" SQ-REC.
           READ SQ AT END DISPLAY "SQ-EOF".
           CLOSE SQ.
           MOVE "K001" TO XF-KEY.
           DELETE XF RECORD.
           STOP RUN.
