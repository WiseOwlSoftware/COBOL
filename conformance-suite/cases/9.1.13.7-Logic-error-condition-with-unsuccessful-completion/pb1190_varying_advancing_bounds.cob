      *>--- conformance-case
      *> id: pb1190_varying_advancing_bounds
      *> rules: [GR-9.1.13.7-4, GR-14.9.51.4-14, GR-14.9.51.4-15]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB1190 -- a WRITE with an ADVANCING phrase on a RECORD
      *> VARYING ... DEPENDING file is bound by the VARYING clause like
      *> any other WRITE.
      *>   python scripts/spec/cite.py --check 14.9.51.4 "shall not be
      *>   larger than the largest or smaller than the smallest number
      *>   of bytes allowed by the RECORD IS VARYING clause"
      *>                                          -> OK  §2023:14.9.51.4 14)
      *>   python scripts/spec/cite.py --check 14.9.51.4 "If the
      *>   execution of a WRITE statement is unsuccessful, the write
      *>   operation does not take place"         -> OK  §2023:14.9.51.4 15)
      *> VF's records are 3 to 8 bytes and the DEPENDING item VL names
      *> each record's length (§2023:13.18.43.4 GR13 a)). VL = 9 is larger
      *> than 8 and VL = 2 smaller than 3, so both ADVANCING writes are
      *> unsuccessful with '44' and write nothing (GR14, GR15); the
      *> print-control arm used to skip the bound and write the whole
      *> 8-byte area with '00'. VL = 5 is inside the bounds, so the plain
      *> WRITE succeeds. VF is written with ADVANCING phrases, so it is a
      *> print file from its OPEN and that plain WRITE acts as AFTER
      *> ADVANCING 1 LINE (§1985:VII.4.7.4 GR16; kb/Work PB5201): it is
      *> not read back as data records. The two refused writes left
      *> nothing behind exactly when VF holds the same bytes as VG, a
      *> second print file that was only ever written by that one WRITE
      *> spelled AFTER ADVANCING 1 LINE. Both are read back a byte at a
      *> time and the counts compared (a refused write that reached the
      *> medium would make VF the longer).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1190VA.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT VF ASSIGN TO "pb1190va.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS FS.
           SELECT VG ASSIGN TO "pb1190vb.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT RA ASSIGN TO "pb1190va.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT RB ASSIGN TO "pb1190vb.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD VF RECORD IS VARYING IN SIZE FROM 3 TO 8 CHARACTERS
             DEPENDING ON VL.
       01 VR PIC X(8).
       FD VG RECORD IS VARYING IN SIZE FROM 3 TO 8 CHARACTERS
             DEPENDING ON VL.
       01 VQ PIC X(8).
       FD RA.
       01 RA-REC PIC X.
       FD RB.
       01 RB-REC PIC X.
       WORKING-STORAGE SECTION.
       01 FS PIC XX.
       01 VL PIC 99.
       01 NA PIC 99 VALUE 0.
       01 NB PIC 99 VALUE 0.
       01 EOF-A PIC X VALUE "N".
       01 EOF-B PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT VF
           MOVE ALL "X" TO VR
           MOVE 9 TO VL
           WRITE VR AFTER ADVANCING 1 LINE
           DISPLAY "AFTER-VL9=" FS
           MOVE 2 TO VL
           WRITE VR BEFORE ADVANCING 1 LINE
           DISPLAY "BEFORE-VL2=" FS
           MOVE 5 TO VL
           WRITE VR
           DISPLAY "PLAIN-VL5=" FS
           CLOSE VF
           OPEN OUTPUT VG
           MOVE ALL "X" TO VQ
           MOVE 5 TO VL
           WRITE VQ AFTER ADVANCING 1 LINE
           CLOSE VG
           OPEN INPUT RA
           PERFORM UNTIL EOF-A = "Y"
               READ RA AT END MOVE "Y" TO EOF-A
                   NOT AT END ADD 1 TO NA
               END-READ
           END-PERFORM
           CLOSE RA
           OPEN INPUT RB
           PERFORM UNTIL EOF-B = "Y"
               READ RB AT END MOVE "Y" TO EOF-B
                   NOT AT END ADD 1 TO NB
               END-READ
           END-PERFORM
           CLOSE RB
           DISPLAY "BYTES-VF=" NA
           DISPLAY "BYTES-VG=" NB
           IF NA = NB AND NA > 0
               DISPLAY "SAME-BYTES"
           ELSE
               DISPLAY "DIFFERENT"
           END-IF
           STOP RUN.
