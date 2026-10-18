      *>--- conformance-case
      *> id: pb1515_io_warning_07_raises_ec
      *> rules: [GR-9.1.13.2-6, GR-14.9.49.4-3, GR-14.9.6.4-1,
      *>    GR-14.9.6.4-3, GR-7.3.25.4-4, DOC-A.1-103]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
       >>TURN EC-I-O-WARNING CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1515EW.
      *> kb/Work PB1515. THE EXCEPTION CONDITION I-O STATUS '07'
      *> RAISES, AND WHICH STATEMENTS RAISE IT. ISO 9.1.13.1: "The
      *> exception-name for successful completion with an I-O status
      *> value that is not '00' is EC-I-O-WARNING", and "If ... the
      *> I-O status value starts with a '0' and the second character
      *> is not '0', the associated exception condition is set to
      *> exist"; 7.3.25.4 GR4 turns it on only by naming it, which the
      *> directive above does. The Format 3 declarative below
      *> (14.9.49.4 GR3 e, a level-3 exception-name with no file-name)
      *> then runs after every statement that leaves a '0x' other than
      *> '00' - and after no other. '07' is set by OPEN and CLOSE alone
      *> (9.1.13.2 item 6, Annex E.2 item 16), so only those statements
      *> can run it, whether or not the file has a FILE STATUS clause
      *> (G has none) and whether or not the statement succeeded.
      *> EVERY EXPECTED VALUE IS COMPUTED FROM THOSE RULES. W counts
      *> the declarative's runs:
      *>   OUT      00 W 0  OPEN OUTPUT, no phrase: '00' raises nothing.
      *>   WRITE    00 W 0  a successful WRITE leaves '00'.
      *>   CLOSE-NR 07 W 1  CLOSE WITH NO REWIND on a file that is not
      *>                    on a reel/unit medium (Table 14 cell c,g).
      *>   IN-NR    07 W 2  OPEN INPUT WITH NO REWIND (14.9.27.4 GR11).
      *>   READ     00 W 2  a READ leaves '00'; the OPEN's '07' is gone.
      *>   REEL     07 W 3  CLOSE REEL, the file stays open (GR3 e).
      *>   CLOSE    00 W 3  a plain CLOSE.
      *>   NR-NOTOP 42 W 3  CLOSE WITH NO REWIND of a file that is not
      *>                    open: unsuccessful (14.9.6.4 GR1), an
      *>                    EC-I-O-LOGIC-ERROR condition and NOT a
      *>                    warning, so this declarative does not run
      *>                    and F's FILE STATUS clause equips the
      *>                    program to see the failure, so the run
      *>                    unit continues (9.1.13.1 leaves the action
      *>                    to the implementor: DOC-A.1-103).
      *>   G-OUT-NR 07 W 4  G has no FILE STATUS clause: the exception
      *>                    still exists and the declarative still runs;
      *>                    only the status item is missing, so F's
      *>                    item is unchanged and shown for the proof.
      *>   G-UNIT   W 5     CLOSE UNIT FOR REMOVAL, the file stays open.
      *>   G-CLOSE  W 6     CLOSE WITH NO REWIND, the file is closed.
      *>   G-PLAIN  W 6     a plain OPEN INPUT and a plain CLOSE of G
      *>                    leave '00' and raise nothing.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb1515ew.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS WS-F.
           SELECT G ASSIGN TO "pb1515ewg.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 F-REC PIC X(4).
       FD G.
       01 G-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 WS-F PIC XX.
       01 WS-W PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       WARN SECTION.
           USE AFTER EXCEPTION CONDITION EC-I-O-WARNING.
       WARN-P.
           ADD 1 TO WS-W.
           DISPLAY "DECL " FUNCTION EXCEPTION-STATUS.
       END DECLARATIVES.
       MAIN SECTION.
       M1.
           OPEN OUTPUT F
           DISPLAY "OUT=" WS-F " W=" WS-W
           MOVE "AAAA" TO F-REC
           WRITE F-REC
           DISPLAY "WRITE=" WS-F " W=" WS-W
           CLOSE F WITH NO REWIND
           DISPLAY "CLOSE-NR=" WS-F " W=" WS-W
           OPEN INPUT F WITH NO REWIND
           DISPLAY "IN-NR=" WS-F " W=" WS-W
           READ F
           DISPLAY "READ=" WS-F " " F-REC " W=" WS-W
           CLOSE F REEL
           DISPLAY "REEL=" WS-F " W=" WS-W
           CLOSE F
           DISPLAY "CLOSE=" WS-F " W=" WS-W
           CLOSE F WITH NO REWIND
           DISPLAY "NR-NOTOPEN=" WS-F " W=" WS-W
           OPEN OUTPUT G WITH NO REWIND
           DISPLAY "G-OUT-NR F=" WS-F " W=" WS-W
           MOVE "BBBB" TO G-REC
           WRITE G-REC
           CLOSE G UNIT FOR REMOVAL
           DISPLAY "G-UNIT W=" WS-W
           CLOSE G WITH NO REWIND
           DISPLAY "G-CLOSE W=" WS-W
           OPEN INPUT G
           CLOSE G
           DISPLAY "G-PLAIN W=" WS-W
           STOP RUN.
