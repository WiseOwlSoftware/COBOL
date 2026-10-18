      *>--- conformance-case
      *> id: pb870_start_first_last_relative_exceptions
      *> rules: [GR-14.9.41.4-1, GR-14.9.41.4-2, GR-14.9.41.4-5,
      *>    GR-14.9.41.4-6, GR-14.9.41.4-7, GR-14.9.41.4-11,
      *>    GR-14.9.41.4-12, GR-9.1.14-2, GR-9.1.14-3, GR-9.1.14-4,
      *>    GR-9.1.14-L2.1, GR-9.1.13.7-6, GR-9.1.13.7-7,
      *>    GR-14.9.49.4-6, GR-14.9.49.4-7]
      *> accept: [2002, 2023]
      *> constructs: [start-first-last-2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB870 - START FIRST and START LAST on a RELATIVE file,
      *> the ABNORMAL and EXCEPTION arms. The sibling
      *> l1_start_first_last_relative walks the successful positions
      *> and the empty-file '23'; this program walks every way the same
      *> two statements can fail and what control does next.
      *> The rules (each quotation checked with scripts/spec/cite.py):
      *>   14.9.41.4 GR11/GR12 - "If no records exist in the file, the
      *>     invalid key condition exists and the execution of the
      *>     START statement is unsuccessful."
      *>   14.9.41.4 GR1  - "The open mode of the file connector
      *>     referenced by file-name-1 shall be input or I-O."; 9.1.13.7
      *>     7) makes the violation I-O status 47: "The execution of a
      *>     READ or START statement is attempted referencing a file
      *>     connector that is not open in the input or I-O mode."
      *>   14.9.41.4 GR2  - the record area is not altered.
      *>   14.9.41.4 GR5  - "the file position indicator indicates that
      *>     an optional input file is not present, the invalid key
      *>     condition exists"; 9.1.13.5 3) b) gives its status, 23.
      *>   14.9.41.4 GR7  - "Following the unsuccessful execution of a
      *>     START statement, the file position indicator is set to
      *>     indicate that no valid record position has been
      *>     established."; 9.1.13.7 6) a) reads it back as 46 on the
      *>     next sequential READ.
      *>   9.1.14 2) - with an INVALID KEY phrase "any applicable
      *>     exception processing statements are not executed";
      *>   9.1.14 3) - without one they ARE, "If control is returned
      *>     from those statements or there are no applicable exception
      *>     processing statements, control is transferred to the end of
      *>     the input-output statement"; 9.1.14 4) - with neither the
      *>     control goes straight to the end of the statement;
      *>   9.1.14 (the unsuccessful completion that is NOT an invalid
      *>     key condition, item 1 of the second list) - the
      *>     exception processing statements run, the INVALID KEY and
      *>     NOT INVALID KEY phrases do not (47 is not one of the
      *>     9.1.13.5 values).
      *>   14.9.49.4 GR6 a) - a USE procedure naming the file runs
      *>     "unless an AT END or INVALID KEY phrase takes precedence";
      *>     GR7 b) - a non-fatal status returns control to the end of
      *>     the statement.
      *> WHY EACH LEG CAN FAIL. C0 is the control: a READ of the empty
      *> file after OPEN is the at end condition, 10. S3 repeats the READ
      *> after a failed START and must read 46 instead, so an emitter
      *> that does not invalidate the position on a failed START prints
      *> 10 there. S1 must run INVALID KEY and NOT the USE procedure, S2
      *> the reverse; S4 and S5 take neither phrase (47), S4 on a file
      *> open OUTPUT and S5 on a closed one; S6 is the absent OPTIONAL
      *> file; S7 is a file no USE procedure covers, which must simply
      *> continue with the status set.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB870EXC.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FE ASSIGN TO "pb870e.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS EK
               FILE STATUS IS ST-E.
           SELECT FW ASSIGN TO "pb870w.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS WK
               FILE STATUS IS ST-W.
           SELECT OPTIONAL FO ASSIGN TO "pb870o-absent.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS OK-K
               FILE STATUS IS ST-O.
           SELECT FN ASSIGN TO "pb870n.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS NK
               FILE STATUS IS ST-N.
       DATA DIVISION.
       FILE SECTION.
       FD FE.
       01 E-REC.
          05 E-VAL PIC X(4).
       FD FW.
       01 W-REC PIC X(4).
       FD FO.
       01 O-REC PIC X(4).
       FD FN.
       01 N-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 EK   PIC 9(4).
       01 WK   PIC 9(4).
       01 OK-K PIC 9(4).
       01 NK   PIC 9(4).
       01 ST-E PIC XX.
       01 ST-W PIC XX.
       01 ST-O PIC XX.
       01 ST-N PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       DSE SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON FE.
       DSE1.
           DISPLAY "DECL-E " ST-E.
       DSW SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON FW.
       DSW1.
           DISPLAY "DECL-W " ST-W.
       DSO SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON FO.
       DSO1.
           DISPLAY "DECL-O " ST-O.
       END DECLARATIVES.
       MAIN SECTION.
       M1.
      *> Two relative files with no records at all.
           OPEN OUTPUT FE
           CLOSE FE
           OPEN OUTPUT FN
           CLOSE FN
      *> ---- C0, the control: READ after OPEN is at end (10) ------
           OPEN INPUT FE
           READ FE NEXT
               AT END DISPLAY "C0 AT-END " ST-E
               NOT AT END DISPLAY "C0 NAE"
           END-READ
           CLOSE FE
      *> ---- S1: FIRST, INVALID KEY present (GR11, 9.1.14 2) -----
           OPEN INPUT FE
           DISPLAY "OPEN-E " ST-E
           MOVE "KEEP" TO E-VAL
           START FE FIRST
               INVALID KEY DISPLAY "S1 INVALID " ST-E
               NOT INVALID KEY DISPLAY "S1 NOT-INVALID " ST-E
           END-START
           DISPLAY "S1 AREA=" E-VAL
      *> ---- S2: LAST, no phrase: the USE procedure runs (GR12) --
           START FE LAST
           DISPLAY "S2 STATUS " ST-E
      *> ---- S3: the READ after a failed START is 46, not 10 -----
           READ FE NEXT
               AT END DISPLAY "S3 AT-END"
               NOT AT END DISPLAY "S3 NAE"
           END-READ
           DISPLAY "S3 STATUS " ST-E
           CLOSE FE
      *> ---- S4: FIRST on a file open OUTPUT (GR1, 47) ------------
           OPEN OUTPUT FW
           START FW FIRST
               INVALID KEY DISPLAY "S4 INVALID"
               NOT INVALID KEY DISPLAY "S4 NOT-INVALID"
           END-START
           DISPLAY "S4 STATUS " ST-W
           CLOSE FW
      *> ---- S5: LAST on a file that is not open (GR1, 47) -------
           START FW LAST
               INVALID KEY DISPLAY "S5 INVALID"
               NOT INVALID KEY DISPLAY "S5 NOT-INVALID"
           END-START
           DISPLAY "S5 STATUS " ST-W
      *> ---- S6: an absent OPTIONAL input file (GR5, 23) ----------
           OPEN INPUT FO
           DISPLAY "OPEN-O " ST-O
           START FO FIRST
               INVALID KEY DISPLAY "S6 INVALID " ST-O
               NOT INVALID KEY DISPLAY "S6 NOT-INVALID"
           END-START
           START FO LAST
           DISPLAY "S6 STATUS " ST-O
           CLOSE FO
      *> ---- S7: no USE procedure covers the file (9.1.14 4) -----
           OPEN INPUT FN
           START FN FIRST
           DISPLAY "S7 STATUS " ST-N
           CLOSE FN
           STOP RUN.
