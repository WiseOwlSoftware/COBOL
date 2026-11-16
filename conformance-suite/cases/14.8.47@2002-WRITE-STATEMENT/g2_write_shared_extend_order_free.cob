      *>--- conformance-case
      *> id: g2_write_shared_extend_order_free
      *> rules: [2002-GR-14.8.47.3-18]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.47.3 GR18 - "If two or more file connectors for a
      *> sequential file add records by sharing the physical file after
      *> opening it in extend mode, the added records follow the records
      *> present in the physical file when it was opened, but are
      *> otherwise in an undefined order." §2002:14.8.47.3 GR11 - each
      *> successful WRITE releases its logical record, so both added
      *> records are in the file.
      *> The file holds SEED when FA and FB open it EXTEND, sharing with
      *> all other. FA writes AAAA and FB writes BBBB. The text fixes:
      *> record 1 is SEED (the added records follow it), records 2..3
      *> are AAAA and BBBB in EITHER order, and there are 3 records.
      *> So the program prints only order-free facts: R1, how many of
      *> records 2..3 are AAAA and how many are BBBB, and the count.
      *> The F- leg uses fixed records, the V- leg RECORD VARYING.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2WR18.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FS ASSIGN TO "g2wr18f.tmp" ORGANIZATION SEQUENTIAL
               FILE STATUS IS ST-S.
           SELECT FA ASSIGN TO "g2wr18f.tmp" ORGANIZATION SEQUENTIAL
               SHARING WITH ALL OTHER LOCK MODE IS MANUAL
               FILE STATUS IS ST-A.
           SELECT FB ASSIGN TO "g2wr18f.tmp" ORGANIZATION SEQUENTIAL
               SHARING WITH ALL OTHER LOCK MODE IS MANUAL
               FILE STATUS IS ST-B.
           SELECT VS ASSIGN TO "g2wr18v.tmp" ORGANIZATION SEQUENTIAL
               FILE STATUS IS ST-S.
           SELECT VA ASSIGN TO "g2wr18v.tmp" ORGANIZATION SEQUENTIAL
               SHARING WITH ALL OTHER LOCK MODE IS MANUAL
               FILE STATUS IS ST-A.
           SELECT VB ASSIGN TO "g2wr18v.tmp" ORGANIZATION SEQUENTIAL
               SHARING WITH ALL OTHER LOCK MODE IS MANUAL
               FILE STATUS IS ST-B.
       DATA DIVISION.
       FILE SECTION.
       FD FS.
       01 FS-REC PIC X(4).
       FD FA.
       01 FA-REC PIC X(4).
       FD FB.
       01 FB-REC PIC X(4).
       FD VS RECORD IS VARYING IN SIZE FROM 3 TO 8
              DEPENDING ON V-LEN.
       01 VS-REC PIC X(8).
       FD VA RECORD IS VARYING IN SIZE FROM 3 TO 8
              DEPENDING ON V-LEN.
       01 VA-REC PIC X(8).
       FD VB RECORD IS VARYING IN SIZE FROM 3 TO 8
              DEPENDING ON V-LEN.
       01 VB-REC PIC X(8).
       WORKING-STORAGE SECTION.
       01 ST-S  PIC XX.
       01 ST-A  PIC XX.
       01 ST-B  PIC XX.
       01 V-LEN PIC 9(4).
       01 N     PIC 9.
       01 HAS-A PIC 9.
       01 HAS-B PIC 9.
       01 R1    PIC X(8).
       01 EOF-SW PIC X.
       PROCEDURE DIVISION.
       MAIN.
           PERFORM FIXED-CASE
           PERFORM VARY-CASE
           STOP RUN.
       FIXED-CASE.
           OPEN OUTPUT FS
           MOVE "SEED" TO FS-REC
           WRITE FS-REC
           CLOSE FS
           OPEN EXTEND FA
           OPEN EXTEND FB
           DISPLAY "F-EXT A=" ST-A " B=" ST-B
           MOVE "AAAA" TO FA-REC
           WRITE FA-REC
           MOVE "BBBB" TO FB-REC
           WRITE FB-REC
           DISPLAY "F-W A=" ST-A " B=" ST-B
           CLOSE FA
           CLOSE FB
           MOVE 0 TO N HAS-A HAS-B
           OPEN INPUT FS
           MOVE "N" TO EOF-SW
           PERFORM UNTIL EOF-SW = "Y"
             READ FS AT END MOVE "Y" TO EOF-SW
             NOT AT END
             ADD 1 TO N
             IF N = 1
               MOVE FS-REC TO R1
             ELSE
               IF FS-REC = "AAAA" ADD 1 TO HAS-A END-IF
               IF FS-REC = "BBBB" ADD 1 TO HAS-B END-IF
             END-IF
             END-READ
           END-PERFORM
           CLOSE FS
           DISPLAY "F-R1=" R1(1:4)
           DISPLAY "F-HAS-AAAA=" HAS-A
           DISPLAY "F-HAS-BBBB=" HAS-B
           DISPLAY "F-COUNT=" N.
       VARY-CASE.
           OPEN OUTPUT VS
           MOVE "SEED" TO VS-REC
           MOVE 4 TO V-LEN
           WRITE VS-REC
           CLOSE VS
           OPEN EXTEND VA
           OPEN EXTEND VB
           DISPLAY "V-EXT A=" ST-A " B=" ST-B
           MOVE "AAAA" TO VA-REC
           MOVE 4 TO V-LEN
           WRITE VA-REC
           MOVE "BBBBB" TO VB-REC
           MOVE 5 TO V-LEN
           WRITE VB-REC
           DISPLAY "V-W A=" ST-A " B=" ST-B
           CLOSE VA
           CLOSE VB
           MOVE 0 TO N HAS-A HAS-B
           OPEN INPUT VS
           MOVE "N" TO EOF-SW
           PERFORM UNTIL EOF-SW = "Y"
             READ VS AT END MOVE "Y" TO EOF-SW
             NOT AT END
             ADD 1 TO N
             IF N = 1
               MOVE VS-REC(1:V-LEN) TO R1
             ELSE
               IF V-LEN = 4 AND VS-REC(1:4) = "AAAA"
                 ADD 1 TO HAS-A
               END-IF
               IF V-LEN = 5 AND VS-REC(1:5) = "BBBBB"
                 ADD 1 TO HAS-B
               END-IF
             END-IF
             END-READ
           END-PERFORM
           CLOSE VS
           DISPLAY "V-R1=" R1(1:4)
           DISPLAY "V-HAS-AAAA=" HAS-A
           DISPLAY "V-HAS-BBBBB=" HAS-B
           DISPLAY "V-COUNT=" N.
