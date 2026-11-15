      *>--- conformance-case
      *> id: set-index-by-standard-sdidi-amount
      *> rules: [2002-GR-14.8.35.3-3]
      *> accept: [2002]
      *> source-format: fixed
      *>---
      *> SET index-name UP BY arithmetic-expression-2 under ARITHMETIC IS
      *> STANDARD (kb/Work PB5619), the index sibling of
      *> set-pointer-by-standard-sdidi-amount: the amount is a standard
      *> intermediate value, tested as it is.
      *> §2002:14.8.35.3 GR3a: "If arithmetic-expression-2 does not
      *> evaluate to an integer, the EC-BOUND-SUBSCRIPT exception
      *> condition is set to exist, the execution of the SET statement
      *> is unsuccessful, and the content of the receiving operand is
      *> unchanged."
      *> T1 amount = 1 + 1/10**20 (21 digits, within the 32-digit
      *>    standard intermediate, §2002:8.8.1.3.5). Not an integer:
      *>    EC-BOUND-SUBSCRIPT is handled and IX still refers to
      *>    element 1, "A". A scale-6 landing read the amount as 1.
      *> T2 control: amount = 1 + 2 / 2 = 2. IX refers to element 3, "C".
      *> Each leg fails if IX moves in T1, another condition is raised,
      *> or the run unit ends.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SETIXSD.
       OPTIONS.
           ARITHMETIC IS STANDARD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TBL.
          05 E PIC X OCCURS 5 INDEXED BY IX.
       01 ONE PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H1 SECTION.
           USE AFTER EXCEPTION CONDITION EC-BOUND-SUBSCRIPT.
       H1-P.
           DISPLAY "HANDLED=EC-BOUND-SUBSCRIPT".
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           >>TURN EC-BOUND-SUBSCRIPT CHECKING ON
           MOVE "ABCDE" TO TBL.
           SET IX TO 1.
           SET IX UP BY ONE + ONE / 100000000000000000000.
           DISPLAY "T1 " E (IX).
           SET IX UP BY ONE + 2 / 2.
           DISPLAY "T2 " E (IX).
           STOP RUN.
