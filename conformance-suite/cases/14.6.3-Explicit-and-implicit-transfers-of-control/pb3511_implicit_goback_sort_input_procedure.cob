*>--- conformance-case
*> id: pb3511_implicit_goback_sort_input_procedure
*> rules: [GR-14.6.3-3, GR-14.9.40.4-11]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> PB3511 - a SORT input procedure whose GO TO leaves the named procedure
      *>   for the source element's last paragraph never reaches the return
      *>   mechanism, and running past that paragraph is the implicit GOBACK:
      *>   the sort never runs its output phase.
      *> cite.py --check 14.6.3 "an implicit GOBACK statement without any
      *>   optional phrases is executed" -> OK  14.6.3 3)
      *> Derivation: the input procedure is REL-SN alone; its return mechanism
      *>   follows REL-SN's last statement (14.9.40.4 GR11). REL-SN releases
      *>   one record and GO TO REL-SN-EXIT leaves it, so the return mechanism
      *>   is never reached: the records are never sequenced, the output
      *>   procedure never runs and the SORT never passes control to the
      *>   statement after it. REL-SN-EXIT is the source element's last
      *>   paragraph; docs/CONFORMANCE.md D-PTHRU takes GnuCOBOL 3.2's answer
      *>   (measured 2026-10-22, cobc -std=cobol2002: RELEASED, exit 0): the
      *>   implicit GOBACK, which in a main program operates as STOP (14.9.18.4
      *>   GR3). How it can fail: a dispatcher that reads the fall-off at the
      *>   end of the procedure division as the input procedure's completion
      *>   goes on to sequence the records, runs the output procedure and
      *>   displays SORTED=.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3511SI.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SN ASSIGN TO "pb3511si.tmp".
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
               OUTPUT PROCEDURE IS RET-SN THRU RET-SN-EXIT
           DISPLAY "SORTED=" N
           STOP RUN.
       RET-SN.
           RETURN SN AT END GO TO RET-SN-EXIT END-RETURN
           ADD 1 TO N
           DISPLAY "GOT " NK
           GO TO RET-SN.
       RET-SN-EXIT.
           EXIT.
       REL-SN.
           MOVE "AAA" TO NK
           RELEASE SNR
           DISPLAY "RELEASED"
           GO TO REL-SN-EXIT.
       REL-SN-UNREACHED.
           DISPLAY "UNREACHED".
       REL-SN-EXIT.
           EXIT.
