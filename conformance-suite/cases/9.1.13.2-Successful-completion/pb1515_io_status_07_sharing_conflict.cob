      *>--- conformance-case
      *> id: pb1515_io_status_07_sharing_conflict
      *> rules: [GR-9.1.13.2-6, GR-14.9.27.4-11, GR-14.9.27.4-25,
      *>    GR-9.1.13.9-1, GR-14.9.49.4-6, DOC-A.1-77, DOC-A.1-131]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1515SC.
      *> kb/Work PB1515. AN OPEN ... WITH NO REWIND THAT THE FILE
      *> SHARING RULES REFUSE ANSWERS '61', NEVER '07'. ISO 14.9.27.4
      *> GR11 gives '07' to an OPEN that SUCCEEDS with the NO REWIND
      *> phrase on a non-reel/unit medium (9.1.13.2 item 6); a request
      *> Table 19 prints as Unsuccessful open is "a file sharing
      *> conflict condition ... because an OPEN statement is attempted
      *> on a physical file and that physical file is already open by
      *> another file connector in a manner that conflicts with this
      *> request" (9.1.13.9 item 1), and "If the execution of the OPEN
      *> statement is unsuccessful, the file is not affected"
      *> (14.9.27.4 GR25). F and G are two connectors of one physical
      *> file. With no SHARING phrase and no SHARING clause GR23 leaves
      *> the sharing mode to the implementor, and docs/CONFORMANCE.md
      *> (DOC-A.1-77, DOC-A.1-131) defines it: OPEN INPUT opens SHARING
      *> WITH READ ONLY and OPEN OUTPUT SHARING WITH NO OTHER. Table 19
      *> then prints, for a request against the most restrictive
      *> existing mode and open mode: INPUT against OUTPUT with NO
      *> OTHER, Unsuccessful open; OUTPUT against any, Unsuccessful
      *> open; INPUT against INPUT with READ ONLY, Normal open.
      *> The file sharing mechanism is a COBOL-2002 addition, so this
      *> test starts at 2002. USE counts the Format 1 declarative's
      *> runs (14.9.49.4 GR6: after an unsuccessful operation).
      *>   OUT      00 USE 0  F creates the file, then holds it.
      *>   SHARE-1  61 USE 1  G INPUT WITH NO REWIND against F's OUTPUT.
      *>   CLOSE-NR 07 USE 1  F CLOSE WITH NO REWIND (Table 14 cell
      *>                      c,g) closes F and gives the file back.
      *>   G-NR     07 USE 1  the refused G is a closed connector
      *>                      still ("not affected"), so G's OPEN now
      *>                      succeeds and GR11 gives the '07'.
      *>   SHARE-2  61 USE 2  F OUTPUT WITH NO REWIND against G's
      *>                      INPUT: Unsuccessful open, so the file is
      *>                      not created anew and not emptied.
      *>   READ     00 AAAA   G still reads the record F wrote.
      *>   CLOSE    00 USE 2  G's plain CLOSE.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb1515sc.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS WS-F.
           SELECT G ASSIGN TO "pb1515sc.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS WS-G.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 F-REC PIC X(4).
       FD G.
       01 G-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 WS-F PIC XX.
       01 WS-G PIC XX.
       01 WS-USE PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       IO-ERR SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON F G.
       IO-ERR-P.
           ADD 1 TO WS-USE.
       END DECLARATIVES.
       MAIN SECTION.
       M1.
           OPEN OUTPUT F
           MOVE "AAAA" TO F-REC
           WRITE F-REC
           DISPLAY "OUT=" WS-F " USE=" WS-USE
           OPEN INPUT G WITH NO REWIND
           DISPLAY "SHARE-1=" WS-G " USE=" WS-USE
           CLOSE F WITH NO REWIND
           DISPLAY "CLOSE-NR=" WS-F " USE=" WS-USE
           OPEN INPUT G WITH NO REWIND
           DISPLAY "G-NR=" WS-G " USE=" WS-USE
           OPEN OUTPUT F WITH NO REWIND
           DISPLAY "SHARE-2=" WS-F " USE=" WS-USE
           READ G
           DISPLAY "READ=" WS-G " " G-REC " USE=" WS-USE
           CLOSE G
           DISPLAY "CLOSE=" WS-G " USE=" WS-USE
           STOP RUN.
