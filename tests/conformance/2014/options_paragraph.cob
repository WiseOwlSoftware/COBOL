      *> ISO 11.9 — the OPTIONS paragraph is accepted (clauses parsed; semantics are a later item).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OPTTEST.
       OPTIONS.
           ARITHMETIC IS STANDARD
           DEFAULT ROUNDED MODE IS NEAREST-EVEN.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "OPTOK".
           STOP RUN.
