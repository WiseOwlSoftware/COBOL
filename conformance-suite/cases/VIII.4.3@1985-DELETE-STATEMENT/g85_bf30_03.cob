      *>--- conformance-case
      *> id: g85_bf30_03
      *> rules: [1985-GR-VIII.4.3.4-1, 1985-GR-VIII.4.3.4-5,
      *>    1985-GR-VIII.4.3.4-6]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> Relative I-O DELETE (§1985:VIII.4.3).
      *> §1985:VIII.4.3.4 GR1 - the file "must be open in the I-O mode
      *> at the time of the execution of this statement".  Every DEL
      *> line below is the statement succeeding in the I-O mode (00).
      *> D49, the program's LAST I-O statement: a DELETE on F open INPUT
      *> is status 49 (§1985:VIII.1.3.4, "a file not open in the I-O
      *> mode"), not an invalid key (no D49-IK line), so the USE
      *> procedure on F runs (§1985:VIII.1.3.5 GRL2.1, §1985:VIII.4.8.4
      *> GR5a) with the status already set (§1985:VIII.1.3.4) and
      *> displays it.  What happens after a critical status is the
      *> implementor's (§1985:VIII.4.8.4 GR6), so nothing follows but
      *> STOP RUN and no line depends on it.
      *> §1985:VIII.4.3.4 GR5 - DELETE "does not affect the content of
      *> the record area" nor the DEPENDING ON item (WL): sentinels no
      *> record holds are stored before each DELETE and must survive
      *> it, after a success (DEL1, DEL3) and after an invalid key
      *> (DEL9, status 23: no record 9, §1985:VIII.4.3.4 GR3).  The "]"
      *> pins all 8 characters of the area.
      *> §1985:VIII.4.3.4 GR6 - "The file position indicator is not
      *> affected by the execution of a DELETE statement."  The
      *> indicator is observed through the record the next READ NEXT
      *> selects (§1985:VIII.4.5.4 GR4d: the first record greater than
      *> the indicator).  DEL1 removes record 1 while the indicator is
      *> 2: RN3 must be record 3 (a DELETE that set the indicator from
      *> the key would give record 2).  DEL3 removes the record AT the
      *> indicator: RN4 must be record 4 (an invalidated indicator
      *> gives 46).  The invalid DEL9 must not move it either: RN5.
      *> Leg Q repeats GR5 and GR6 in sequential access: the DELETE
      *> removes the record last read (Q002) and the next READ is Q003.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF3003.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "g85bf3003f.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC RELATIVE KEY IS KF
               FILE STATUS IS FS.
           SELECT Q ASSIGN TO "g85bf3003q.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS QS.
       DATA DIVISION.
       FILE SECTION.
       FD F
           RECORD IS VARYING IN SIZE FROM 3 TO 8 CHARACTERS
               DEPENDING ON WL.
       01 F-REC PIC X(8).
       FD Q.
       01 Q-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 FS PIC XX.
       01 QS PIC XX.
       01 KF PIC 9(4).
       01 WL PIC 9(2).
       PROCEDURE DIVISION.
       DECLARATIVES.
       F-ERR SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON F.
       F-ERR-PARA.
           DISPLAY "D49=" FS.
       END DECLARATIVES.
       MAIN-SECTION SECTION.
       MAIN-PARA.
           OPEN OUTPUT F.
           MOVE 8 TO WL.
           MOVE 1 TO KF.
           MOVE "REC1AAAA" TO F-REC.
           WRITE F-REC INVALID KEY DISPLAY "W1-IK".
           MOVE 2 TO KF.
           MOVE "REC2BBBB" TO F-REC.
           WRITE F-REC INVALID KEY DISPLAY "W2-IK".
           MOVE 3 TO KF.
           MOVE "REC3CCCC" TO F-REC.
           WRITE F-REC INVALID KEY DISPLAY "W3-IK".
           MOVE 4 TO KF.
           MOVE "REC4DDDD" TO F-REC.
           WRITE F-REC INVALID KEY DISPLAY "W4-IK".
           MOVE 5 TO KF.
           MOVE "REC5EEEE" TO F-REC.
           WRITE F-REC INVALID KEY DISPLAY "W5-IK".
           CLOSE F.
           OPEN I-O F.
           MOVE 1 TO KF.
           READ F INVALID KEY DISPLAY "R1-IK".
           DISPLAY "R1=" FS " " F-REC.
           READ F NEXT AT END DISPLAY "RN2-ATEND".
           DISPLAY "RN2=" FS " " KF " " F-REC.
      * GR6 and GR5: delete record 1, behind the indicator (2).
           MOVE 1 TO KF.
           MOVE "SENTINL1" TO F-REC.
           MOVE 3 TO WL.
           DELETE F RECORD INVALID KEY DISPLAY "DEL1-IK".
           DISPLAY "DEL1=" FS " " F-REC "] " WL.
           READ F NEXT AT END DISPLAY "RN3-ATEND".
           DISPLAY "RN3=" FS " " KF " " F-REC.
      * GR6 and GR5: delete record 3, the one at the indicator.
           MOVE 3 TO KF.
           MOVE "SENTINL2" TO F-REC.
           MOVE 5 TO WL.
           DELETE F RECORD INVALID KEY DISPLAY "DEL3-IK".
           DISPLAY "DEL3=" FS " " F-REC "] " WL.
           READ F NEXT AT END DISPLAY "RN4-ATEND".
           DISPLAY "RN4=" FS " " KF " " F-REC.
      * GR5 and GR6 after an unsuccessful DELETE (invalid key).
           MOVE 9 TO KF.
           MOVE "SENTINL3" TO F-REC.
           MOVE 6 TO WL.
           DELETE F RECORD INVALID KEY DISPLAY "IK9=" FS.
           DISPLAY "DEL9=" F-REC "] " WL.
           READ F NEXT AT END DISPLAY "RN5-ATEND".
           DISPLAY "RN5=" FS " " KF " " F-REC.
           READ F NEXT AT END DISPLAY "END=" FS.
           CLOSE F.
      * Sequential access.
           OPEN OUTPUT Q.
           MOVE "Q001" TO Q-REC.
           WRITE Q-REC INVALID KEY DISPLAY "WQ1-IK".
           MOVE "Q002" TO Q-REC.
           WRITE Q-REC INVALID KEY DISPLAY "WQ2-IK".
           MOVE "Q003" TO Q-REC.
           WRITE Q-REC INVALID KEY DISPLAY "WQ3-IK".
           CLOSE Q.
           OPEN I-O Q.
           READ Q AT END DISPLAY "Q1-ATEND".
           READ Q AT END DISPLAY "Q2-ATEND".
           DISPLAY "QR2=" QS " " Q-REC.
           MOVE "QSNT" TO Q-REC.
           DELETE Q RECORD.
           DISPLAY "QDEL=" QS " " Q-REC.
           READ Q AT END DISPLAY "Q3-ATEND".
           DISPLAY "QR3=" QS " " Q-REC.
           CLOSE Q.
      * GR1: not open in the I-O mode.  The last I-O statement.
           OPEN INPUT F.
           MOVE 1 TO KF.
           DELETE F RECORD INVALID KEY DISPLAY "D49-IK".
           STOP RUN.
