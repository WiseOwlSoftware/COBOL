      *>--- conformance-case
      *> id: g85_f20_07
      *> rules: [1985-GR-IX.4.5.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.4.5.4 GR1 - a READ of an indexed file executes only on
      *> a file open in the input or I-O mode: "The file referenced by
      *> file-name-1 must be open in the input or I-O mode at the time
      *> this statement is executed." X has dynamic access, so both
      *> READ formats are exercised: the keyed READ (format 2) and READ
      *> NEXT (format 1). In the input and the I-O mode each succeeds,
      *> §1985:IX.1.3.4 "The input-output statement is successfully
      *> executed and no further information is available" = 00, with
      *> the record of the key asked for, or the next one (the keyed
      *> READ "sets the file position indicator to the value in the
      *> key of reference", §1985:IX.4.5.4 GR17, so READ NEXT after
      *> K001 gives K002; after OPEN it gives the first). Never opened
      *> and open in the output mode, each READ gets §1985:IX.1.3.4
      *> "The execution of a READ or START statement is attempted on a
      *> file not open in the input or I-O mode" = 47, and neither its
      *> INVALID KEY nor its AT END imperative runs (47 is a logic
      *> error). A compiler that refused the READ in the input or I-O
      *> mode, or let it through in another mode, fails a line.
      *> Continuing after a 4x status with no USE procedure is the
      *> implementor's choice (§1985:IX.1.3.4); WiseOwl continues.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F2007.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT X ASSIGN TO "g85f2007.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS X-KEY
               FILE STATUS IS ST.
       DATA DIVISION.
       FILE SECTION.
       FD X.
       01 X-REC.
           02 X-KEY PIC X(4).
           02 X-DATA PIC X(4).
       WORKING-STORAGE SECTION.
       01 ST PIC XX.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "K001" TO X-KEY
           READ X INVALID KEY DISPLAY "X-RINV" END-READ
           DISPLAY "NOTOPEN-READ=" ST
           READ X NEXT RECORD AT END DISPLAY "X-AT-END" END-READ
           DISPLAY "NOTOPEN-READNEXT=" ST
           OPEN OUTPUT X
           MOVE "K001" TO X-KEY
           MOVE "AAAA" TO X-DATA
           WRITE X-REC INVALID KEY DISPLAY "X-WINV" END-WRITE
           MOVE "K002" TO X-KEY
           MOVE "BBBB" TO X-DATA
           WRITE X-REC INVALID KEY DISPLAY "X-WINV" END-WRITE
           MOVE "K001" TO X-KEY
           READ X INVALID KEY DISPLAY "X-RINV" END-READ
           DISPLAY "OUTPUT-READ=" ST
           READ X NEXT RECORD AT END DISPLAY "X-AT-END" END-READ
           DISPLAY "OUTPUT-READNEXT=" ST
           CLOSE X
           OPEN INPUT X
           MOVE "K001" TO X-KEY
           MOVE SPACES TO X-DATA
           READ X INVALID KEY DISPLAY "X-RINV" END-READ
           DISPLAY "INPUT-READ=" ST " " X-REC
           READ X NEXT RECORD AT END DISPLAY "X-AT-END" END-READ
           DISPLAY "INPUT-READNEXT=" ST " " X-REC
           CLOSE X
           OPEN I-O X
           MOVE "K002" TO X-KEY
           MOVE SPACES TO X-DATA
           READ X INVALID KEY DISPLAY "X-RINV" END-READ
           DISPLAY "IO-READ=" ST " " X-REC
           CLOSE X
           OPEN I-O X
           READ X NEXT RECORD AT END DISPLAY "X-AT-END" END-READ
           DISPLAY "IO-READNEXT=" ST " " X-REC
           CLOSE X
           STOP RUN.
