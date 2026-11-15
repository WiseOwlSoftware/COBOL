      *>--- conformance-case
      *> id: g2-arg-boolean
      *> rules: [2002-AR-15.2-3]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:15.2 AR3 - a boolean argument is "A bit group item, a
      *> boolean expression or literal, or an elementary boolean data
      *> item"; each of the four forms is given to INTEGER-OF-BOOLEAN
      *> and LENGTH (Table 22 Bool1).
      *> INTEGER-OF-BOOLEAN is the unsigned binary value of the bit
      *> configuration (§2002:15.35.3 RV1); LENGTH of a bit group item,
      *> an elementary boolean item or a boolean literal is its count of
      *> boolean positions (§2002:15.39.3 RV1). The expressions use the
      *> 2002 operators of §2002:8.8.2: B-NOT before B-AND before B-XOR
      *> before B-OR, and a shorter operand extended on the right with
      *> boolean zeros (8.8.2 rule 7).
      *>   BO1 010  item B"1010".
      *>   BO2 005  literal B"101".
      *>   BO3 002  1010 B-AND 0110 = 0010.
      *>   BO4 014  1010 B-OR 0110 = 1110.
      *>   BO5 012  1010 B-XOR 0110 = 1100.
      *>   BO6 005  B-NOT 1010 = 0101.
      *>   BO7 012  (1010 B-OR 0110) B-AND 1100 = 1100.
      *>   BO8 008  1010 B-AND 11 -> 1010 B-AND 1100 = 1000.
      *>   BO9 008 241  the bit group BG (1111 then 0001): 8 boolean
      *>               positions, 11110001 = 241.
      *>   BO10 004 003 LENGTH of the item and of the literal.
      *> A refused argument form, or a wrong value, fails the case.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2ARG02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B-BOOL       PIC 1(4) VALUE B"1010".
       01 B-BOOL2      PIC 1(4) VALUE B"0110".
       01 BG GROUP-USAGE BIT.
          05 BG1 PIC 1(4) VALUE B"1111".
          05 BG2 PIC 1(4) VALUE B"0001".
       01 N3A          PIC 999.
       01 N3B          PIC 999.
       PROCEDURE DIVISION.
       P0.
           MOVE FUNCTION INTEGER-OF-BOOLEAN(B-BOOL) TO N3A
           DISPLAY "BO1 " N3A
           MOVE FUNCTION INTEGER-OF-BOOLEAN(B"101") TO N3A
           DISPLAY "BO2 " N3A
           MOVE FUNCTION INTEGER-OF-BOOLEAN(B-BOOL B-AND B-BOOL2)
             TO N3A
           DISPLAY "BO3 " N3A
           MOVE FUNCTION INTEGER-OF-BOOLEAN(B-BOOL B-OR B-BOOL2)
             TO N3A
           DISPLAY "BO4 " N3A
           MOVE FUNCTION INTEGER-OF-BOOLEAN(B-BOOL B-XOR B-BOOL2)
             TO N3A
           DISPLAY "BO5 " N3A
           MOVE FUNCTION INTEGER-OF-BOOLEAN(B-NOT B-BOOL) TO N3A
           DISPLAY "BO6 " N3A
           MOVE FUNCTION INTEGER-OF-BOOLEAN(
                (B-BOOL B-OR B-BOOL2) B-AND B"1100") TO N3A
           DISPLAY "BO7 " N3A
           MOVE FUNCTION INTEGER-OF-BOOLEAN(B-BOOL B-AND B"11")
             TO N3A
           DISPLAY "BO8 " N3A
           MOVE FUNCTION LENGTH(BG) TO N3A
           MOVE FUNCTION INTEGER-OF-BOOLEAN(BG) TO N3B
           DISPLAY "BO9 " N3A " " N3B
           MOVE FUNCTION LENGTH(B-BOOL) TO N3A
           MOVE FUNCTION LENGTH(B"101") TO N3B
           DISPLAY "BO10 " N3A " " N3B
           STOP RUN.
