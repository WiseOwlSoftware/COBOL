      *>--- conformance-case
      *> id: pb1563_line_sequential_dynamic_members_exceptions
      *> rules: [DOC-A.1-63, GR-12.4.5.10.3-2, GR-14.9.30.4-15,
      *>    GR-14.9.30.4-16,
      *>    GR-14.9.35.4-3, GR-14.9.35.4-5, GR-14.9.35.4-11,
      *>    GR-14.9.35.4-14, GR-14.9.35.4-17, GR-14.7.9.3-4,
      *>    GR-9.1.13.8-1, GR-14.9.40.4-12, GR-14.9.40.4-15]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB1563 - THE ABNORMAL AND EXCEPTION PATHS OF A LINE
      *> SEQUENTIAL RECORD THAT HOLDS DYNAMIC-LENGTH MEMBERS (the
      *> companion of pb1563_line_sequential_dynamic_members, which
      *> walks the success paths and the first exceptions; docs/
      *> CONFORMANCE.md section 3, determination D-FRA (x)).
      *>
      *> The record area of such a record after a READ is the WHOLE
      *> contiguous line (D-FRA (x)), but its FIXED run is only
      *> three characters wide.  Every rule below that asks about
      *> "the record area" therefore has to look at the line, not at
      *> the fixed run - a control character that sits in a dynamic
      *> member BEYOND the fixed run is exactly as outside the line
      *> sequential character set as one inside it.  The file below
      *> is written through a record sequential print stream (which
      *> has no 14.9.51.4 GR23) so that it can hold such lines.
      *>
      *> Paths walked (each uses a file with a FILE STATUS; LF also
      *> has a USE procedure, LA/LC/the SORT files have none):
      *>   H. READ, 14.9.30.4 GR16 '09': a control character past the
      *>      fixed run (line 3), inside it (line 4), and in the
      *>      remainder of an over-length line - the READ that
      *>      delivers 20 characters is '06' (GR15), the READ that
      *>      delivers the remainder holding the character is '09'.
      *>      '10' ends the file.
      *>   I. REWRITE, through the USE procedure: '43' with no
      *>      successful READ before it (14.9.35.4 GR5), '71' for a
      *>      record whose dynamic member holds a control character
      *>      (GR17 d), '43' again because the failed REWRITE is not a
      *>      READ (GR5) - and the line is unchanged (GR14) - and
      *>      '49' on a closed file and on one open for input (GR3).
      *>   J. Record locks between two connectors on one file:
      *>      a plain READ of a record locked by the other connector
      *>      is '51' (14.9.30.4 GR9, 9.1.13.8 item 1), with or
      *>      without RETRY (14.7.9.3 GR4 - the retries fail); the
      *>      REWRITE of a record the other connector locked is '51'
      *>      (14.9.35.4 GR11), and the same REWRITE executed again
      *>      (here with RETRY) is '43' - the '51' REWRITE is not a
      *>      READ (GR5, tested before GR11; DOC-A.1-104 (2));
      *>      once the lock is released (a second
      *>      READ WITH LOCK moves the single lock, 14.9.30.4 GR11 a)
      *>      the REWRITE is '00'.
      *>   K. SORT with a line sequential USING file that is not
      *>      there ('35' at the implicit OPEN) leaves the GIVING
      *>      file untouched, and a GIVING file that cannot be
      *>      created ('30') ends the SORT (14.9.40.4 GR12 b) and
      *>      GR15, the sequential twins are pb993).
      *> The same abnormal paths WITHOUT a FILE STATUS or a USE
      *> procedure end the run unit (9.1.13.1 and 14.6.13.1.3 7)
      *> and cannot be a golden: they are
      *> LineSequentialDynamicMembersAbnormalTests.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1563LSEXC.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SEEDF ASSIGN TO "pb1563x.dat"
              ORGANIZATION IS SEQUENTIAL
              FILE STATUS IS SEED-ST.
           SELECT LF ASSIGN TO "pb1563x.dat"
              ORGANIZATION IS LINE SEQUENTIAL
              FILE STATUS IS LF-ST.
           SELECT LA ASSIGN TO "pb1563x.dat"
              ORGANIZATION IS LINE SEQUENTIAL
              SHARING WITH ALL OTHER
              LOCK MODE IS MANUAL
              FILE STATUS IS LA-ST.
           SELECT LC ASSIGN TO "pb1563x.dat"
              ORGANIZATION IS LINE SEQUENTIAL
              SHARING WITH ALL OTHER
              LOCK MODE IS MANUAL
              FILE STATUS IS LC-ST.
           SELECT SIN ASSIGN TO "pb1563y.dat"
              ORGANIZATION IS LINE SEQUENTIAL
              FILE STATUS IS SI-ST.
           SELECT SOUT ASSIGN TO "pb1563z.dat"
              ORGANIZATION IS LINE SEQUENTIAL
              FILE STATUS IS SO-ST.
           SELECT MISS ASSIGN TO "pb1563-never-written.dat"
              ORGANIZATION IS LINE SEQUENTIAL
              FILE STATUS IS MS-ST.
           SELECT BADOUT ASSIGN TO "pb1563-no-such-dir/o.dat"
              ORGANIZATION IS LINE SEQUENTIAL
              FILE STATUS IS BO-ST.
           SELECT SRTF ASSIGN TO "pb1563.srt".
       DATA DIVISION.
       FILE SECTION.
       FD SEEDF.
       01 S-REC PIC X(25).
       FD LF.
       01 LR.
          05 A PIC X DYNAMIC LENGTH LIMIT 9.
          05 KY PIC X(3).
          05 C PIC X DYNAMIC LENGTH LIMIT 8.
       FD LA.
       01 AR.
          05 AA PIC X DYNAMIC LENGTH LIMIT 9.
          05 AKY PIC X(3).
          05 AC PIC X DYNAMIC LENGTH LIMIT 8.
       FD LC.
       01 CR.
          05 CA PIC X DYNAMIC LENGTH LIMIT 9.
          05 CKY PIC X(3).
          05 CC PIC X DYNAMIC LENGTH LIMIT 8.
       FD SIN.
       01 IR.
          05 IK PIC X(2).
          05 IA PIC X DYNAMIC LENGTH LIMIT 9.
       FD SOUT.
       01 OR1.
          05 OK PIC X(2).
          05 OA PIC X DYNAMIC LENGTH LIMIT 9.
       FD MISS.
       01 MR.
          05 MK PIC X(2).
          05 MA PIC X DYNAMIC LENGTH LIMIT 9.
       FD BADOUT.
       01 BR.
          05 BK PIC X(2).
          05 BA PIC X DYNAMIC LENGTH LIMIT 9.
       SD SRTF.
       01 SR.
          05 SK PIC X(2).
          05 SA PIC X DYNAMIC LENGTH LIMIT 9.
       WORKING-STORAGE SECTION.
       01 SEED-ST PIC XX.
       01 LF-ST PIC XX.
       01 LA-ST PIC XX.
       01 LC-ST PIC XX.
       01 SI-ST PIC XX.
       01 SO-ST PIC XX.
       01 MS-ST PIC XX.
       01 BO-ST PIC XX.
       01 W-LEN PIC 99.
       PROCEDURE DIVISION.
       DECLARATIVES.
       LF-ERR SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON LF.
       LF-ERR-1.
           DISPLAY "  USE LF=" LF-ST.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-1.
           OPEN OUTPUT SEEDF.
           MOVE "AAKEYCCCC" TO S-REC.
           WRITE S-REC BEFORE ADVANCING 1 LINE.
           MOVE "HILOW" TO S-REC.
           WRITE S-REC BEFORE ADVANCING 1 LINE.
           MOVE "AAKEYCC" TO S-REC.
           MOVE X"01" TO S-REC(6:1).
           WRITE S-REC BEFORE ADVANCING 1 LINE.
           MOVE "AKEYCCC" TO S-REC(2:7).
           MOVE X"01" TO S-REC(1:1).
           MOVE SPACES TO S-REC(9:).
           WRITE S-REC BEFORE ADVANCING 1 LINE.
           MOVE "ABCDEFGHIJKLMNOPQRSTUV" TO S-REC.
           MOVE X"01" TO S-REC(23:1).
           MOVE "WX" TO S-REC(24:2).
           WRITE S-REC BEFORE ADVANCING 1 LINE.
           CLOSE SEEDF.
           DISPLAY "SEED=" SEED-ST.
      *> ---- H. READ '09'
           OPEN INPUT LF.
           PERFORM 6 TIMES
              READ LF AT END DISPLAY "EOF-H"
              END-READ
              MOVE FUNCTION LENGTH(LR) TO W-LEN
              DISPLAY "H=" LF-ST " LEN=" W-LEN
           END-PERFORM.
           READ LF AT END DISPLAY "EOF-H".
           DISPLAY "H7=" LF-ST.
           CLOSE LF.
      *> ---- I. REWRITE 43, 71, 00, 49
           OPEN I-O LF.
           REWRITE LR.
           DISPLAY "I1=" LF-ST.
           READ LF AT END DISPLAY "EOF-I".
           DISPLAY "I2=" LF-ST " [" A "] [" KY "] [" C "]".
           MOVE "AA" TO A.
           MOVE "KEY" TO KY.
           MOVE X"01" TO C.
           REWRITE LR.
           DISPLAY "I3=" LF-ST.
           MOVE "c" TO C.
           REWRITE LR.
           DISPLAY "I4=" LF-ST.
           CLOSE LF.
           REWRITE LR.
           DISPLAY "I5=" LF-ST.
           OPEN INPUT LF.
           READ LF AT END DISPLAY "EOF-I6".
           REWRITE LR.
           DISPLAY "I6=" LF-ST.
           CLOSE LF.
      *> ---- J. lock
           OPEN I-O LA.
           OPEN I-O LC.
           DISPLAY "J0=" LA-ST LC-ST.
           READ LA WITH LOCK AT END DISPLAY "EOF-J".
           DISPLAY "J1=" LA-ST " [" AR "]".
           READ LC AT END DISPLAY "EOF-J".
           DISPLAY "J2=" LC-ST.
           READ LC RETRY 2 TIMES AT END DISPLAY "EOF-J".
           DISPLAY "J3=" LC-ST.
           CLOSE LA.
           CLOSE LC.
           OPEN I-O LA.
           OPEN I-O LC.
           READ LC AT END DISPLAY "EOF-J".
           DISPLAY "J4=" LC-ST " [" CR "]".
           READ LA WITH LOCK AT END DISPLAY "EOF-J".
           DISPLAY "J5=" LA-ST " [" AR "]".
           MOVE "ZZ" TO CA.
           REWRITE CR.
           DISPLAY "J6=" LC-ST.
      *> The immediately previous statement was the '51' REWRITE, not a
      *> successful READ: '43' (14.9.35.4 GR5), with no lock wait.
           REWRITE CR RETRY 2 TIMES.
           DISPLAY "J7=" LC-ST.
           CLOSE LA.
           CLOSE LC.
           OPEN I-O LA.
           OPEN I-O LC.
           READ LA WITH LOCK AT END DISPLAY "EOF-J".
           DISPLAY "J8=" LA-ST " [" AR "]".
           READ LA WITH LOCK AT END DISPLAY "EOF-J".
           DISPLAY "J9=" LA-ST " [" AR "]".
           READ LC AT END DISPLAY "EOF-J".
           DISPLAY "J10=" LC-ST " [" CR "]".
           REWRITE CR.
           DISPLAY "J11=" LC-ST.
           CLOSE LA.
           CLOSE LC.
      *> ---- K. SORT
           OPEN OUTPUT SOUT.
           MOVE "OL" TO OK.
           MOVE "D" TO OA.
           WRITE OR1.
           CLOSE SOUT.
           SORT SRTF ON ASCENDING KEY SK USING MISS GIVING SOUT.
           DISPLAY "K1=" MS-ST.
           OPEN INPUT SOUT.
           READ SOUT AT END DISPLAY "EOF-K".
           DISPLAY "K2=" SO-ST " [" OR1 "]".
           CLOSE SOUT.
           OPEN OUTPUT SIN.
           MOVE "B2" TO IK.
           MOVE "AA" TO IA.
           WRITE IR.
           CLOSE SIN.
           SORT SRTF ON ASCENDING KEY SK USING SIN GIVING BADOUT.
           DISPLAY "K3=" BO-ST.
           STOP RUN.
