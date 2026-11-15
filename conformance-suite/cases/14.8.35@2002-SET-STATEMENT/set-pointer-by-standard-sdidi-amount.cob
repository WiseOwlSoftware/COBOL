      *>--- conformance-case
      *> id: set-pointer-by-standard-sdidi-amount
      *> rules: [2002-GR-14.8.35.3-16, 2002-GR-14.8.35.3-17]
      *> accept: [2002]
      *> source-format: fixed
      *>---
      *> SET pointer UP BY arithmetic-expression-3 under ARITHMETIC IS
      *> STANDARD (kb/Work PB5619). The amount is a standard
      *> intermediate value, tested as it is, not landed at a fixed
      *> scale first.
      *> §2002:14.8.35.3 GR16: "If arithmetic-expression-3 does not
      *> evaluate to an integer, the EC-SIZE-ADDRESS exception
      *> condition is set to exist, the execution of the SET statement
      *> is unsuccessful, and the content of identifier-9 is
      *> unchanged."
      *> T1 amount = 1 + 1/10**20 = 1.00000000000000000001, 21 digits,
      *>    inside the 32-digit standard intermediate (§2002:8.8.1.3.5
      *>    division: exact quotient truncated to 32 significant
      *>    digits). Not an integer: EC-SIZE-ADDRESS, P stays at B1.
      *>    A scale-6 landing read it as 1 and moved P.
      *> §2002:14.8.35.3 GR17: the EC-RANGE-PTR exception condition is
      *> set to exist when the resulting address is not representable,
      *> and the content of identifier-9 is unchanged.
      *> T2 amount = 10 ** 40, an integer no address can take: the
      *>    condition is EC-RANGE-PTR (handled, P stays at B1), not a
      *>    size error that ends the run unit.
      *> T3 control: amount = 1 + 2 / 2 = 2, an integer. P moves to B3.
      *> ARITHMETIC IS STANDARD is a 2002-2014 clause (2023 removed it,
      *> COBOL0807); the rules are 2002-band, so the golden is accepted at 2002.
      *> Each leg fails if the SET moves P, raises another condition,
      *> or ends the run unit.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SETPTRSD.
       OPTIONS.
           ARITHMETIC IS STANDARD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BUF.
          05 B1 PIC X VALUE "A".
          05 B2 PIC X VALUE "B".
          05 B3 PIC X VALUE "C".
       01 P USAGE POINTER.
       01 Q USAGE POINTER.
       01 ONE PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H1 SECTION.
           USE AFTER EXCEPTION CONDITION EC-SIZE-ADDRESS.
       H1-P.
           DISPLAY "HANDLED=EC-SIZE-ADDRESS".
           RESUME AT NEXT STATEMENT.
       H2 SECTION.
           USE AFTER EXCEPTION CONDITION EC-RANGE-PTR.
       H2-P.
           DISPLAY "HANDLED=EC-RANGE-PTR".
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           >>TURN EC-SIZE-ADDRESS EC-RANGE-PTR CHECKING ON
           SET Q TO ADDRESS OF B1.
           SET P TO ADDRESS OF B1.
           SET P UP BY ONE + ONE / 100000000000000000000.
           IF P = Q DISPLAY "T1 UNMOVED" ELSE DISPLAY "T1 MOVED" END-IF.
           SET P UP BY 10 ** 40.
           IF P = Q DISPLAY "T2 UNMOVED" ELSE DISPLAY "T2 MOVED" END-IF.
           SET Q TO ADDRESS OF B3.
           SET P UP BY ONE + 2 / 2.
           IF P = Q DISPLAY "T3 AT-B3" ELSE DISPLAY "T3 ELSEWHERE"
           END-IF.
           STOP RUN.
