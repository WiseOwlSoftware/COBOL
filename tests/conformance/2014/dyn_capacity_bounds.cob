      *> OCCURS DYNAMIC — the TO clause is the EXPECTED capacity, NOT a hard cap (increment 2, D9; ISO 8.5.1.9). With
      *> runtime checking OFF (the default), SET past TO continues and the current capacity becomes the requested
      *> value. (EC-BOUND-OVERFLOW is the checking-ON observation of "current exceeds expected" — a later increment.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DYN-CAP-BOUNDS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 ED       PIC ZZZ9.
       01 WS-TABLE.
          05 WS-E PIC 9(3) OCCURS DYNAMIC CAPACITY IN WS-CAP FROM 2 TO 4.
       PROCEDURE DIVISION.
       MAIN-PARA.
           SET WS-CAP TO 9.
           MOVE WS-CAP TO ED.
           DISPLAY "OVER=[" ED "]".
           STOP RUN.
