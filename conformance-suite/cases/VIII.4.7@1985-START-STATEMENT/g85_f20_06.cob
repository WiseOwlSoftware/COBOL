      *>--- conformance-case
      *> id: g85_f20_06
      *> rules: [1985-GR-VIII.4.7.4-3, 1985-GR-VIII.4.7.4-5, 1985-GR-VIII.4.7.4-6, 1985-GR-VIII.4.7.4-7]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.4.7.4 GR3, GR5, GR6, GR7 - the START statement of
      *> the Relative I-O module (level 2), on R (dynamic access,
      *> RECORD IS VARYING ... DEPENDING ON RL) and on M (OPTIONAL,
      *> absent, so its OPEN INPUT gives §1985:VIII.1.3.4 "An OPEN
      *> statement is successfully executed but the referenced optional
      *> file is not present" = 05).
      *> GR3 "does not alter either the content of the record area or
      *> the content of the data item referenced by the data-name
      *> specified in the DEPENDING ON phrase": before each START on R
      *> the record area holds a pattern record 2 does not hold and RL
      *> a length no record has; S1 and S2 show both unchanged after a
      *> successful and after a failed START. A START that loaded the
      *> record or set RL fails S1.
      *> GR5 "causes the value of the I-O status associated with
      *> file-name-1 to be updated": the status item is set to 99
      *> before each START and reads 00 after the successful one
      *> (§1985:VIII.1.3.4 "The input-output statement is successfully
      *> executed and no further information is available") and 23
      *> after each failed one. 23 is the invalid key value: for S2 by
      *> §1985:VIII.4.7.4 GR4 "If the comparison is not satisfied by
      *> any record in the file, the invalid key condition exists"
      *> (no record 9; of the 1985 invalid key values 22 is a duplicate
      *> write, 24 a boundary, so 23); for S3 by §1985:VIII.1.3.4 "A
      *> START or random READ statement is attempted on an optional
      *> input file that is not present" = 23.
      *> GR6 "the invalid key condition exists and the execution of the
      *> START statement is unsuccessful": the START on absent M runs
      *> its INVALID KEY imperative and leaves status 23.
      *> GR7 transfer of control by the INVALID KEY and NOT INVALID KEY
      *> phrases (§1985:VIII.1.3.5 "control is transferred to the
      *> imperative-statement specified in the INVALID KEY phrase", and
      *> on success "control is transferred to the end of the
      *> input-output statement or to the imperative-statement
      *> specified in the NOT INVALID KEY phrase"): S1 prints only its
      *> NOT INVALID line, S2 and S3 only their INVALID lines. The
      *> READ NEXT after S1 (R1) shows the START positioned on record
      *> 2 (GR4a) and RL then set to its length, §1985:VII.3.8.4 GR11
      *> "after the successful execution of a READ or RETURN statement
      *> for the file, the contents of the data item referenced by
      *> data-name-1 will indicate the number of character positions
      *> in the record just read" (the Relative I-O RECORD clause is
      *> the Sequential I-O one).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F2006.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT R ASSIGN TO "g85f2006.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC RELATIVE KEY IS RK
               FILE STATUS IS ST.
           SELECT OPTIONAL M ASSIGN TO "g85f2006m.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC RELATIVE KEY IS MK
               FILE STATUS IS ST-M.
       DATA DIVISION.
       FILE SECTION.
       FD R
           RECORD IS VARYING IN SIZE FROM 1 TO 8 CHARACTERS
               DEPENDING ON RL.
       01 R-REC PIC X(8).
       FD M.
       01 M-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 RK PIC 9(4).
       01 MK PIC 9(4).
       01 RL PIC 9.
       01 ST PIC XX.
       01 ST-M PIC XX.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT R
           MOVE 1 TO RK
           MOVE 2 TO RL
           MOVE "AA" TO R-REC
           WRITE R-REC INVALID KEY DISPLAY "W-INVALID" END-WRITE
           MOVE 2 TO RK
           MOVE 4 TO RL
           MOVE "BBBB" TO R-REC
           WRITE R-REC INVALID KEY DISPLAY "W-INVALID" END-WRITE
           CLOSE R
           OPEN INPUT R
           MOVE "ZZZZZZZZ" TO R-REC
           MOVE 7 TO RL
           MOVE "99" TO ST
           MOVE 2 TO RK
           START R KEY IS EQUAL TO RK
               INVALID KEY DISPLAY "S1-INVALID"
               NOT INVALID KEY DISPLAY "S1-NOT-INVALID"
           END-START
           DISPLAY "S1=" ST " " R-REC " " RL
           READ R NEXT RECORD AT END DISPLAY "R-AT-END" END-READ
           DISPLAY "R1=" ST " " R-REC(1:RL) " " RL
           MOVE "YYYYYYYY" TO R-REC
           MOVE 5 TO RL
           MOVE "99" TO ST
           MOVE 9 TO RK
           START R KEY IS EQUAL TO RK
               INVALID KEY DISPLAY "S2-INVALID"
               NOT INVALID KEY DISPLAY "S2-NOT-INVALID"
           END-START
           DISPLAY "S2=" ST " " R-REC " " RL
           CLOSE R
           OPEN INPUT M
           DISPLAY "M-OPEN=" ST-M
           MOVE "99" TO ST-M
           MOVE 1 TO MK
           START M KEY IS EQUAL TO MK
               INVALID KEY DISPLAY "S3-INVALID"
               NOT INVALID KEY DISPLAY "S3-NOT-INVALID"
           END-START
           DISPLAY "S3=" ST-M
           CLOSE M
           STOP RUN.
