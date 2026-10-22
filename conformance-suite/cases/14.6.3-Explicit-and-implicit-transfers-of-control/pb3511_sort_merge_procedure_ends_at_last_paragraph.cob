      *>--- conformance-case
      *> id: pb3511_sort_merge_procedure_ends_at_last_paragraph
      *> rules: [GR-14.6.3-1, GR-14.9.40.4-10, GR-14.9.40.4-11,
      *>     GR-14.9.40.4-13, GR-14.9.40.4-14, GR-14.9.24.4-8,
      *>     GR-14.9.24.4-9, GR-14.9.28.4-5]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> PB3511 - a SORT or MERGE input or output procedure, or a
      *>   PERFORM, whose range ENDS at the last paragraph of the source
      *>   element COMPLETES there: control passes the return mechanism
      *>   after that paragraph and resumes after the controlling
      *>   statement. It is not the implicit GOBACK of running off the
      *>   element (the companion cases
      *>   pb3511_implicit_goback_sort_output_procedure and
      *>   pb3511_implicit_goback_sort_merge_called_program).
      *> cite.py --check 14.6.3 "If a procedure is being executed under
      *>   control of another COBOL statement such as PERFORM, USE,
      *>   SORT, and MERGE, and the procedure is the last procedure in
      *>   the range of the controlling statement" -> OK  14.6.3 1)
      *> cite.py --check 14.9.40.4 "The compiler inserts a return
      *>   mechanism after the last statement in the input procedure"
      *>   -> OK  14.9.40.4 11)
      *> cite.py --check 14.9.40.4 "The compiler inserts a return
      *>   mechanism after the last statement in the output procedure"
      *>   -> OK  14.9.40.4 14)
      *> cite.py --check 14.9.24.4 "The compiler inserts a return
      *>   mechanism after the last statement in the output procedure"
      *>   -> OK  14.9.24.4 9)
      *> cite.py --check 14.9.40.4 "The range includes all statements
      *>   that are executed as the result of a transfer of control in
      *>   the range of the output procedure" -> OK  14.9.40.4 13)
      *> Derivation: every procedure ends at LAST-EXIT, the last
      *>   paragraph, and reaches it by GO TO from inside its own range
      *>   (the common-exit idiom), so the return mechanism after
      *>   LAST-EXIT is reached each time.
      *>   SORT-OUT  output procedure RET-SA THRU LAST-EXIT returns AAA
      *>             then BBB (ASCENDING), N=2, then the SORT resumes.
      *>   SORT-IN   input procedure REL-SA THRU LAST-EXIT releases XXX,
      *>             ZZZ, YYY; the sort runs (DESCENDING) and the output
      *>             procedure returns ZZZ YYY XXX, N=3.
      *>   MERGE-OUT output procedure RET-MA THRU LAST-EXIT returns AAA
      *>             BBB CCC from M1 (AAA, CCC) and M2 (BBB), N=3.
      *>   PERFORM-LAST  PERFORM LAST-EXIT (14.9.28.4 GR5) returns.
      *> How it can fail: a dispatcher that reads the completion of a
      *>   range ending at the element's last paragraph as running off
      *>   the element executes the implicit GOBACK there, and the run
      *>   ends after the first SORT with nothing displayed.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3511SL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SA ASSIGN TO "pb3511sla.tmp".
           SELECT MA ASSIGN TO "pb3511slb.tmp".
           SELECT M1 ASSIGN TO "pb3511sl1.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT M2 ASSIGN TO "pb3511sl2.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       SD  SA.
       01  SA-REC.
           05 SA-K PIC X(3).
       SD  MA.
       01  MA-REC.
           05 MA-K PIC X(3).
       FD  M1.
       01  M1-REC PIC X(3).
       FD  M2.
       01  M2-REC PIC X(3).
       WORKING-STORAGE SECTION.
       01  N PIC 9 VALUE 0.
       01  KEYS PIC X(9) VALUE SPACES.
       PROCEDURE DIVISION.
       MAIN-PARA.
           SORT SA ON ASCENDING KEY SA-K
               INPUT PROCEDURE IS GEN-SA
               OUTPUT PROCEDURE IS RET-SA THRU LAST-EXIT
           DISPLAY "SORT-OUT " N " " KEYS
           MOVE 0 TO N
           MOVE SPACES TO KEYS
           SORT SA ON DESCENDING KEY SA-K
               INPUT PROCEDURE IS REL-SA THRU LAST-EXIT
               OUTPUT PROCEDURE IS RET-SA THRU LAST-EXIT
           DISPLAY "SORT-IN " N " " KEYS
           OPEN OUTPUT M1 M2
           MOVE "AAA" TO M1-REC
           WRITE M1-REC
           MOVE "CCC" TO M1-REC
           WRITE M1-REC
           MOVE "BBB" TO M2-REC
           WRITE M2-REC
           CLOSE M1 M2
           MOVE 0 TO N
           MOVE SPACES TO KEYS
           MERGE MA ON ASCENDING KEY MA-K
               USING M1 M2
               OUTPUT PROCEDURE IS RET-MA THRU LAST-EXIT
           DISPLAY "MERGE-OUT " N " " KEYS
           MOVE 0 TO N
           PERFORM LAST-EXIT
           DISPLAY "PERFORM-LAST " N
           STOP RUN.
       GEN-SA.
           MOVE "BBB" TO SA-K
           RELEASE SA-REC
           MOVE "AAA" TO SA-K
           RELEASE SA-REC.
       RET-SA.
           RETURN SA AT END GO TO LAST-EXIT END-RETURN
           ADD 1 TO N
           MOVE SA-K TO KEYS(N * 3 - 2:3)
           GO TO RET-SA.
       RET-MA.
           RETURN MA AT END GO TO LAST-EXIT END-RETURN
           ADD 1 TO N
           MOVE MA-K TO KEYS(N * 3 - 2:3)
           GO TO RET-MA.
       REL-SA.
           MOVE "XXX" TO SA-K
           RELEASE SA-REC
           MOVE "ZZZ" TO SA-K
           RELEASE SA-REC
           MOVE "YYY" TO SA-K
           RELEASE SA-REC
           GO TO LAST-EXIT.
       LAST-EXIT.
           ADD 0 TO N.
