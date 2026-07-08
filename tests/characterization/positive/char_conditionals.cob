       IDENTIFICATION DIVISION.
       PROGRAM-ID. CHAR-CONDITIONALS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-N PIC S9(3) VALUE -5.
       01 WS-C PIC X VALUE "A".
       PROCEDURE DIVISION.
       MAIN-PARA.
           IF WS-N IS NEGATIVE
               DISPLAY "NEG"
           ELSE
               DISPLAY "NONNEG"
           END-IF.
           IF WS-C IS ALPHABETIC
               DISPLAY "ALPHA"
           END-IF.
           EVALUATE WS-C
               WHEN "A" DISPLAY "IS-A"
               WHEN "B" DISPLAY "IS-B"
               WHEN OTHER DISPLAY "OTHER"
           END-EVALUATE.
           STOP RUN.
