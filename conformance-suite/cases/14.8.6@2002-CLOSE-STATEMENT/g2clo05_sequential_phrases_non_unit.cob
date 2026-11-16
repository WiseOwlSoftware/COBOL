      *>--- conformance-case
      *> id: g2clo05_sequential_phrases_non_unit
      *> rules: [2002-SR-14.8.6.2-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.6.2 SR1 - "The NO REWIND, REEL, and UNIT phrases
      *> may be used only with files that are of sequential
      *> organization": the rule GRANTS the three phrases on a
      *> sequential file, so they are written here and executed.
      *> SF is a disk file, Non-unit (§2002:14.8.6.3 GR2a; DOC-A.1-24).
      *> Table 15, Non-unit column (§2002:14.8.6.3 GR3):
      *>   CLOSE UNIT [FOR REMOVAL] -> f: "Execution of this statement
      *>     is considered successful. The file remains in the open
      *>     mode, the file position indicator is unchanged"
      *>   CLOSE WITH NO REWIND -> c,h: the file is closed, "as if
      *>     none of the optional phrases were present"
      *> and each of the three reports 07 (§2002:9.1.12.1 item 5).
      *>   OUTPUT: O=00 W=00 R=07 W=00 U=07 W=00 N=07
      *>     (WRITE after REEL and after UNIT FOR REMOVAL is 00: open)
      *>   INPUT:  I=00 AAAA 00 U=07 BBBB 00 CCCC 00 E=10 C=00
      *>     (I=00, not 41: NO REWIND closed it; BBBB after CLOSE UNIT:
      *>     the file position indicator is unchanged)
      *> Fails if a granted phrase is refused, if REEL/UNIT closes the
      *> file (WRITE 48, READ 47), or if NO REWIND leaves it open
      *> (OPEN 41).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CLS05.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "g2clo05.dat"
               ORGANIZATION SEQUENTIAL FILE STATUS FS.
       DATA DIVISION.
       FILE SECTION.
       FD SF.
       01 SR PIC X(4).
       WORKING-STORAGE SECTION.
       01 FS  PIC XX.
       01 S1  PIC XX.
       01 S2  PIC XX.
       01 S3  PIC XX.
       01 S4  PIC XX.
       01 S5  PIC XX.
       01 S6  PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT SF
           MOVE FS TO S1
           MOVE "AAAA" TO SR
           WRITE SR
           MOVE FS TO S2
           CLOSE SF REEL
           MOVE FS TO S3
           MOVE "BBBB" TO SR
           WRITE SR
           MOVE FS TO S4
           CLOSE SF UNIT FOR REMOVAL
           MOVE FS TO S5
           MOVE "CCCC" TO SR
           WRITE SR
           MOVE FS TO S6
           CLOSE SF WITH NO REWIND
           DISPLAY "O=" S1 " W=" S2 " R=" S3 " W=" S4 " U=" S5
                   " W=" S6 " N=" FS
           OPEN INPUT SF
           DISPLAY "I=" FS WITH NO ADVANCING
           READ SF
           DISPLAY " " SR " " FS WITH NO ADVANCING
           CLOSE SF UNIT
           DISPLAY " U=" FS WITH NO ADVANCING
           READ SF
           DISPLAY " " SR " " FS WITH NO ADVANCING
           READ SF
           DISPLAY " " SR " " FS WITH NO ADVANCING
           READ SF
           DISPLAY " E=" FS WITH NO ADVANCING
           CLOSE SF
           DISPLAY " C=" FS
           STOP RUN.
