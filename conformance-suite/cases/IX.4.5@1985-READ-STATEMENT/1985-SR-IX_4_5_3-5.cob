      *>--- conformance-case
      *> id: 1985-SR-IX_4_5_3-5
      *> rules: [1985-SR-IX.4.5.3-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.4.5.3 SR5 (THE READ STATEMENT). A refusal: "The NEXT
      *> phrase must be specified for files in dynamic access mode, when
      *> records are to be retrieved sequentially." The AT END phrase is
      *> printed in format 1 of the 1985 READ only (the sequential
      *> retrieval), so the READ of IFILE on line 46 writes AT END for a
      *> dynamic-access file and omits NEXT: WiseOwl refuses it at 85 with
      *> COBOL7144 there. A compiler that reads the later editions' implied
      *> NEXT (§2023:14.9.30.3 SR9) and accepts it fails.
      *> (kb/Work PB5102)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SR1985IX453N.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IFILE ASSIGN TO "IFILE"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS IKEY.
       DATA DIVISION.
       FILE SECTION.
       FD  IFILE
           LABEL RECORDS ARE STANDARD.
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
           CLOSE IFILE.
           STOP RUN.
