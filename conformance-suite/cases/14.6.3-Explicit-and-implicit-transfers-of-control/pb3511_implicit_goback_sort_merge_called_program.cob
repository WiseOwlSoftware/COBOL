      *>--- conformance-case
      *> id: pb3511_implicit_goback_sort_merge_called_program
      *> rules: [GR-14.6.3-3, GR-14.9.40.4-10, GR-14.9.40.4-11,
      *>     GR-14.9.40.4-13, GR-14.9.40.4-14, GR-14.9.24.4-7,
      *>     GR-14.9.24.4-8, GR-14.9.24.4-9, GR-14.9.18.4-2,
      *>     GR-9.1.13.7-1, GR-14.6.2.3.2-3]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> PB3511 - in a CALLED program, a SORT or MERGE input or output
      *>   procedure whose GO TO leaves its procedure-name range for the
      *>   LAST paragraph runs off the source element: the return
      *>   mechanism after the procedure's last statement is never
      *>   reached, the SORT or MERGE never resumes, and the implicit
      *>   GOBACK returns to the CALL (determination D-PTHRU,
      *>   docs/CONFORMANCE.md). The abandoned statement leaves the
      *>   called program usable: the next SORT runs, and a MERGE's
      *>   USING files keep the open mode it gave them. The main-program
      *>   SORT forms are pb3511_implicit_goback_sort_input_procedure
      *>   and pb3511_implicit_goback_sort_output_procedure; a range
      *>   that ENDS at the last paragraph is
      *>   pb3511_sort_merge_procedure_ends_at_last_paragraph.
      *> cite.py --check 14.9.40.4 "The compiler inserts a return
      *>   mechanism after the last statement in the output procedure"
      *>   -> OK  14.9.40.4 14)  (11) for the input procedure, 14.9.24.4
      *>   9) for the MERGE output procedure: the same words)
      *> cite.py --check 14.9.24.4 "The range includes all statements
      *>   that are executed as the result of a transfer of control in
      *>   the range of the output procedure" -> OK  14.9.24.4 8)
      *>   (so the GO TO target is in the range: the case D-PTHRU
      *>   decides, the standard naming no next executable statement)
      *> cite.py --check 14.6.3 "an implicit GOBACK statement without
      *>   any optional phrases is executed" -> OK  14.6.3 3)
      *> cite.py --check 14.9.24.4 "If an output procedure is specified,
      *>   this termination is not performed until after control passes
      *>   the last statement in the output procedure"
      *>   -> OK  14.9.24.4 7) c)
      *> cite.py --check 9.1.13.7 "An OPEN statement or a DELETE FILE
      *>   statement that is attempted for a file connector in an open
      *>   mode" -> OK  9.1.13.7 1)  (I-O status 41)
      *> cite.py --check 14.6.2.3.2 "internal file connectors are
      *>   initialized by setting them to not be in any open mode"
      *>   -> OK  14.6.2.3.2 3)  (only in the initial state: a program
      *>   re-activated without CANCEL keeps its last-used state)
      *> Derivation, PB3511SS called once per MODE (14.9.18.4 GR2: the
      *>   implicit GOBACK of a called program returns to the CALL):
      *>   MODE 1  the output procedure RET-SA returns AAA, BBB (N=2),
      *>           then AT END GO TO SUB-LAST: SUB-LAST 1 N=2, and
      *>           "SORT 1 RESUMED" is never displayed.
      *>   MODE 2  the input procedure REL-OUT releases one record and
      *>           GO TO SUB-LAST: the records are never sorted, the
      *>           output procedure never runs (N=0), no RESUMED line.
      *>   MODE 3  OPEN OUTPUT M1 M2 (00, first activation); the MERGE
      *>           output procedure returns AAA, BBB (N=2), then leaves
      *>           for SUB-LAST. GR7 c) closes M1 and M2 only after
      *>           control passes the output procedure's last statement,
      *>           which it never does: they stay open (INPUT).
      *>   MODE 1  again: a new execution of the SORT statement, N=2.
      *>   MODE 3  again: M1 and M2 are still open in the last-used
      *>           state, so OPEN OUTPUT is 41; the WRITEs fail (the
      *>           files are in the input mode), CLOSE closes them, and
      *>           the MERGE re-reads AAA, BBB (N=2).
      *> GnuCOBOL 3.2 matches every line but the second MODE 3 OPEN (it
      *>   prints 00): it reads and closes the USING files before the
      *>   output procedure, which GR7 c) does not allow.
      *> How it can fail: a dispatcher that reads running off the
      *>   element as the procedure's completion resumes the SORT or
      *>   MERGE (the RESUMED lines appear; until kb/Work PB3511 they
      *>   did); a runtime that leaves the abandoned SORT active refuses
      *>   or corrupts the next one; one that closes the USING files on
      *>   the abandonment prints OPEN 00 the second time.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3511SG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  MODE-X PIC 9.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 1 TO MODE-X
           CALL "PB3511SS" USING MODE-X
           DISPLAY "BACK " MODE-X
           MOVE 2 TO MODE-X
           CALL "PB3511SS" USING MODE-X
           DISPLAY "BACK " MODE-X
           MOVE 3 TO MODE-X
           CALL "PB3511SS" USING MODE-X
           DISPLAY "BACK " MODE-X
           MOVE 1 TO MODE-X
           CALL "PB3511SS" USING MODE-X
           DISPLAY "BACK " MODE-X
           MOVE 3 TO MODE-X
           CALL "PB3511SS" USING MODE-X
           DISPLAY "BACK " MODE-X
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3511SS.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SA ASSIGN TO "pb3511sga.tmp".
           SELECT MA ASSIGN TO "pb3511sgb.tmp".
           SELECT M1 ASSIGN TO "pb3511sg1.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS FS.
           SELECT M2 ASSIGN TO "pb3511sg2.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS FS.
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
       01  N PIC 9.
       01  FS PIC XX.
       LINKAGE SECTION.
       01  MODE-L PIC 9.
       PROCEDURE DIVISION USING MODE-L.
       SUB-MAIN.
           MOVE 0 TO N
           IF MODE-L = 1
               SORT SA ON ASCENDING KEY SA-K
                   INPUT PROCEDURE IS GEN-SA
                   OUTPUT PROCEDURE IS RET-SA
               DISPLAY "SORT 1 RESUMED"
           END-IF
           IF MODE-L = 2
               SORT SA ON ASCENDING KEY SA-K
                   INPUT PROCEDURE IS REL-OUT
                   OUTPUT PROCEDURE IS RET-SA
               DISPLAY "SORT 2 RESUMED"
           END-IF
           IF MODE-L = 3
               OPEN OUTPUT M1 M2
               DISPLAY "OPEN " FS
               MOVE "AAA" TO M1-REC
               WRITE M1-REC
               MOVE "BBB" TO M2-REC
               WRITE M2-REC
               CLOSE M1 M2
               MERGE MA ON ASCENDING KEY MA-K
                   USING M1 M2
                   OUTPUT PROCEDURE IS RET-MA
               DISPLAY "MERGE RESUMED"
           END-IF
           GOBACK.
       GEN-SA.
           MOVE "BBB" TO SA-K
           RELEASE SA-REC
           MOVE "AAA" TO SA-K
           RELEASE SA-REC.
       REL-OUT.
           MOVE "CCC" TO SA-K
           RELEASE SA-REC
           GO TO SUB-LAST.
       RET-SA.
           RETURN SA AT END GO TO SUB-LAST END-RETURN
           ADD 1 TO N
           GO TO RET-SA.
       RET-MA.
           RETURN MA AT END GO TO SUB-LAST END-RETURN
           ADD 1 TO N
           GO TO RET-MA.
       SUB-LAST.
           DISPLAY "SUB-LAST " MODE-L " N=" N.
       END PROGRAM PB3511SS.
       END PROGRAM PB3511SG.
