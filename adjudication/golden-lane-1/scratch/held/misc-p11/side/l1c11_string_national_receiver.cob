      *> SIDE FINDING (not a row golden) — ISO §14.9.43.3 SR1 admits a
      *>   national STRING receiver when every operand is national.
      *>   cite.py --check 14.9.43.3 "shall be described implicitly or
      *>   explicitly as usage display or national"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C11S.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-N PIC N(6).
       01 W-P PIC 99 VALUE 1.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE SPACES TO W-N.
           STRING N"AB" N"CD" DELIMITED BY SIZE
               INTO W-N WITH POINTER W-P
           END-STRING.
           DISPLAY "[" W-N "] P=" W-P.
           STOP RUN.
