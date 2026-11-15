      *>--- conformance-case
      *> id: g2can05_call_on_overflow_after_cancel
      *> rules: [2002-GR-14.8.5.3-6]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.5.3 GR6 - "A logical relationship to a canceled
      *> program is established only by execution of a subsequent CALL
      *> statement referencing that program". The CALL here is the
      *> 2002 format 1 CALL ... ON OVERFLOW (archaic: §2002:14.8.4.1),
      *> which 2023 deleted. G2CNO5S counts its activations in CNT.
      *>   CALL, CALL                         C=1  C=2
      *>   CANCEL, CANCEL again (GR7: no action, no relationship)
      *>   CALL "G2CNO5S" ON OVERFLOW         C=1  (re-established in
      *>        its initial state, §2002:14.8.5.3 GR3; ON OVERFLOW
      *>        ignored, §2002:14.8.4.3 GR3h)
      *>   CALL PN ON OVERFLOW                C=2  (the relationship the
      *>        ON OVERFLOW CALL established persists: last-used state,
      *>        §2002:8.6.5)
      *>   CANCEL, CALL "G2CNO5X" ON OVERFLOW OVF (no such program:
      *>        EC-PROGRAM-NOT-FOUND, §2002:14.8.4.3 GR3; control to
      *>        imperative-statement-1, §2002:14.8.4.3 GR3g1); that
      *>        CALL references another program, establishing nothing
      *>   CALL "G2CNO5S" ON OVERFLOW         C=1
      *> Fails if CANCEL does not end the relationship (C=3), if a
      *> CALL ... ON OVERFLOW does not re-establish it or runs its
      *> imperative statement on success (OVF after a C= line), or if
      *> the format 1 CALL is refused at 2002.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CNO5M.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PN PIC X(7) VALUE "G2CNO5S".
       PROCEDURE DIVISION.
       MAIN.
           CALL "G2CNO5S"
           CALL "G2CNO5S"
           CANCEL "G2CNO5S"
           CANCEL "G2CNO5S"
           CALL "G2CNO5S" ON OVERFLOW DISPLAY "OVF" END-CALL
           CALL PN ON OVERFLOW DISPLAY "OVF" END-CALL
           CANCEL PN
           CALL "G2CNO5X" ON OVERFLOW DISPLAY "OVF" END-CALL
           CALL "G2CNO5S" ON OVERFLOW DISPLAY "OVF" END-CALL
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CNO5S.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 CNT PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       S1.
           ADD 1 TO CNT
           DISPLAY "C=" CNT
           GOBACK.
       END PROGRAM G2CNO5S.
       END PROGRAM G2CNO5M.
