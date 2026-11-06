*>--- conformance-case
*> id: pb4831_perform_varying_length_of_variable_length_group
*> rules: [SR-14.9.28.3-2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> PB4831 - PERFORM VARYING's FROM and BY take a function-identifier. ISO §14.9.28.3 SR2 asks of each identifier a numeric
      *> item, and FUNCTION LENGTH (VG) is a function-identifier (§8.4.3.1.2 Format 1) whose value is the integer 3 while VG's
      *> OCCURS DEPENDING ON item holds 3 (§15.50.4 r4b). It is an identifier in the FROM and BY phrases and the
      *> integer SR4 a) / SR5 b) ask of an identifier beside an index-name, so each loop below runs: I counts 3, 4, 5 from the
      *> function in FROM; J counts 1, 4, 7 by the function in BY; after MOVE 4 TO K the index-name loop starts at occurrence 4 (the function's value then) and ends after it.
      *> The bare function was refused COBOL2119 as "an arithmetic expression" because its BoundBinary sum was read by tree shape.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4831ACCEPT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 K     PIC 9 VALUE 3.
       01 VG.
          05 T  PIC X OCCURS 1 TO 9 DEPENDING ON K INDEXED BY IX.
       01 I     PIC 99.
       01 J     PIC 99.
       PROCEDURE DIVISION.
           PERFORM VARYING I FROM FUNCTION LENGTH (VG) BY 1 UNTIL I > 5
               DISPLAY "I " I
           END-PERFORM
           PERFORM VARYING J FROM 1 BY FUNCTION LENGTH (VG) UNTIL J > 9
               DISPLAY "J " J
           END-PERFORM
           MOVE 4 TO K
           PERFORM VARYING IX FROM FUNCTION LENGTH (VG) BY 1 UNTIL IX > 4
               SET I TO IX
               DISPLAY "IX " I
           END-PERFORM
           STOP RUN.
