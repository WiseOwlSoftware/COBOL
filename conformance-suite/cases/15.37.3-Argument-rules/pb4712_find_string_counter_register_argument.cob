*>--- conformance-case
*> id: pb4712_find_string_counter_register_argument
*> rules: [AR-15.37.3-3]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB4712 review R1-1 - the legal side of pb4712-find-string-parenthesized-counter-register. ISO §15.37.3 r3: "argument-3
      *> shall be an integer data item or integer literal." LINAGE-COUNTER written bare "references a temporary unsigned integer data
      *> item" (§8.4.3.14.4 r1), so it is a legal start position; the parenthesized spelling is an arithmetic expression and is
      *> refused. The register is 1 after OPEN OUTPUT (pb1153_counter_register_character_image), and argument-3 "represents the number
      *> of matches to ignore before determining the character position that shall be returned" (§15.37.4 r2), so one match of "A" in
      *> "AXA" is ignored (position 1) and the function returns the second, position 3. The leg fails if the register stops being a
      *> data item to this argument screen, which now asks the binder's statement that the register was written bare.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4712CTRACCEPT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb4712-counter-accept.dat".
       DATA DIVISION.
       FILE SECTION.
       FD F LINAGE IS 20 LINES.
       01 FR    PIC X(10).
       WORKING-STORAGE SECTION.
       01 X3    PIC X(3) VALUE "AXA".
       01 N     PIC 9(4).
       PROCEDURE DIVISION.
           OPEN OUTPUT F
           COMPUTE N = FUNCTION FIND-STRING(X3 "A" LINAGE-COUNTER)
           DISPLAY "FIND " N
           CLOSE F
           STOP RUN.
