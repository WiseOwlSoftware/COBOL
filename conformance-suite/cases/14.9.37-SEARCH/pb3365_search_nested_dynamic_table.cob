*>--- conformance-case
*> id: pb3365_search_nested_dynamic_table
*> rules: [SR-14.9.37.3-2, SR-14.9.37.3-3, GR-14.9.37.4-1]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3365 - SEARCH and SEARCH ALL of a dynamic-capacity table (OCCURS DYNAMIC) that lies INSIDE another
      *> table. ISO 8.5.1.9.1: "The number of occurrences allocated at a particular time is referred to as the
      *> current capacity of the table", and 8.5.1.9.2 makes a sending reference "the same as for a fixed-capacity
      *> table whose number of occurrences is the current capacity of the table". Each occurrence of the enclosing
      *> table E therefore holds a capacity of its own, and the search is bounded by the capacity of the ONE occurrence
      *> it searches (14.9.37.4 GR1: the search varies the index "associated with identifier-1" over that table):
      *>   E(1) has capacity 3 (A B C)       E(2) has capacity 1 (X)       E(3) has capacity 4 (P Q R S)
      *> The occurrence is named either on identifier-1 (14.9.37.3 SR2 bans only the SEARCHED level's subscript, so
      *> the enclosing one may be written) or - the spelling 14.9.37.4 GR1 puts first - "specified by the user in the
      *> WHEN phrases"; SR3 keeps identifier-1 "contained within one or more other tables, for which the subscripting
      *> is still required". Both spellings give the same answer, asserted below.
      *> Expected (derived from the capacities, never from a run):
      *>   S1  SEARCH EP(1) VARYING J, WHEN = "C": found at index 3; J, a data item, is incremented once per step
      *>       the index takes (14.9.37.4 GR3 b), starting at 0, so 2 steps -> J = 2.
      *>   S2  the same occurrence, bare identifier-1: found at 3.
      *>   S3  E(3), WHEN = "S": found at 4. A bound taken from E(1)'s capacity (3) would end the search first.
      *>   S4  E(2), WHEN = "C": AT END - capacity 1, only "X" is searched.
      *>   S5  E(2), WHEN = SPACE: AT END. Occurrence 2 of E(2) does not exist, so a bound beyond its capacity would
      *>       read an occurrence the table does not hold.
      *>   S6  SEARCH ALL over the ascending key EQ, E(3) = A B C D, WHEN = "D": found at 4.
      *>   S7  SEARCH ALL, E(2) = A B, WHEN = "D": AT END (capacity 2).
      *>   S8  the WHEN body moves the enclosing subscript I: the search ends at the satisfied WHEN, and the next
      *>       statement searches the table I now names.
      *>   S9  the enclosing occurrence is a FUNCTION result written only in the WHEN phrase, inside a loop (the value
      *>       is evaluated afresh for each execution of the statement - 8.4.3.2.4 GR1): T = 3 searches E(3) and finds
      *>       "S" at 4; T = 1 then searches E(1), capacity 3, and ends. A bound read before the function was evaluated
      *>       (or kept from the previous execution) would fail the first and misbound the second.
      *>   S10 the same through SEARCH ALL over EQ: T = 3 finds "D" at 4; T = 2 searches E(2), capacity 2, and ends.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3365NESTDYN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BR.
          05 E OCCURS 3.
             10 EP PIC X OCCURS DYNAMIC CAPACITY IN ECAP INDEXED BY EX.
             10 EQ PIC X OCCURS DYNAMIC CAPACITY IN QCAP
                ASCENDING KEY IS EQ INDEXED BY QX.
       01 I PIC 9.
       01 J PIC 9 VALUE 0.
       01 K PIC 9.
       01 T PIC S9 SIGN IS LEADING SEPARATE.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE "A" TO EP(1, 1)
           MOVE "B" TO EP(1, 2)
           MOVE "C" TO EP(1, 3)
           MOVE "X" TO EP(2, 1)
           MOVE "P" TO EP(3, 1)
           MOVE "Q" TO EP(3, 2)
           MOVE "R" TO EP(3, 3)
           MOVE "S" TO EP(3, 4)
           MOVE "A" TO EQ(2, 1)
           MOVE "B" TO EQ(2, 2)
           MOVE "A" TO EQ(3, 1)
           MOVE "B" TO EQ(3, 2)
           MOVE "C" TO EQ(3, 3)
           MOVE "D" TO EQ(3, 4)
           MOVE 1 TO I
           SET EX TO 1
           SEARCH EP(I) VARYING J
               AT END DISPLAY "S1 END"
               WHEN EP(I, EX) = "C"
                   SET K TO EX
                   DISPLAY "S1 FOUND " K " J=" J
           END-SEARCH
           SET EX TO 1
           SEARCH EP
               AT END DISPLAY "S2 END"
               WHEN EP(I, EX) = "C"
                   SET K TO EX
                   DISPLAY "S2 FOUND " K
           END-SEARCH
           MOVE 3 TO I
           SET EX TO 1
           SEARCH EP
               AT END DISPLAY "S3 END"
               WHEN EP(I, EX) = "S"
                   SET K TO EX
                   DISPLAY "S3 FOUND " K
           END-SEARCH
           MOVE 2 TO I
           SET EX TO 1
           SEARCH EP(I)
               AT END DISPLAY "S4 END"
               WHEN EP(I, EX) = "C"
                   SET K TO EX
                   DISPLAY "S4 FOUND " K
           END-SEARCH
           SET EX TO 1
           SEARCH EP
               AT END DISPLAY "S5 END"
               WHEN EP(I, EX) = SPACE
                   SET K TO EX
                   DISPLAY "S5 FOUND " K
           END-SEARCH
           MOVE 3 TO I
           SEARCH ALL EQ
               AT END DISPLAY "S6 END"
               WHEN EQ(I, QX) = "D"
                   SET K TO QX
                   DISPLAY "S6 FOUND " K
           END-SEARCH
           MOVE 2 TO I
           SEARCH ALL EQ(I)
               AT END DISPLAY "S7 END"
               WHEN EQ(I, QX) = "D"
                   SET K TO QX
                   DISPLAY "S7 FOUND " K
           END-SEARCH
           MOVE 1 TO I
           SET EX TO 1
           SEARCH EP(I)
               AT END DISPLAY "S8 END"
               WHEN EP(I, EX) = "B"
                   MOVE 3 TO I
                   DISPLAY "S8 FOUND"
           END-SEARCH
           SET EX TO 1
           SEARCH EP(I)
               AT END DISPLAY "S8 END3"
               WHEN EP(I, EX) = "R"
                   SET K TO EX
                   DISPLAY "S8 FOUND3 " K
           END-SEARCH
           PERFORM VARYING T FROM 3 BY -2 UNTIL T < 1
               SET EX TO 1
               SEARCH EP
                   AT END DISPLAY "S9 END T=" T
                   WHEN EP(FUNCTION INTEGER(T), EX) = "S"
                       SET K TO EX
                       DISPLAY "S9 FOUND " K " T=" T
               END-SEARCH
           END-PERFORM
           PERFORM VARYING T FROM 3 BY -1 UNTIL T < 2
               SEARCH ALL EQ
                   AT END DISPLAY "S10 END T=" T
                   WHEN EQ(FUNCTION INTEGER(T), QX) = "D"
                       SET K TO QX
                       DISPLAY "S10 FOUND " K " T=" T
               END-SEARCH
           END-PERFORM
           STOP RUN.
