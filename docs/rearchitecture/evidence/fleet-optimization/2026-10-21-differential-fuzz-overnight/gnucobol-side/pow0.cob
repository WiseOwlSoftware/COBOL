       IDENTIFICATION DIVISION.
       PROGRAM-ID. POW0.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-Z PIC S9(5) VALUE 0.
       01 R1 PIC S9(5) VALUE 77.
       01 R2 PIC S9(5) VALUE 77.
       01 R3 PIC S9(5) VALUE 77.
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R1 = WS-Z ** WS-Z
             ON SIZE ERROR DISPLAY "VAR: SIZE ERROR, R1=" R1
             NOT ON SIZE ERROR DISPLAY "VAR: no size error, R1=" R1
           END-COMPUTE
           DISPLAY "VAR: status=" FUNCTION EXCEPTION-STATUS
           COMPUTE R2 = 0 ** 0
             ON SIZE ERROR DISPLAY "LIT: SIZE ERROR, R2=" R2
             NOT ON SIZE ERROR DISPLAY "LIT: no size error, R2=" R2
           END-COMPUTE
           DISPLAY "LIT: status=" FUNCTION EXCEPTION-STATUS
           COMPUTE R3 = WS-Z ** WS-Z
           DISPLAY "VAR no phrase: R3=" R3 " status="
               FUNCTION EXCEPTION-STATUS
           STOP RUN.
