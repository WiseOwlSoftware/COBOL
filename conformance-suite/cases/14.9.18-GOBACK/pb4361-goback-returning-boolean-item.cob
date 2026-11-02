*>--- conformance-case
*> id: pb4361-goback-returning-boolean-item
*> rules: [GR-14.9.18.4-2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4361 review round 1 (R1-1). With no header RETURNING item the GOBACK RETURNING extension moves
      *> its operand to the exit status, an elementary integer, so the operand is held to the WHOLE 14.9.25.3
      *> validity question and not to SR1 alone: SR10 and Table 16 make a boolean sending operand "No" to a
      *> numeric receiver, the same COBOL0819 the header-RETURNING arm gives. It used to compile clean and abort
      *> at run time. (Positive control: pb4361_goback_returning_scaled_operand.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGPB4361B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FLAGS PIC 1(8) USAGE BIT VALUE B"00000111".
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "X".
           GOBACK RETURNING FLAGS.
