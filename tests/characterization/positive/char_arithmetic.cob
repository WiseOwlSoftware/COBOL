       IDENTIFICATION DIVISION.
       PROGRAM-ID. CHAR-ARITHMETIC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9(3) VALUE 100.
       01 B PIC 9(3) VALUE 25.
       01 C PIC 9(3).
       01 SMALL PIC 9(2).
       PROCEDURE DIVISION.
       MAIN-PARA.
           ADD A TO B GIVING C.
           SUBTRACT B FROM A GIVING C.
           COMPUTE C = (A + B) / 5.
           COMPUTE SMALL = A * B
               ON SIZE ERROR DISPLAY "OVERFLOW"
           END-COMPUTE.
           DISPLAY C.
           STOP RUN.
