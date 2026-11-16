      *>--- conformance-case
      *> id: g85_bf30_01
      *> rules: [1985-GR-VIII.2.4.4-6, 1985-GR-VIII.2.4.4-7]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.2.4.4 GR6 - data-name-1 "is used to communicate a
      *> relative record number between the user and the mass storage
      *> control system": both directions are exercised.  User to MSCS:
      *> records are WRITTEN out of order and READ randomly by the key
      *> (§1985:VIII.4.9.4 GR11b, §1985:VIII.4.5.4 GR15), so W/RF/RG
      *> lines can only be right if the key carried the number.  MSCS
      *> to user: a READ NEXT stores the number of the record made
      *> available into the key (§1985:VIII.4.5.4 GR14), line NX.
      *> §1985:VIII.2.4.4 GR7 - the relative key of an I-O statement is
      *> "the data item referenced by data-name-1 in the ACCESS MODE
      *> clause": F and G each name their OWN key and every statement
      *> runs while the OTHER file's key holds a different number, so
      *> a statement that used the wrong key item would place or select
      *> the wrong record (RF1/RG3/RF3/RG1), and the READ NEXT of F
      *> must leave KG at 0001 (NX).
      *> IKF2 - no record 2 exists in F: the random READ is the invalid
      *> key condition (§1985:VIII.4.5.4 GR15), status 23.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF3001.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "g85bf3001f.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC RELATIVE KEY IS KF
               FILE STATUS IS FS.
           SELECT G ASSIGN TO "g85bf3001g.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM RELATIVE KEY IS KG
               FILE STATUS IS GS.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 F-REC PIC X(4).
       FD G.
       01 G-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 FS PIC XX.
       01 GS PIC XX.
       01 KF PIC 9(4).
       01 KG PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT F G.
           MOVE 3 TO KF.
           MOVE 1 TO KG.
           MOVE "F003" TO F-REC.
           WRITE F-REC INVALID KEY DISPLAY "WF3-IK".
           MOVE "G001" TO G-REC.
           WRITE G-REC INVALID KEY DISPLAY "WG1-IK".
           DISPLAY "W1=" FS " " GS.
           MOVE 1 TO KF.
           MOVE 3 TO KG.
           MOVE "F001" TO F-REC.
           WRITE F-REC INVALID KEY DISPLAY "WF1-IK".
           MOVE "G003" TO G-REC.
           WRITE G-REC INVALID KEY DISPLAY "WG3-IK".
           DISPLAY "W2=" FS " " GS.
           CLOSE F G.
           OPEN INPUT F G.
           MOVE 1 TO KF.
           MOVE 3 TO KG.
           READ F INVALID KEY DISPLAY "RF1-IK".
           DISPLAY "RF1=" FS " " F-REC.
           READ G INVALID KEY DISPLAY "RG3-IK".
           DISPLAY "RG3=" GS " " G-REC.
           MOVE 3 TO KF.
           MOVE 1 TO KG.
           READ F INVALID KEY DISPLAY "RF3-IK".
           DISPLAY "RF3=" FS " " F-REC.
           READ G INVALID KEY DISPLAY "RG1-IK".
           DISPLAY "RG1=" GS " " G-REC.
           MOVE 2 TO KF.
           READ F INVALID KEY DISPLAY "IKF2=" FS.
      * A random READ of record 1 sets the file position indicator to
      * 1; the READ NEXT then makes record 3 available and stores 3.
           MOVE 1 TO KF.
           READ F INVALID KEY DISPLAY "RF1B-IK".
           MOVE 9999 TO KF.
           READ F NEXT AT END DISPLAY "NX-ATEND".
           DISPLAY "NX=" FS " " KF " " F-REC " " KG.
           CLOSE F G.
           STOP RUN.
