      *>--- conformance-case
      *> id: g85_f20_08
      *> rules: [1985-GR-IX.4.7.4-1, 1985-GR-IX.4.7.4-3, 1985-GR-IX.4.7.4-5, 1985-GR-IX.4.7.4-6]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.4.7.4 GR1, GR3, GR5, GR6 - the START statement of the
      *> Indexed I-O module (level 2), on X (dynamic access, RECORD IS
      *> VARYING ... DEPENDING ON XL, prime key X-KEY) and on M
      *> (OPTIONAL, absent, so its OPEN INPUT gives §1985:IX.1.3.4 "An
      *> OPEN statement is successfully executed but the referenced
      *> optional file is not present" = 05).
      *> GR1 "must be open in the input or I-O mode at the time that the
      *> START statement is executed": START never opened and in the
      *> output mode gets §1985:IX.1.3.4 "The execution of a READ or
      *> START statement is attempted on a file not open in the input
      *> or I-O mode" = 47 (NOTOPEN, OUTPUT; no INVALID KEY line, 47 is
      *> a logic error); in the input mode (S1) and the I-O mode (IO)
      *> it succeeds, "The input-output statement is successfully
      *> executed and no further information is available" = 00.
      *> GR3 "does not alter either the content of the record area or
      *> the content of the data item referenced by the data-name
      *> specified in the DEPENDING ON phrase": before S1 and S2 the
      *> record area holds the key asked for and a pattern no record
      *> holds, and XL a length no record has; both are unchanged
      *> after the successful S1 and the failed S2. A START that loaded
      *> the record or set XL fails S1.
      *> GR5 "causes the value of the I-O status associated with
      *> file-name-1 to be updated": the status item is set to 99
      *> before each START and reads 00, 23, 00 and 23 after S1, S2,
      *> IO and S3. 23 is the invalid key value: for S2 by
      *> §1985:IX.4.7.4 GR4 "If the comparison is not satisfied by any
      *> record in the file, the invalid key condition exists" (no key
      *> K009; of the 1985 invalid key values 21 is a sequence error,
      *> 22 a duplicate key, 24 a boundary, so 23); for S3 by
      *> §1985:IX.1.3.4 "A START or random READ statement is attempted
      *> on an optional input file that is not present" = 23.
      *> GR6 "the invalid key condition exists and the execution of the
      *> START statement is unsuccessful": the START on absent M runs
      *> its INVALID KEY imperative (§1985:IX.1.3.5 "control is
      *> transferred to the imperative-statement specified in the
      *> INVALID KEY phrase") and leaves status 23.
      *> The READ NEXT after S1 (R1) shows the START positioned on K002
      *> (GR4a) and XL set to that record's length 8, §1985:VII.3.8.4
      *> GR11 "the contents of the data item referenced by data-name-1
      *> will indicate the number of character positions in the record
      *> just read" (the Indexed I-O RECORD clause is the Sequential
      *> I-O one).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F2008.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT X ASSIGN TO "g85f2008.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS X-KEY
               FILE STATUS IS ST.
           SELECT OPTIONAL M ASSIGN TO "g85f2008m.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS M-KEY
               FILE STATUS IS ST-M.
       DATA DIVISION.
       FILE SECTION.
       FD X
           RECORD IS VARYING IN SIZE FROM 4 TO 8 CHARACTERS
               DEPENDING ON XL.
       01 X-REC.
           02 X-KEY PIC X(4).
           02 X-DATA PIC X(4).
       FD M.
       01 M-REC.
           02 M-KEY PIC X(4).
           02 M-DATA PIC X(4).
       WORKING-STORAGE SECTION.
       01 XL PIC 9.
       01 ST PIC XX.
       01 ST-M PIC XX.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "K001" TO X-KEY
           START X KEY IS EQUAL TO X-KEY
               INVALID KEY DISPLAY "NOTOPEN-INVALID"
           END-START
           DISPLAY "NOTOPEN-START=" ST
           OPEN OUTPUT X
           MOVE 6 TO XL
           MOVE "K001AA" TO X-REC
           WRITE X-REC INVALID KEY DISPLAY "W-INVALID" END-WRITE
           MOVE 8 TO XL
           MOVE "K002BBBB" TO X-REC
           WRITE X-REC INVALID KEY DISPLAY "W-INVALID" END-WRITE
           MOVE "K001" TO X-KEY
           START X KEY IS EQUAL TO X-KEY
               INVALID KEY DISPLAY "OUTPUT-INVALID"
           END-START
           DISPLAY "OUTPUT-START=" ST
           CLOSE X
           OPEN INPUT X
           MOVE "K002ZZZZ" TO X-REC
           MOVE 5 TO XL
           MOVE "99" TO ST
           START X KEY IS EQUAL TO X-KEY
               INVALID KEY DISPLAY "S1-INVALID"
               NOT INVALID KEY DISPLAY "S1-NOT-INVALID"
           END-START
           DISPLAY "S1=" ST " " X-REC " " XL
           READ X NEXT RECORD AT END DISPLAY "X-AT-END" END-READ
           DISPLAY "R1=" ST " " X-REC " " XL
           MOVE "K009YYYY" TO X-REC
           MOVE 7 TO XL
           MOVE "99" TO ST
           START X KEY IS EQUAL TO X-KEY
               INVALID KEY DISPLAY "S2-INVALID"
               NOT INVALID KEY DISPLAY "S2-NOT-INVALID"
           END-START
           DISPLAY "S2=" ST " " X-REC " " XL
           CLOSE X
           OPEN I-O X
           MOVE "K000" TO X-KEY
           MOVE "99" TO ST
           START X KEY IS NOT LESS THAN X-KEY
               INVALID KEY DISPLAY "IO-INVALID"
           END-START
           DISPLAY "IO-START=" ST
           CLOSE X
           OPEN INPUT M
           DISPLAY "M-OPEN=" ST-M
           MOVE "99" TO ST-M
           MOVE "K001" TO M-KEY
           START M KEY IS EQUAL TO M-KEY
               INVALID KEY DISPLAY "S3-INVALID"
               NOT INVALID KEY DISPLAY "S3-NOT-INVALID"
           END-START
           DISPLAY "S3=" ST-M
           CLOSE M
           STOP RUN.
