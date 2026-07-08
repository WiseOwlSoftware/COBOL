       IDENTIFICATION DIVISION.
       PROGRAM-ID. CHAR-OCCURS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-TBL.
          05 WS-ELEM PIC 9(2) OCCURS 5 TIMES INDEXED BY IX.
       01 J PIC 9(2).
       PROCEDURE DIVISION.
       MAIN-PARA.
           PERFORM VARYING J FROM 1 BY 1 UNTIL J > 5
               MOVE J TO WS-ELEM (J)
           END-PERFORM.
           SET IX TO 3.
           DISPLAY WS-ELEM (IX).
           SEARCH WS-ELEM
               WHEN WS-ELEM (IX) = 4
                   DISPLAY "FOUND4"
           END-SEARCH.
           STOP RUN.
