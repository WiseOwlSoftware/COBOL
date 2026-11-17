      *>--- conformance-case
      *> id: pb5882_io_status_1985_values
      *> rules: [1985-GR-VII.1.3.5-5, 1985-GR-VII.1.3.5-L2.1, 1985-GR-VII.1.3.5-L2.5, 1985-GR-VII.4.4.4-13]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.1.3.5 gives "a list of the values placed in the I-O
      *> status for the previously named conditions" and makes a
      *> condition the implementor adds an "Implementor-Defined Condition
      *> With Unsuccessful Completion", 9x (§1985:C.2 item 48). LSF is a
      *> sequential file framed by the implementor-name LINE-SEQUENTIAL
      *> (§1985:VII.2.8.4 GR3; kb/Work PB5844); RAWF writes its lines raw
      *> (a fixed-length record sequential file adds nothing to its
      *> records, DOC-A.1-146). The program is told only 1985 values
      *> (kb/Work PB5882; docs/FILES_DESIGN.md D33; the conformance
      *> statement's section 3.13):
      *> - R1: a line holding a character outside the line character
      *>   set (SOH) is a successful READ of which 1985 says no more,
      *>   "successfully executed and no further information is
      *>   available": 00, never 09. LL holds its length, 4.
      *> - R2: a line of five characters: 00, length 5.
      *> - R3: a line of twelve characters into a record of at most
      *>   eight: §1985:VII.4.4.4 GR13, "the record is truncated on the
      *>   right to the maximum size ... the READ statement is
      *>   successful and an I-O status is set indicating a record
      *>   length conflict has occurred": 04, never 06; the record area
      *>   holds ABCDEFGH and LL 8. Each READ is successful, so its NOT
      *>   AT END phrase runs and no USE procedure does (§1985:VII.1.3.5:
      *>   "If one of the conditions listed under the heading
      *>   'Successful Completion' ... results, no such procedure will
      *>   be executed").
      *> - W1: a WRITE whose record holds the line delimiter is
      *>   unsuccessful, a condition no 1985 value names, so its 9x:
      *>   99 (never 71). The USE procedure runs and sees 99 in the
      *>   FILE STATUS item before the statement after the WRITE.
      *> - W2: the next WRITE is successful, 00, and the file then holds
      *>   only that record: the unsuccessful WRITE wrote nothing, so a
      *>   READ gives GOOD (00) and the next READ the at end condition
      *>   (10), whose AT END phrase runs and no USE procedure.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5882LS.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS LFC IS 11 SOHC IS 2.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT LSF ASSIGN TO "P5882LS.DAT"
               ORGANIZATION IS SEQUENTIAL
               RECORD DELIMITER IS LINE-SEQUENTIAL
               FILE STATUS IS FSL.
           SELECT RAWF ASSIGN TO "P5882LS.DAT"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FSR.
       DATA DIVISION.
       FILE SECTION.
       FD  LSF
           RECORD IS VARYING IN SIZE FROM 1 TO 8 CHARACTERS
               DEPENDING ON LL.
       01  LREC        PIC X(8).
       FD  RAWF.
       01  RREC        PIC X(24).
       WORKING-STORAGE SECTION.
       01  FSL         PIC XX.
       01  FSR         PIC XX.
       01  LL          PIC 99.
       PROCEDURE DIVISION.
       DECLARATIVES.
       LSF-ERR SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON LSF.
       LSF-ERR-PARA.
           DISPLAY "USE " FSL.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-PARA.
      * three lines, written raw: XY<SOH>Z, FINE!, ABCDEFGHIJKL
           MOVE "XY" TO RREC (1:2).
           MOVE SOHC TO RREC (3:1).
           MOVE "Z" TO RREC (4:1).
           MOVE LFC TO RREC (5:1).
           MOVE "FINE!" TO RREC (6:5).
           MOVE LFC TO RREC (11:1).
           MOVE "ABCDEFGHIJKL" TO RREC (12:12).
           MOVE LFC TO RREC (24:1).
           OPEN OUTPUT RAWF.
           WRITE RREC.
           CLOSE RAWF.
           DISPLAY "RF " FSR.
           OPEN INPUT LSF.
           READ LSF AT END DISPLAY "EOF"
               NOT AT END DISPLAY "NAE".
           DISPLAY "R1 " FSL " " LL " " LREC (1:2) LREC (4:1).
           READ LSF AT END DISPLAY "EOF"
               NOT AT END DISPLAY "NAE".
           DISPLAY "R2 " FSL " " LL " " LREC (1:5).
           READ LSF AT END DISPLAY "EOF"
               NOT AT END DISPLAY "NAE".
           DISPLAY "R3 " FSL " " LL " " LREC.
           CLOSE LSF.
           OPEN OUTPUT LSF.
           MOVE 5 TO LL.
           MOVE "AB" TO LREC (1:2).
           MOVE LFC TO LREC (3:1).
           MOVE "CD" TO LREC (4:2).
           WRITE LREC.
           DISPLAY "W1 " FSL.
           MOVE 4 TO LL.
           MOVE "GOOD" TO LREC.
           WRITE LREC.
           DISPLAY "W2 " FSL.
           CLOSE LSF.
           OPEN INPUT LSF.
           READ LSF AT END DISPLAY "EOF".
           DISPLAY "R4 " FSL " " LL " " LREC (1:4).
           READ LSF AT END DISPLAY "EOF".
           DISPLAY "R5 " FSL.
           CLOSE LSF.
           STOP RUN.
