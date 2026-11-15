*>--- conformance-case
*> id: pb5612_sort_fatal_io_propagates_2002
*> rules: [2002-GR-7.2.17.3-1]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB5612 (VCR row 7.80).  §2002:7.2.17.3 GR1 (cite.py --check): "When the ON phrase is specified or implied, automatic
      *> propagation of exception conditions becomes enabled for functions, methods, and programs that follow in the compilation
      *> group."  PB5612PS22's SORT USING a missing file leaves the implicit OPEN's I-O status '35', the fatal
      *> EC-I-O-PERMANENT-ERROR, with EC-I-O checking on and no declarative in PB5612PS22.  §2002:14.5.12.1.2 3) (cite.py --check):
      *> "the exception condition is propagated as if a GOBACK statement with the RAISING LAST EXCEPTION phrase were executed",
      *> so the caller's declarative runs ("MAIN CAUGHT") and, completing normally, ends the run unit (item 2): "MAIN AFTER" and
      *> "SSUB CONTINUED" are never displayed.
      >>TURN EC-I-O CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5612PM2.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D1 SECTION.
           USE AFTER EXCEPTION CONDITION EC-I-O-PERMANENT-ERROR.
       D1P.
           DISPLAY "MAIN CAUGHT " FUNCTION EXCEPTION-STATUS.
       END DECLARATIVES.
       M SECTION.
       M1.
           DISPLAY "MAIN START"
           CALL "PB5612PS2"
           DISPLAY "MAIN AFTER"
           STOP RUN.
       END PROGRAM PB5612PM2.
      >>PROPAGATE ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5612PS2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT INF ASSIGN TO "pb5612-nosuch2-in.dat"
               ORGANIZATION SEQUENTIAL
               FILE STATUS IS FS.
           SELECT OUTF ASSIGN TO "pb5612-sort2-out.dat"
               ORGANIZATION SEQUENTIAL.
           SELECT SF ASSIGN TO "pb5612-sort2wk".
       DATA DIVISION.
       FILE SECTION.
       FD INF.
       01 IR PIC X(4).
       FD OUTF.
       01 OR1 PIC X(4).
       SD SF.
       01 SR.
          05 SK PIC X(4).
       WORKING-STORAGE SECTION.
       01 FS PIC XX.
       PROCEDURE DIVISION.
           SORT SF ON ASCENDING KEY SK USING INF GIVING OUTF
           DISPLAY "SSUB CONTINUED FS=" FS
           GOBACK.
       END PROGRAM PB5612PS2.
