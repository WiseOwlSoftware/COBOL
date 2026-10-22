*>--- conformance-case
*> id: pb3511_implicit_goback_sort_output_procedure
*> rules: [GR-14.6.3-3, GR-14.9.40.4-13, GR-14.9.40.4-14]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> PB3511 - a SORT output procedure whose AT END GO TO leaves the named
      *>   procedure for the source element's last paragraph never reaches the
      *>   return mechanism, and running past that paragraph is the implicit
      *>   GOBACK, the same arm as a PERFORM whose range a GO TO has left.
      *> cite.py --check 14.9.40.4 "The compiler inserts a return mechanism
      *>   after the last statement in the output procedure. When control
      *>   passes to that return mechanism, the mechanism provides for the
      *>   termination of the sort and then passes control to the next
      *>   executable statement after the SORT statement" -> OK  14.9.40.4 14)
      *> cite.py --check 14.9.40.4 "The range includes all statements that are
      *>   executed as the result of a transfer of control in the range of the
      *>   output procedure" -> OK  14.9.40.4 13)
      *> cite.py --check 14.6.3 "an implicit GOBACK statement without any
      *>   optional phrases is executed" -> OK  14.6.3 3)
      *> Derivation: the output procedure is RET-SN alone; its return
      *>   mechanism follows RET-SN's last statement (GR14). RET-SN returns
      *>   both records (GOT AAA, GOT BBB) and its AT END GO TO RET-SN-EXIT
      *>   leaves RET-SN, so the return mechanism is never reached and the SORT
      *>   never passes control to the statement after it. RET-SN-EXIT is in
      *>   the procedure's range (GR13) yet is the source element's last
      *>   paragraph; whether 14.6.3's sentence covers a procedure still in a
      *>   range the standard does not decide, and docs/CONFORMANCE.md D-PTHRU
      *>   takes GnuCOBOL 3.2's answer (measured 2026-10-22, cobc -std=cobol2002:
      *>   GOT AAA, GOT BBB, exit 0): the implicit GOBACK, which in a main
      *>   program operates as STOP (14.9.18.4 GR3). SORTED= is never
      *>   displayed. How it can fail: a dispatcher that reads the fall-off at
      *>   the end of the procedure division as the output procedure's
      *>   completion displays SORTED=2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3511SO.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SN ASSIGN TO "pb3511so.tmp".
       DATA DIVISION.
       FILE SECTION.
       SD  SN.
       01  SNR.
           05 NK PIC X(3).
       WORKING-STORAGE SECTION.
       01  N PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           SORT SN ON ASCENDING KEY NK
               INPUT PROCEDURE IS REL-SN
               OUTPUT PROCEDURE IS RET-SN
           DISPLAY "SORTED=" N
           STOP RUN.
       REL-SN.
           MOVE "BBB" TO NK
           RELEASE SNR
           MOVE "AAA" TO NK
           RELEASE SNR.
       RET-SN.
           RETURN SN AT END GO TO RET-SN-EXIT END-RETURN
           ADD 1 TO N
           DISPLAY "GOT " NK
           GO TO RET-SN.
       RET-SN-EXIT.
           EXIT.
