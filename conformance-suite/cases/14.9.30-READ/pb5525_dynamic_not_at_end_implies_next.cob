      *>--- conformance-case
      *> id: pb5525_dynamic_not_at_end_implies_next
      *> rules: [SR-14.9.30.3-9]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2023:14.9.30.3 SR9: "If neither the NEXT phrase nor the PREVIOUS
      *> phrase is specified and ACCESS MODE DYNAMIC is specified in the
      *> file control entry for file-name-1, the NEXT phrase is implied if
      *> any of the following phrases is specified: ADVANCING, AT END, or
      *> NOT AT END." So a READ that writes NOT AT END alone on a dynamic
      *> file is the sequential read: the later editions accept it where
      *> COBOL-85 refuses it (§1985:VIII.4.5.3 SR3, §1985:IX.4.5.3 SR5,
      *> COBOL7144; the refusals are pb5525-relative-not-at-end-85 and
      *> pb5525-indexed-not-at-end-85, kb/Work PB5525). For each file the
      *> random read of the first record sets the position, so the NOT AT
      *> END read is the NEXT read: it makes the second record available
      *> and runs its NOT AT END phrase. A compiler that bound it as a
      *> random read would re-read the first record's key and print ALPHA
      *> (or AAAA) twice; one that did not run the phrase prints nothing.
      *> The relative read "moves the relative record number of the record
      *> made available" to RKEY (§2023:14.9.30.4 GR25), so it shows 0002.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5525IMPLNEXT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IFILE ASSIGN TO "PB5525IF"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS IKEY.
           SELECT RFILE ASSIGN TO "PB5525RF"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS RKEY.
       DATA DIVISION.
       FILE SECTION.
       FD  IFILE.
       01  IREC.
           05  IKEY  PIC X(3).
           05  IDATA PIC X(5).
       FD  RFILE.
       01  RREC PIC X(4).
       WORKING-STORAGE SECTION.
       01  RKEY PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT IFILE.
           MOVE "001" TO IKEY.
           MOVE "ALPHA" TO IDATA.
           WRITE IREC INVALID KEY DISPLAY "BADW1".
           MOVE "002" TO IKEY.
           MOVE "BRAVO" TO IDATA.
           WRITE IREC INVALID KEY DISPLAY "BADW2".
           CLOSE IFILE.
           OPEN INPUT IFILE.
           MOVE "001" TO IKEY.
           READ IFILE INVALID KEY DISPLAY "NOKEY".
           DISPLAY IDATA.
           READ IFILE RECORD
               NOT AT END DISPLAY "NEXT " IDATA.
           CLOSE IFILE.
           OPEN OUTPUT RFILE.
           MOVE 1 TO RKEY.
           MOVE "AAAA" TO RREC.
           WRITE RREC INVALID KEY DISPLAY "BADW3".
           MOVE 2 TO RKEY.
           MOVE "BBBB" TO RREC.
           WRITE RREC INVALID KEY DISPLAY "BADW4".
           CLOSE RFILE.
           OPEN INPUT RFILE.
           MOVE 1 TO RKEY.
           READ RFILE INVALID KEY DISPLAY "NOREL".
           DISPLAY RREC.
           READ RFILE RECORD
               NOT AT END DISPLAY "NEXT " RREC " " RKEY.
           CLOSE RFILE.
           STOP RUN.
