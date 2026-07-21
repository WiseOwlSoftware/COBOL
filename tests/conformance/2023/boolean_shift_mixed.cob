      *> ISO §8.8.2 rule 7b — a boolean SHIFT inherits the precedence of the operator preceding it (B-AND if
      *> none), so a shift MIXED with a binary boolean operator groups (binary-op THEN shift), left-to-right —
      *> NOT shift-first. Oracle (A=1010, B=0110): A B-AND B B-SHIFT-L 1 = (A B-AND B) shift-L1 = 0010<<1 = 0100
      *> (a shift-first mis-grouping would give A B-AND (B<<1) = 1010 B-AND 1100 = 1000); A B-OR B B-SHIFT-L 1 =
      *> (A B-OR B)<<1 = 1110<<1 = 1100; A B-SHIFT-L 1 B-AND B = (A<<1) B-AND B = 0100 B-AND 0110 = 0100.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. BSHMIX.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A   PIC 1(4) VALUE B"1010".
       01 B   PIC 1(4) VALUE B"0110".
       01 R   PIC 1(4).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R = A B-AND B B-SHIFT-L 1.
           DISPLAY "AND-SHIFT=" R.
           COMPUTE R = A B-OR B B-SHIFT-L 1.
           DISPLAY "OR-SHIFT=" R.
           COMPUTE R = A B-SHIFT-L 1 B-AND B.
           DISPLAY "SHIFT-AND=" R.
           STOP RUN.
