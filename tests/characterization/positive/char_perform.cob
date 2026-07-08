       IDENTIFICATION DIVISION.
       PROGRAM-ID. CHAR-PERFORM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 I PIC 9(2).
       01 T PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
       MAIN-PARA.
           PERFORM 3 TIMES
               ADD 1 TO T
           END-PERFORM.
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > 5
               ADD I TO T
           END-PERFORM.
           PERFORM SUB-PARA.
           DISPLAY T.
           STOP RUN.
       SUB-PARA.
           ADD 100 TO T.
