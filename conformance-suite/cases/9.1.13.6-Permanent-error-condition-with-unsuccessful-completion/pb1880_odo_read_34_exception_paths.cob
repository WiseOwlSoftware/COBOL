*>--- conformance-case
*> id: pb1880_odo_read_34_exception_paths
*> rules: [GR-9.1.13.6-4]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 1989:2023 §9.1.13.6 item 4 b): the '34' of a READ whose OCCURS DEPENDING ON item makes the record too long,
      *> through every way a program can be equipped (or not) to see it. The record is a 2-byte header plus a table of
      *> 1 TO 10 characters counted by the header's own field; the file description implies 12 bytes (§13.18.43.4 GR5 a)),
      *> so the second record on the medium (count 11, 13 bytes) is the one that fails, and the third (count 03) is a
      *> legal record that fails too because the permanent error REMAINS IN EFFECT (§9.1.13.1) until a CLOSE.
      *>   EU  no FILE STATUS clause, a USE procedure for the file: the declarative runs once for each unsuccessful
      *>       READ (the second and the third), and execution continues after the READ (Annex A.1 item 103,
      *>       docs/CONFORMANCE.md DOC-A.1-103: "execution continues if the file connector has a FILE STATUS clause or a
      *>       USE procedure applied to the statement").
      *>   EB  both a FILE STATUS clause and the USE procedure: the procedure runs and sees '34' in the status item.
      *>   RU  the same for a RELATIVE file (the keyed READ arm), USE procedure only: the failing record is slot 2.
      *>   EN  neither a FILE STATUS clause nor a USE procedure (and an AT END phrase, which covers only the at-end
      *>       condition, §9.1.13.4): the status is fatal (§9.1.13.1: a status beginning with 3 is a fatal exception
      *>       condition) and nothing is equipped to see it, so the run unit terminates abnormally at that READ - the
      *>       line "NOT REACHED" is never displayed.
      *>   SP  a FILE STATUS clause and an AT END / NOT AT END pair (no USE procedure) on the sequential file; RP the same
      *>       with an INVALID KEY / NOT INVALID KEY pair on the RELATIVE file. A '34' is neither the at end condition
      *>       (§9.1.13.4) nor the invalid key condition (§9.1.13.5), so the FILE STATUS clause sees it, execution
      *>       continues after the READ, and NEITHER phrase of either pair runs (the AT END and INVALID KEY phrases are
      *>       for their own conditions, §14.9.30.4 GR13): a '34' that reached the AT END or INVALID KEY imperative would
      *>       print an "SPn AE" / "RPn IK" line.
      *> In every unsuccessful READ the NOT AT END (or NOT INVALID KEY) phrase is ignored (§9.1.13.1: "Any NOT AT END or
      *> NOT INVALID KEY phrase specified for that statement is ignored").
      *> WHY EACH LEG CAN FAIL: a '34' that skipped the declarative would print no USE line; one that ran the NOT AT END
      *> phrase would print an NAE line for the failing READ; one that let the run unit continue at EN would print
      *> NOT REACHED; one that took the AT END or INVALID KEY phrase for a '34' would print SP2 AE or RP2 IK; one that forgot the condition in effect would run the declarative only once per file.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1880EX.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT EU ASSIGN TO "pb1880ex.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT EB ASSIGN TO "pb1880ex.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS FSB.
           SELECT EN ASSIGN TO "pb1880ex.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT EW ASSIGN TO "pb1880ex.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT SP ASSIGN TO "pb1880ex.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS FSP.
           SELECT RP ASSIGN TO "pb1880exr.dat"
               ORGANIZATION IS RELATIVE ACCESS MODE IS RANDOM
               RELATIVE KEY IS RKP FILE STATUS IS FSR.
           SELECT RU ASSIGN TO "pb1880exr.dat"
               ORGANIZATION IS RELATIVE ACCESS MODE IS RANDOM
               RELATIVE KEY IS RKU.
           SELECT RW ASSIGN TO "pb1880exr.dat"
               ORGANIZATION IS RELATIVE ACCESS MODE IS RANDOM
               RELATIVE KEY IS RKW.
       DATA DIVISION.
       FILE SECTION.
       FD  EU.
       01  EU-R.
           05 EU-N PIC 99.
           05 EU-T PIC X OCCURS 1 TO 10 TIMES DEPENDING ON EU-N.
       FD  EB.
       01  EB-R.
           05 EB-N PIC 99.
           05 EB-T PIC X OCCURS 1 TO 10 TIMES DEPENDING ON EB-N.
       FD  EN.
       01  EN-R.
           05 EN-N PIC 99.
           05 EN-T PIC X OCCURS 1 TO 10 TIMES DEPENDING ON EN-N.
       FD  EW.
       01  EW-R PIC X(12).
       FD  SP.
       01  SP-R.
           05 SP-N PIC 99.
           05 SP-T PIC X OCCURS 1 TO 10 TIMES DEPENDING ON SP-N.
       FD  RP.
       01  RP-R.
           05 RP-N PIC 99.
           05 RP-T PIC X OCCURS 1 TO 10 TIMES DEPENDING ON RP-N.
       FD  RU.
       01  RU-R.
           05 RU-N PIC 99.
           05 RU-T PIC X OCCURS 1 TO 10 TIMES DEPENDING ON RU-N.
       FD  RW.
       01  RW-R PIC X(12).
       WORKING-STORAGE SECTION.
       01  FSB PIC XX.
       01  FSP PIC XX.
       01  FSR PIC XX.
       01  RKP PIC 9.
       01  RKU PIC 9.
       01  RKW PIC 9.
       PROCEDURE DIVISION.
       DECLARATIVES.
       USE-EU SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON EU.
       USE-EU-P.
           DISPLAY "USE-EU".
       USE-EB SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON EB.
       USE-EB-P.
           DISPLAY "USE-EB FSB=" FSB.
       USE-RU SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON RU.
       USE-RU-P.
           DISPLAY "USE-RU".
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           OPEN OUTPUT EW.
           MOVE "03ABCDEFGHIJ" TO EW-R.
           WRITE EW-R.
           MOVE "11ABCDEFGHIJ" TO EW-R.
           WRITE EW-R.
           MOVE "03ABCDEFGHIJ" TO EW-R.
           WRITE EW-R.
           CLOSE EW.
           OPEN INPUT EU.
           READ EU NOT AT END DISPLAY "EU1 NAE N=" EU-N END-READ.
           READ EU NOT AT END DISPLAY "EU2 NAE" END-READ.
           READ EU NOT AT END DISPLAY "EU3 NAE" END-READ.
           CLOSE EU.
           OPEN INPUT EB.
           READ EB NOT AT END DISPLAY "EB1 NAE N=" EB-N END-READ.
           DISPLAY "EB1 " FSB.
           READ EB NOT AT END DISPLAY "EB2 NAE" END-READ.
           DISPLAY "EB2 " FSB.
           READ EB NOT AT END DISPLAY "EB3 NAE" END-READ.
           DISPLAY "EB3 " FSB.
           CLOSE EB.
           DISPLAY "EB-CLOSE " FSB.
           OPEN OUTPUT RW.
           MOVE 1 TO RKW.
           MOVE "03ABCDEFGHIJ" TO RW-R.
           WRITE RW-R.
           MOVE 2 TO RKW.
           MOVE "11ABCDEFGHIJ" TO RW-R.
           WRITE RW-R.
           CLOSE RW.
           OPEN INPUT RU.
           MOVE 1 TO RKU.
           READ RU NOT INVALID KEY DISPLAY "RU1 NIK N=" RU-N END-READ.
           MOVE 2 TO RKU.
           READ RU NOT INVALID KEY DISPLAY "RU2 NIK" END-READ.
           MOVE 1 TO RKU.
           READ RU NOT INVALID KEY DISPLAY "RU3 NIK" END-READ.
           CLOSE RU.
           OPEN INPUT SP.
           READ SP AT END DISPLAY "SP1 AE" NOT AT END DISPLAY "SP1 NAE N=" SP-N END-READ.
           DISPLAY "SP1 " FSP.
           READ SP AT END DISPLAY "SP2 AE" NOT AT END DISPLAY "SP2 NAE" END-READ.
           DISPLAY "SP2 " FSP.
           READ SP AT END DISPLAY "SP3 AE" NOT AT END DISPLAY "SP3 NAE" END-READ.
           DISPLAY "SP3 " FSP.
           CLOSE SP.
           OPEN INPUT RP.
           MOVE 1 TO RKP.
           READ RP INVALID KEY DISPLAY "RP1 IK" NOT INVALID KEY DISPLAY "RP1 NIK N=" RP-N END-READ.
           DISPLAY "RP1 " FSR.
           MOVE 2 TO RKP.
           READ RP INVALID KEY DISPLAY "RP2 IK" NOT INVALID KEY DISPLAY "RP2 NIK" END-READ.
           DISPLAY "RP2 " FSR.
           MOVE 1 TO RKP.
           READ RP INVALID KEY DISPLAY "RP3 IK" NOT INVALID KEY DISPLAY "RP3 NIK" END-READ.
           DISPLAY "RP3 " FSR.
           CLOSE RP.
           OPEN INPUT EN.
           READ EN AT END DISPLAY "EN AT END" NOT AT END DISPLAY "EN1 NAE N=" EN-N END-READ.
           READ EN AT END DISPLAY "EN AT END" NOT AT END DISPLAY "EN2 NAE" END-READ.
           DISPLAY "NOT REACHED".
           CLOSE EN.
           STOP RUN.
