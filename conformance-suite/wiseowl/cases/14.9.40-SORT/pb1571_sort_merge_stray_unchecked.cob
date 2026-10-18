*>--- conformance-case
*> id: pb1571_sort_merge_stray_unchecked
*> rules: [GR-14.9.40.4-10, GR-14.9.40.4-13]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1571 -- the same misplaced RETURN and RELEASE as
      *> pb1571_sort_merge_active_vs_flow, with checking OFF (no TURN): ISO 14.6.13.1.1
      *> raises nothing, and 14.9.40.4 GR10 / GR13 leave the results undefined.
      *> docs/CONFORMANCE.md 3, D-SMA (c) is the determination: a RETURN executed in
      *> the range of an input procedure answers at end and takes no record; a RELEASE
      *> executed in the range of an output procedure is not kept. GnuCOBOL 3.2
      *> differs (its RETURN takes the record the output procedure is owed, its RELEASE
      *> is kept); the survey in D-SMA (a) says why this compiler does not follow it.
      *> Here each statement is reached from the procedure by a PERFORM of a
      *> paragraph outside the range, as in the checked program; a RETURN or RELEASE
      *> written directly in the range is legal source (14.9.40.3 SR3 refuses only a
      *> SORT there) and answers the same way.
      *>
      *> EXPECTED, in execution order --
      *>   STRAY-END         the RETURN in IP1's range answers at end
      *>   GOT BBB / END     the record IP1 released reaches OP1
      *>   SORT1 DONE
      *>   OP2 AFTER STRAY   the RELEASE of ZZZ in OP2's range raised nothing
      *>   GOT AAA / END     ZZZ is not returned
      *>   SORT2 DONE
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1571STRAYUNCHK.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "pb1571-stray.srt".
       DATA DIVISION.
       FILE SECTION.
       SD SF.
       01 SR PIC X(3).
       WORKING-STORAGE SECTION.
       01 EOFF PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-P.
           SORT SF ON ASCENDING KEY SR
              INPUT PROCEDURE IP1 OUTPUT PROCEDURE OP1
           DISPLAY "SORT1 DONE"
           MOVE 0 TO EOFF
           SORT SF ON ASCENDING KEY SR
              INPUT PROCEDURE IP2 OUTPUT PROCEDURE OP2
           DISPLAY "SORT2 DONE"
           STOP RUN.
       IP1.
           MOVE "BBB" TO SR
           RELEASE SR
           PERFORM STRAY-RET.
       IP2.
           MOVE "AAA" TO SR
           RELEASE SR.
       OP1.
           PERFORM RET-ALL.
       OP2.
           PERFORM STRAY-REL
           DISPLAY "OP2 AFTER STRAY"
           PERFORM RET-ALL.
       RET-ALL.
           PERFORM UNTIL EOFF = 1
              RETURN SF AT END MOVE 1 TO EOFF DISPLAY "END"
                 NOT AT END DISPLAY "GOT " SR
              END-RETURN
           END-PERFORM.
       STRAY-RET.
           RETURN SF AT END DISPLAY "STRAY-END"
              NOT AT END DISPLAY "STRAY-GOT " SR
           END-RETURN.
       STRAY-REL.
           MOVE "ZZZ" TO SR
           RELEASE SR.
