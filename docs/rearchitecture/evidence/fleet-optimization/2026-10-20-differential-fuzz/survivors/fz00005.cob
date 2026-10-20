       IDENTIFICATION DIVISION.
       PROGRAM-ID. FUZZ.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A PIC 9(5) VALUE 2.
       01  B PIC 9(5) VALUE 3.
       01  C PIC 9(5) VALUE 4.
       01  RESULT PIC 9(10).
       PROCEDURE DIVISION.
           COMPUTE RESULT = A + B * C.
           DISPLAY "A+B*C=" RESULT.
           COMPUTE RESULT = (A + B) * C.
           DISPLAY "(A+B)*C=" RESULT.
           COMPUTE RESULT = A ** B ** C.
           DISPLAY "A**B**C=" RESULT.
           COMPUTE RESULT = (A ** B) ** C.
           DISPLAY "(A**B)**C=" RESULT.
           COMPUTE RESULT = -A + B.
           DISPLAY "-A+B=" RESULT.
           COMPUTE RESULT = A + (-B).
           DISPLAY "A+(-B)=" RESULT.
           COMPUTE RESULT = A * B / C.
           DISPLAY "A*B/C=" RESULT.
           COMPUTE RESULT = (A * B) / C.
           DISPLAY "(A*B)/C=" RESULT.
           COMPUTE RESULT = A * (B / C).
           DISPLAY "A*(B/C)=" RESULT.
           STOP RUN.
