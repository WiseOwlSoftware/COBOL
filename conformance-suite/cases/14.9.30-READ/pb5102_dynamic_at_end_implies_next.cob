      *>--- conformance-case
      *> id: pb5102_dynamic_at_end_implies_next
      *> rules: [SR-14.9.30.3-9]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2023:14.9.30.3 SR9: "If neither the NEXT phrase nor the PREVIOUS
      *> phrase is specified and ACCESS MODE DYNAMIC is specified in the
      *> file control entry for file-name-1, the NEXT phrase is implied if
      *> any of the following phrases is specified: ADVANCING, AT END, or
      *> NOT AT END." So READ IFILE RECORD AT END on a dynamic file is the
      *> sequential read: the later editions accept it where COBOL-85 refuses
      *> it (§1985:IX.4.5.3 SR5, COBOL7144, kb/Work PB5102 - the refusal is
      *> conformance-suite/wiseowl/diagnostics/1985-SR-IX_4_5_3-5.json).
      *> The random read of key 001 makes record ALPHA available and sets
      *> the position; the AT END read is then the NEXT read (GR19), so it
      *> makes BRAVO available, and the third finds no record: AT END.
      *> A compiler that bound the AT END read as a random read would
      *> re-read ALPHA's key from the record area and print ALPHA twice.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5102IMPLNEXT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IFILE ASSIGN TO "PB5102IF"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS IKEY.
       DATA DIVISION.
       FILE SECTION.
       FD  IFILE.
       01  IREC.
           05  IKEY  PIC X(3).
           05  IDATA PIC X(5).
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
           READ IFILE RECORD AT END DISPLAY "EOF".
           DISPLAY IDATA.
           READ IFILE RECORD AT END DISPLAY "EOF".
           CLOSE IFILE.
           STOP RUN.
