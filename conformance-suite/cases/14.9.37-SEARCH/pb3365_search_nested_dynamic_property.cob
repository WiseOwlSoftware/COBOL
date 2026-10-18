*>--- conformance-case
*> id: pb3365_search_nested_dynamic_property
*> rules: [SR-14.9.37.3-2, SR-14.9.37.3-3, GR-14.9.37.4-1]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3365 (review R2-1) - the enclosing occurrence of a dynamic-capacity table is named ONLY in the WHEN
      *> phrase (14.9.37.4 GR1: "The subscript that is used to determine the occurrence of each superordinate table to
      *> search is specified by the user in the WHEN phrases"), and the subscript is an OBJECT PROPERTY reference
      *> (8.4.3.9.4 GR1: a sending property is the value its get method returns). The search is bounded by the current
      *> capacity (8.5.1.9.1) of the occurrence the property names, so the property is read BEFORE the bound is.
      *>   E(3) holds P Q R S (capacity 4)     E(1) holds A B (capacity 2)     EQ(3) holds A B C D, EQ(1) holds A B
      *> The property OC of an object holds the occurrence number the object was given (SET), so the two objects name
      *> E(3) and E(1) and the answer is derived from the capacities alone.
      *> Expected (derived from the capacities, never from a run):
      *>   P1 bare SEARCH EP, object O3 (OC = 3), WHEN "S": found at index 4. A bound read from an unfetched property
      *>      would end the search (or fail) before the fourth occurrence.
      *>   P2 the same statement executed again with O1 (OC = 1) in a loop, WHEN "S": E(1) has capacity 2, AT END.
      *>      Reading the previous execution's property value would still search E(3) and find "S".
      *>   P3 SEARCH ALL over the ascending key EQ with O3, WHEN "D": found at index 4.
      *>   P3 again with O1, WHEN "D": AT END (capacity 2).
      *>   P4 the property is the argument of an intrinsic function inside the subscript, object O3, WHEN "S": found at
      *>      index 4 (the argument's get is ordered ahead of the function by 8.4.3.2.4 GR2).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3365NESTPRP.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB3365OCC
           PROPERTY OC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O3 USAGE OBJECT REFERENCE PB3365OCC.
       01 O1 USAGE OBJECT REFERENCE PB3365OCC.
       01 S USAGE OBJECT REFERENCE PB3365OCC.
       01 BR.
          05 E OCCURS 3.
             10 EP PIC X OCCURS DYNAMIC CAPACITY IN ECAP INDEXED BY EX.
             10 EQ PIC X OCCURS DYNAMIC CAPACITY IN QCAP
                ASCENDING KEY IS EQ INDEXED BY QX.
       01 K PIC 9.
       01 T PIC 9.
       PROCEDURE DIVISION.
       MAIN-P.
           INVOKE PB3365OCC "NEW" RETURNING O3
           INVOKE PB3365OCC "NEW" RETURNING O1
           MOVE 3 TO OC OF O3
           MOVE 1 TO OC OF O1
           MOVE "A" TO EP(1, 1)
           MOVE "B" TO EP(1, 2)
           MOVE "P" TO EP(3, 1)
           MOVE "Q" TO EP(3, 2)
           MOVE "R" TO EP(3, 3)
           MOVE "S" TO EP(3, 4)
           MOVE "A" TO EQ(1, 1)
           MOVE "B" TO EQ(1, 2)
           MOVE "A" TO EQ(3, 1)
           MOVE "B" TO EQ(3, 2)
           MOVE "C" TO EQ(3, 3)
           MOVE "D" TO EQ(3, 4)
           SET S TO O3
           SET EX TO 1
           SEARCH EP
               AT END DISPLAY "P1 END"
               WHEN EP(OC OF S, EX) = "S"
                   SET K TO EX
                   DISPLAY "P1 FOUND " K
           END-SEARCH
           PERFORM VARYING T FROM 1 BY 1 UNTIL T > 2
               IF T = 1
                   SET S TO O3
               ELSE
                   SET S TO O1
               END-IF
               SET EX TO 1
               SEARCH EP
                   AT END DISPLAY "P2 END T=" T
                   WHEN EP(OC OF S, EX) = "S"
                       SET K TO EX
                       DISPLAY "P2 FOUND " K " T=" T
               END-SEARCH
           END-PERFORM
           PERFORM VARYING T FROM 1 BY 1 UNTIL T > 2
               IF T = 1
                   SET S TO O3
               ELSE
                   SET S TO O1
               END-IF
               SEARCH ALL EQ
                   AT END DISPLAY "P3 END T=" T
                   WHEN EQ(OC OF S, QX) = "D"
                       SET K TO QX
                       DISPLAY "P3 FOUND " K " T=" T
               END-SEARCH
           END-PERFORM
           SET S TO O3
           SET EX TO 1
           SEARCH EP
               AT END DISPLAY "P4 END"
               WHEN EP(FUNCTION INTEGER(OC OF S), EX) = "S"
                   SET K TO EX
                   DISPLAY "P4 FOUND " K
           END-SEARCH
           STOP RUN.
       END PROGRAM PB3365NESTPRP.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB3365OCC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-OC PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       METHOD-ID. GET PROPERTY OC.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-P PIC 9.
       PROCEDURE DIVISION RETURNING LK-P.
       GET-P.
           MOVE W-OC TO LK-P.
           GOBACK.
       END METHOD.
       METHOD-ID. SET PROPERTY OC.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-P PIC 9.
       PROCEDURE DIVISION USING LK-P.
       SET-P.
           MOVE LK-P TO W-OC.
           GOBACK.
       END METHOD.
       END OBJECT.
       END CLASS PB3365OCC.
