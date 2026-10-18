      *>--- conformance-case
      *> id: pb722_return_into_unsuccessful
      *> rules: [GR-14.9.34.4-4, GR-14.9.34.4-5]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB722 (campaign PB3353: the abnormal path of RETURN ... INTO).
      *> ISO §14.9.34.4 GR5 b): "The implied MOVE statement does not occur
      *> if the execution of the RETURN statement was unsuccessful. Item
      *> identification of the data item referenced by identifier-1 is
      *> done after the record has been read and immediately before it is
      *> moved to the data item."  GR3: "When the at end condition
      *> exists, execution of the RETURN statement is unsuccessful and
      *> the contents of the record area ... are undefined."  GR4: "after
      *> the record is made available and after executing any implicit
      *> move resulting from the presence of an INTO phrase, control is
      *> transferred to imperative-statement-2".  This is the RETURN twin
      *> of l1_read_into_unsuccessful (READ ... INTO, §14.9.30.4 GR5); the
      *> success-path size witness is pb339_into_current_record leg G.
      *>
      *> LEG A - SUCCESS, MOVE BEFORE NOT AT END.  The SD record is the
      *> eight bytes "K01PQRST"; the receiver is X(12) JUSTIFIED RIGHT, so
      *> the current record (GR5 b), size by the RECORD clause) lands
      *> right-aligned.  The NOT AT END arm (GR4) already sees it, and the
      *> record stays available in the record area too.
      *> LEG B - CONTENT UNCHANGED.  The receiver holds "KEEP" (right
      *> aligned by its JUSTIFIED clause) immediately before the RETURN
      *> that finds no next record.  GR5 b) requires it back untouched; an
      *> implementation that ran the move anyway would hand over the
      *> record area, which still holds "K01PQRST" (GR3 leaves it
      *> undefined, so any other value fails the rule).
      *> LEG C - ITEM IDENTIFICATION.  The receiver is TE(IDX), IDX = 5
      *> over a 3-occurrence table, so identifying it sets
      *> EC-BOUND-SUBSCRIPT (§8.4.2.3.4 GR2, Table 13 fatal).  C1 is a
      *> successful RETURN: identification is done "after the record has
      *> been read and immediately before it is moved", so the declarative
      *> MUST run - the positive control proving it is armed.  Its RESUME
      *> is a not-normal completion (§14.6.13.1.2 1)), so the SORT whose
      *> output procedure raised it is terminated (§14.9.40.4 GR17: "If a
      *> USE procedure invoked while a format 1 SORT statement is active
      *> does not complete normally, the SORT statement is terminated"):
      *> "C1-RETURNED" never prints and control resumes at "C1-SORT-END".
      *> C2 is a RETURN that finds the file empty (the input procedure
      *> releases nothing): the move does not occur, so no "IDENT=" line
      *> may stand between "C2-ATEND" and "C2-RETURNED", and the SORT ends
      *> normally.  The last MOVE shows the declarative is still armed.
       >>TURN EC-BOUND-SUBSCRIPT CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB722R1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SRTF ASSIGN TO "pb722r1s.tmp".
       DATA DIVISION.
       FILE SECTION.
       SD SRTF RECORD IS VARYING IN SIZE FROM 3 TO 20
              DEPENDING ON WS-SLEN.
       01 S-REC.
          05 S-KEY PIC X(3).
          05 S-DATA PIC X(17).
       WORKING-STORAGE SECTION.
       01 WS-SLEN PIC 9(4) VALUE 8.
       01 WS-J12 PIC X(12) JUSTIFIED RIGHT.
       01 WS-T PIC X(8).
       01 WS-LEG PIC XX.
       01 TBL.
          05 TE PIC X(8) OCCURS 3 TIMES.
       01 IDX PIC 9 VALUE 5.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-BOUND-SUBSCRIPT.
       H-P.
           DISPLAY "IDENT=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           SORT SRTF ON ASCENDING KEY S-KEY
               INPUT PROCEDURE IS IN-ONE
               OUTPUT PROCEDURE IS OUT-AB.
           MOVE "C1" TO WS-LEG.
           SORT SRTF ON ASCENDING KEY S-KEY
               INPUT PROCEDURE IS IN-ONE
               OUTPUT PROCEDURE IS OUT-C.
           DISPLAY WS-LEG "-SORT-END".
           MOVE "C2" TO WS-LEG.
           SORT SRTF ON ASCENDING KEY S-KEY
               INPUT PROCEDURE IS IN-NONE
               OUTPUT PROCEDURE IS OUT-C.
           DISPLAY WS-LEG "-SORT-END".
           MOVE TE (IDX) TO WS-T.
           DISPLAY "DONE".
           STOP RUN.
       IN-ONE SECTION.
       IN-ONE-P.
           MOVE 8 TO WS-SLEN.
           MOVE "K01" TO S-KEY.
           MOVE "PQRST" TO S-DATA.
           RELEASE S-REC.
       IN-NONE SECTION.
       IN-NONE-P.
           CONTINUE.
       OUT-AB SECTION.
       OUT-AB-P.
           MOVE ALL "." TO WS-J12.
           RETURN SRTF INTO WS-J12
               AT END DISPLAY "A-ATEND"
               NOT AT END DISPLAY "A-NOTATEND=[" WS-J12 "]"
           END-RETURN.
           DISPLAY "A-AREA=[" S-REC (1:8) "]".
           MOVE "KEEP" TO WS-J12.
           RETURN SRTF INTO WS-J12
               AT END DISPLAY "B-ATEND"
           END-RETURN.
           DISPLAY "B=[" WS-J12 "]".
       OUT-C SECTION.
       OUT-C-P.
           RETURN SRTF INTO TE (IDX)
               AT END DISPLAY WS-LEG "-ATEND"
           END-RETURN.
           DISPLAY WS-LEG "-RETURNED".
