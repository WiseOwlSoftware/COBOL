*>--- conformance-case
*> id: pb680_bit_record_area_file_exceptions
*> rules: [GR-13.18.60.4-5, GR-9.1.13.4-1]
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
      *>   X10 the READ after the at-end READ: "The preceding READ statement referencing that file connector was
      *>       unsuccessful" -> 46 (9.1.13.7 item 6 b), not a second AT END.
      *>   X11 OPEN INPUT of a physical file that is not present and not OPTIONAL: 35 (9.1.13.6 item 5).
      *>   X12 AT END phrase specified: it runs and "no other applicable exception processing statements shall be
      *>       executed" (9.1.13.1), so the USE declarative of that file does NOT run: N stays 0.
      *>   X14 the same at-end condition with NO AT END phrase: the USE declarative IS the applicable exception
      *>       processing statement (9.1.12 item 2), so it runs once: N = 1. Status 1x is not fatal (9.1.13.1), so
      *>       execution continues.
      *> The 3x and 4x statuses are fatal exception conditions (9.1.13.1); the implementor chooses the action when
      *> no applicable statement exists, and WiseOwl COBOL continues when the file has a FILE STATUS clause
      *> (CONFORMANCE.md A.1 item 103), which FB and FM have.
      *> Every status has a FILE STATUS data item except FDD, whose absence is what makes the USE declarative
      *> the only observer.
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
       WORKING-STORAGE SECTION.
       01 ST-B PIC XX.
       01 ST-W PIC XX.
       01 ST-M PIC XX.
       01 N PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D1 SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON FDD.
       D1P.
           ADD 1 TO N.
           DISPLAY "DECL".
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
           STOP RUN.
