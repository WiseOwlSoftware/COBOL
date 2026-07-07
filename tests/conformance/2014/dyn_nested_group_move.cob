      *> OCCURS DYNAMIC review #1 regression (data-model D9; ISO 8.5.1.9.3): a whole-GROUP receiving MOVE into a group
      *> nested BELOW the dynamic level must grow the table through the RECEIVING accessor (RefReceiving), NOT the
      *> sending one (RefSending drops an out-of-capacity write into benign scratch = silent data loss). FROM 2:
      *> MOVE to G(5) grows ELEM to 5 and the value lands.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DYN-NESTED-GROUP-MOVE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 ED PIC ZZ9.
       01 T.
          05 ELEM OCCURS DYNAMIC CAPACITY IN CAP FROM 2.
             10 G.
                15 A PIC X(3).
                15 B PIC 9(2).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "AB7" TO G (5).
           MOVE CAP TO ED.
           DISPLAY "CAP=[" ED "]".
           DISPLAY "A5=[" A OF G (5) "]".
           DISPLAY "B5=[" B OF G (5) "]".
           STOP RUN.
