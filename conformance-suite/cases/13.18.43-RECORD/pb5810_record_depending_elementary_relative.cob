      *>--- conformance-case
      *> id: pb5810_record_depending_elementary_relative
      *> rules: [SR-13.18.43.3-6, FMT-13.18.43.2, FMT-13.4.6.2, SR-13.4.5.3-5, SR-13.4.5.3-7]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5810 - the accepted sibling of pb5810-record-depending-table-element-relative: the same relative file with an
      *> ELEMENTARY unsigned integer DEPENDING item, the one §2023:13.18.43.3 SR6 admits. The length register is used:
      *> §2023:13.18.43.4 GR14 makes a WRITE of a record outside FROM 2 TO 8 unsuccessful, I-O status 44 (§2023:9.1.13.7 GR4a,
      *> and a length inside the range writes (status 00). Expected: "W9 44" for 9, "W5 00" for 5.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5810OK.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RL ASSIGN TO "pb5810ok.rel"
               ORGANIZATION IS RELATIVE ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD RL RECORD IS VARYING IN SIZE FROM 2 TO 8 CHARACTERS
           DEPENDING ON TL.
       01 RL-R PIC X(8).
       WORKING-STORAGE SECTION.
       01 FS PIC XX.
       01 TL PIC 99.
       PROCEDURE DIVISION.
       P.
           OPEN OUTPUT RL
           MOVE "12345678" TO RL-R
           MOVE 9 TO TL
           WRITE RL-R INVALID KEY DISPLAY "IK" END-WRITE
           DISPLAY "W9 " FS
           MOVE 5 TO TL
           WRITE RL-R INVALID KEY DISPLAY "IK" END-WRITE
           DISPLAY "W5 " FS
           CLOSE RL
           STOP RUN.
