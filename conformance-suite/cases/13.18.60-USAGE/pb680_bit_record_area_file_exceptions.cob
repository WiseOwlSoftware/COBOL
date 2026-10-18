*>--- conformance-case
*> id: pb680_bit_record_area_file_exceptions
*> rules: [GR-13.18.60.4-5, GR-9.1.13.4-1, GR-9.1.13.5-2, GR-9.1.13.5-3, GR-9.1.14-2, GR-9.1.14-3, GR-14.9.30.4-18, GR-14.9.30.4-29, GR-14.9.30.4-32, GR-14.9.35.4-14, GR-14.9.41.4-9, GR-14.9.51.4-15]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB680, the ABNORMAL half - the exception paths of a file whose record area is a USAGE BIT item
      *> (13.18.60.4 GR5; the success half is pb680_bit_record_area_file_io). A bit record is as wide as the bits
      *> fill, ceil(bits / 8) characters (8.5.1.6.3; 8 bits to the character is the 8.1.2 implementor choice,
      *> CONFORMANCE.md), so the 16-bit record below is TWO characters wide and the one-character physical file
      *> written through a PIC X connector is SHORTER than that record. Each leg states the rule that gives its
      *> I-O status; none of the values is read from a run.
      *>
      *>   X1  OPEN INPUT of the existing one-byte file: 00 (9.1.13.2 item 1).
      *>   X2  READ of that one byte into the two-byte bit record: "the physical record from the file is shorter
      *>       than or longer than the minimum or maximum length of records allowed for the fixed file
      *>       attributes" -> 04 (9.1.13.2 item 3). Were the record one character wide (the carrier truncation
      *>       PB680 records) the read would be 00 and the next READ would end the file.
      *>   X3  the next READ finds no next logical record: AT END, 10 (9.1.13.4 item 1), and the AT END phrase runs.
      *>   X4  READ on the now CLOSED connector: 47 (9.1.13.7 item 7).
      *>   X5  WRITE on a connector that is not open in the extend or output mode: 48 (9.1.13.7 item 8 a).
      *>   X6  CLOSE on a connector that is not open: 42 (9.1.13.7 item 2).
      *>   X7  OPEN of a connector already open: 41 (9.1.13.7 item 1).
      *>   X8  WRITE on a connector open INPUT: 48 again; the record is not written.
      *>   X9  after X8 the file is untouched, so a fresh read is the same short read: 04.
      *>   X10 a second READ, now of the file's end: the AT END condition, 10 (9.1.13.4 item 1), and the AT END
      *>       phrase runs (X10-ATEND).
      *>   X10B the READ after that at-end READ: "The preceding READ statement referencing that file connector was
      *>       unsuccessful" -> 46 (9.1.13.7 item 6 b), not a second AT END (X10B-NOVALID).
      *>   X11 OPEN INPUT of a physical file that is not present and not OPTIONAL: 35 (9.1.13.6 item 5).
      *>   X12 AT END phrase specified: it runs and "no other applicable exception processing statements shall be
      *>       executed" (9.1.13.1), so the USE declarative of that file does NOT run: N stays 0.
      *>   X13 the declarative count after X12 (N=0, the proof that the phrase displaced the declarative).
      *>   X14 the same at-end condition with NO AT END phrase: the USE declarative IS the applicable exception
      *>       processing statement (9.1.12 item 2), so it runs once: N = 1. Status 1x is not fatal (9.1.13.1), so
      *>       execution continues.
      *> The 3x and 4x statuses are fatal exception conditions (9.1.13.1); the implementor chooses the action when
      *> no applicable statement exists, and WiseOwl COBOL continues when the file has a FILE STATUS clause
      *> (CONFORMANCE.md A.1 item 103), which FB and FM have.
      *> Every status has a FILE STATUS data item except FDD, whose absence is what makes the USE declarative
      *> the only observer.
      *>
      *> THE INVALID KEY HALF. A RELATIVE file FR and an INDEXED file FI whose record area is a USAGE BIT item are
      *> driven to each invalid key condition they can answer. 9.1.14: when the condition occurs "the file is not
      *> affected"; with an INVALID KEY phrase "any applicable exception processing statements are not executed"
      *> and the phrase runs; without one the USE declarative on the file runs and control then goes to the end of
      *> the statement. The declarative below counts into ND, so a leg with the phrase leaves ND alone and a leg
      *> without it adds 1. An unsuccessful WRITE or REWRITE leaves "the content of the record area unaffected"
      *> (14.9.51.4 GR15, 14.9.35.4 GR14): the bit record DISPLAYs the value MOVEd to it before the statement.
      *>
      *>   Y1  WRITE of relative key 3 again, INVALID KEY phrase: 22 (9.1.13.5 item 2 a); phrase runs; ND stays 0;
      *>       the bit record area still shows what was moved.
      *>   Y2  the same WRITE with no phrase: 22; the declarative runs: ND = 1.
      *>   Y3  random READ of relative key 2, which is not in the file: 23 (9.1.13.5 item 3 a, 14.9.30.4 GR29);
      *>       INVALID KEY runs, NOT INVALID KEY does not; ND stays 1.
      *>   Y4  the same READ with no phrase: 23; ND = 2.
      *>   Y5  REWRITE of key 2 (14.9.35.4 GR21): 23; the phrase runs; the record area is unaffected (GR14).
      *>   Y6  START with key 5 (14.9.41.4 GR9 c): 23. Then READ of key 1 and key 3: the file kept its ORIGINAL
      *>       records (the duplicate WRITE did not replace key 3, the REWRITE did not create key 2): 00 with
      *>       NOT INVALID KEY running (9.1.14, the condition does not exist); READ NEXT after key 3: AT END, 10.
      *>   Z1  WRITE of the prime key "AB" again: 22 (9.1.13.5 item 2 b), phrase runs, record area unaffected.
      *>   Z2  the same with no phrase: 22; the declarative runs: ND = 3.
      *>   Z3  random READ of key "ZZ" (14.9.30.4 GR32): 23; the phrase runs. Z4 the same with no phrase: 23, ND = 4.
      *>   Z5  REWRITE of key "ZZ" (IK is set again first: after the unsuccessful READs Z3 and Z4 the record area is undefined, 14.9.30.4 GR18): 23, record area unaffected. Z6 START with key "ZZ": 23.
      *>   Z7  READ of key "AB" returns the original record, not the duplicate WRITE's; READ NEXT: AT END, 10.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB680BITEX.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FB ASSIGN TO "pb680_ex.dat" ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS ST-B.
           SELECT FW ASSIGN TO "pb680_ex.dat" ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS ST-W.
           SELECT FM ASSIGN TO "pb680_missing.dat" ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS ST-M.
           SELECT FDD ASSIGN TO "pb680_decl.dat" ORGANIZATION IS SEQUENTIAL.
           SELECT FR ASSIGN TO "pb680_exr.dat" ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC RELATIVE KEY IS RK FILE STATUS IS ST-R.
           SELECT FI ASSIGN TO "pb680_exi.dat" ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC RECORD KEY IS IK FILE STATUS IS ST-I.
       DATA DIVISION.
       FILE SECTION.
       FD FB.
       01 BR PIC 1(16) USAGE BIT.
       FD FW.
       01 WR PIC X(1).
       FD FM.
       01 MR PIC 1(8) USAGE BIT.
       FD FDD.
       01 DR PIC 1(8) USAGE BIT.
       FD FR.
       01 RR PIC 1(12) USAGE BIT.
       FD FI.
       01 IR.
          05 IK PIC X(2).
          05 IB PIC 1(8) USAGE BIT.
       WORKING-STORAGE SECTION.
       01 ST-B PIC XX.
       01 ST-W PIC XX.
       01 ST-M PIC XX.
       01 ST-R PIC XX.
       01 ST-I PIC XX.
       01 RK PIC 9(2).
       01 N PIC 9 VALUE 0.
       01 ND PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D1 SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON FDD.
       D1P.
           ADD 1 TO N.
           DISPLAY "DECL".
       D2 SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON FR FI.
       D2P.
           ADD 1 TO ND.
           DISPLAY "DECL2".
       END DECLARATIVES.
       MAIN SECTION.
           OPEN OUTPUT FW.
           MOVE "A" TO WR.
           WRITE WR.
           CLOSE FW.
           OPEN INPUT FB.
           DISPLAY "X1-OPEN=" ST-B.
           READ FB.
           DISPLAY "X2-SHORT=" ST-B.
           READ FB AT END DISPLAY "X3-ATEND=" ST-B.
           CLOSE FB.
           READ FB.
           DISPLAY "X4-READ-CLOSED=" ST-B.
           WRITE BR.
           DISPLAY "X5-WRITE-CLOSED=" ST-B.
           CLOSE FB.
           DISPLAY "X6-CLOSE-CLOSED=" ST-B.
           OPEN INPUT FB.
           OPEN INPUT FB.
           DISPLAY "X7-REOPEN=" ST-B.
           MOVE B"0000000011111111" TO BR.
           WRITE BR.
           DISPLAY "X8-WRITE-INPUT=" ST-B.
           CLOSE FB.
           OPEN INPUT FB.
           READ FB.
           DISPLAY "X9-REREAD=" ST-B.
           READ FB AT END DISPLAY "X10-ATEND=" ST-B.
           READ FB.
           DISPLAY "X10B-NOVALID=" ST-B.
           CLOSE FB.
           OPEN INPUT FM.
           DISPLAY "X11-MISSING=" ST-M.
           OPEN OUTPUT FDD.
           MOVE B"01000001" TO DR.
           WRITE DR.
           CLOSE FDD.
           OPEN INPUT FDD.
           READ FDD.
           READ FDD AT END DISPLAY "X12-PHRASE".
           DISPLAY "X13-N=" N.
           CLOSE FDD.
           OPEN INPUT FDD.
           READ FDD.
           READ FDD.
           DISPLAY "X14-N=" N.
           CLOSE FDD.
      *> ===== Y - RELATIVE file, 12-bit record area =====
           OPEN OUTPUT FR.
           MOVE 1 TO RK.
           MOVE B"010000010101" TO RR.
           WRITE RR.
           MOVE 3 TO RK.
           MOVE B"111100001111" TO RR.
           WRITE RR.
           DISPLAY "Y0-W=" ST-R.
           MOVE B"000011110000" TO RR.
           WRITE RR INVALID KEY DISPLAY "Y1-INVALID=" ST-R "[" RR "]"
               NOT INVALID KEY DISPLAY "Y1-NOT".
           DISPLAY "Y1-ND=" ND.
           WRITE RR.
           DISPLAY "Y2-DUP=" ST-R "[" RR "] ND=" ND.
           CLOSE FR.
           OPEN I-O FR.
           MOVE 2 TO RK.
           READ FR INVALID KEY DISPLAY "Y3-INVALID=" ST-R
               NOT INVALID KEY DISPLAY "Y3-NOT".
           DISPLAY "Y3-ND=" ND.
           READ FR.
           DISPLAY "Y4-NOKEY=" ST-R " ND=" ND.
           MOVE B"101010100101" TO RR.
           REWRITE RR INVALID KEY DISPLAY "Y5-INVALID=" ST-R "[" RR "]"
               NOT INVALID KEY DISPLAY "Y5-NOT".
           MOVE 5 TO RK.
           START FR KEY IS EQUAL TO RK INVALID KEY
               DISPLAY "Y6-START=" ST-R.
           MOVE 1 TO RK.
           MOVE ZERO TO RR.
           READ FR INVALID KEY DISPLAY "Y6-R1-INVALID"
               NOT INVALID KEY DISPLAY "Y6-R1=" ST-R "[" RR "]".
           MOVE 2 TO RK.
           READ FR INVALID KEY DISPLAY "Y6-R2=" ST-R.
           MOVE 3 TO RK.
           READ FR INVALID KEY DISPLAY "Y6-R3-INVALID"
               NOT INVALID KEY DISPLAY "Y6-R3=" ST-R "[" RR "]".
           READ FR NEXT AT END DISPLAY "Y6-END=" ST-R.
           DISPLAY "Y6-ND=" ND.
           CLOSE FR.
      *> ===== Z - INDEXED file, 2-character key then an 8-bit leaf =====
           OPEN OUTPUT FI.
           MOVE "AB" TO IK.
           MOVE B"01000011" TO IB.
           WRITE IR.
           MOVE "AA" TO IK.
           MOVE B"11110000" TO IB.
           WRITE IR.
           DISPLAY "Z0-W=" ST-I.
           MOVE "AB" TO IK.
           MOVE B"00000001" TO IB.
           WRITE IR INVALID KEY DISPLAY "Z1-INVALID=" ST-I "[" IB "]"
               NOT INVALID KEY DISPLAY "Z1-NOT".
           WRITE IR.
           DISPLAY "Z2-DUP=" ST-I "[" IB "] ND=" ND.
           CLOSE FI.
           OPEN I-O FI.
           MOVE "ZZ" TO IK.
           READ FI INVALID KEY DISPLAY "Z3-INVALID=" ST-I
               NOT INVALID KEY DISPLAY "Z3-NOT".
           READ FI.
           DISPLAY "Z4-NOKEY=" ST-I " ND=" ND.
           MOVE "ZZ" TO IK.
           MOVE B"00001111" TO IB.
           REWRITE IR INVALID KEY DISPLAY "Z5-INVALID=" ST-I "[" IB "]"
               NOT INVALID KEY DISPLAY "Z5-NOT".
           START FI KEY IS EQUAL TO IK INVALID KEY
               DISPLAY "Z6-START=" ST-I.
           MOVE "AB" TO IK.
           MOVE ZERO TO IB.
           READ FI INVALID KEY DISPLAY "Z7-INVALID"
               NOT INVALID KEY DISPLAY "Z7-R=" ST-I "[" IB "]".
           READ FI NEXT AT END DISPLAY "Z7-END=" ST-I.
           DISPLAY "Z7-ND=" ND.
           CLOSE FI.
           STOP RUN.
