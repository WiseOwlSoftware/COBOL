*>--- conformance-case
*> id: pb5622-upper-lower-zero-length-2002
*> rules: [2002-AR-15.78.2-1]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB5622 - §2002:15.78.2 AR1 "Argument-1 shall be of class alphabetic, alphanumeric, or national and
      *> shall be at least one character position in length" (UPPER-CASE; LOWER-CASE's argument rule is the same
      *> sentence). §2002:15.2 AR13 makes an incorrect argument value set EC-ARGUMENT-FUNCTION. Judged on the 2002 text
      *> at --std 2002.
      *> The argument is a group whose only subordinate is a table OCCURS 0 TO 5 DEPENDING ON CNT, so at CNT = 0 it is
      *> a zero-length item (§2023:8.5.4 item 2, 'A group data item containing only a subordinate zero-length
      *> item'; FUNCTION LENGTH reads 0): the static MinWidth screen cannot see it, only the run does. The legs:
      *>   LEN        0 - the argument really is zero-length at run time, so the probe is what it claims
      *>   -OFF       checking OFF: the condition is set and the function reference yields the zero-length value
      *>              (docs/CONFORMANCE.md DOC-A.1-90: the returned length derives from the rejected argument); a
      *>              bracketed DISPLAY of a zero-length operand transfers nothing, so the brackets touch
      *>   -THREE     CNT = 3 and the group is the three characters: the rule is not tripped by a legal argument
      *>   -ON        checking ON: the declarative fires ("  RAISED") and RESUMEs, once per zero-length reference
      *>   -ON-THREE  the legal argument under checking ON raises nothing
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5622UL2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 CNT PIC 9 VALUE 0.
       01 V.
          05 E PIC X OCCURS 0 TO 5 DEPENDING ON CNT.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "LEN=" FUNCTION LENGTH(V)
           MOVE 0 TO CNT
           DISPLAY "UPPER-CASE-OFF=[" FUNCTION UPPER-CASE(V) "]"
           MOVE 3 TO CNT
           MOVE "abc" TO V
           DISPLAY "UPPER-CASE-THREE=[" FUNCTION UPPER-CASE(V) "]"
           MOVE 0 TO CNT
           DISPLAY "LOWER-CASE-OFF=[" FUNCTION LOWER-CASE(V) "]"
           MOVE 3 TO CNT
           MOVE "ABC" TO V
           DISPLAY "LOWER-CASE-THREE=[" FUNCTION LOWER-CASE(V) "]"
           CALL "PB5622UL2ON"
           STOP RUN.
       END PROGRAM PB5622UL2.

       >>TURN EC-ARGUMENT-FUNCTION CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5622UL2ON.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 CNT PIC 9 VALUE 0.
       01 V.
          05 E PIC X OCCURS 0 TO 5 DEPENDING ON CNT.
       01 R PIC X(5) VALUE "?????".
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-ARGUMENT-FUNCTION.
       H-P.
           DISPLAY "  RAISED".
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           MOVE 0 TO CNT
           DISPLAY "UPPER-CASE-ON"
           MOVE FUNCTION UPPER-CASE(V) TO R
           MOVE 3 TO CNT
           MOVE "abc" TO V
           MOVE FUNCTION UPPER-CASE(V) TO R
           DISPLAY "UPPER-CASE-ON-THREE=" R
           MOVE 0 TO CNT
           DISPLAY "LOWER-CASE-ON"
           MOVE FUNCTION LOWER-CASE(V) TO R
           MOVE 3 TO CNT
           MOVE "ABC" TO V
           MOVE FUNCTION LOWER-CASE(V) TO R
           DISPLAY "LOWER-CASE-ON-THREE=" R
           GOBACK.
       END PROGRAM PB5622UL2ON.
