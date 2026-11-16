      *>--- conformance-case
      *> id: g2evr00_range_same_class_accepted
      *> rules: [2002-SR-14.8.12.2-4]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.12.2 SR4 - "The two operands in a range-expression
      *> shall be of the same class and shall not be of class boolean,
      *> object or pointer."  The accepted companion of the four
      *> refusals g2evr01..04: ranges whose two ends share one class
      *> that SR4 does not exclude (alphanumeric, numeric, national)
      *> compile and select by value. §2002:14.8.12.3 GR4 turns the pair
      *> into "selection-subject >= left-part AND selection-subject <=
      *> right-part"; "The result of the analysis is the truth value of
      *> the resulting conditional expression" (cite.py OK, GR4).
      *>   A-IN   "BB" >= "AA" and "BB" <= "ZZ"            (alphanumeric)
      *>   N-IN   12 >= 10 and 12 <= 20                    (numeric)
      *>   N-OUT  25 > 20, so the range is false, OTHER     (numeric)
      *>   U-IN   N"BB" >= N"AA" and N"BB" <= N"ZZ"        (national)
      *> A refusal of any same-class range, or a wrong selection, fails.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2EVR00.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-A  PIC X(2) VALUE "BB".
       01 WS-AL PIC X(2) VALUE "AA".
       01 WS-AH PIC X(2) VALUE "ZZ".
       01 WS-N  PIC 99   VALUE 12.
       01 WS-U  PIC N(2) VALUE N"BB".
       01 WS-UL PIC N(2) VALUE N"AA".
       01 WS-UH PIC N(2) VALUE N"ZZ".
       PROCEDURE DIVISION.
       MAIN-P.
           EVALUATE WS-A
               WHEN WS-AL THRU WS-AH
                   DISPLAY "A-IN"
               WHEN OTHER
                   DISPLAY "A-OUT"
           END-EVALUATE
           EVALUATE WS-N
               WHEN 10 THRU 20
                   DISPLAY "N-IN"
               WHEN OTHER
                   DISPLAY "N-OUT"
           END-EVALUATE
           MOVE 25 TO WS-N
           EVALUATE WS-N
               WHEN 10 THRU 20
                   DISPLAY "N-IN"
               WHEN OTHER
                   DISPLAY "N-OUT"
           END-EVALUATE
           EVALUATE WS-U
               WHEN WS-UL THRU WS-UH
                   DISPLAY "U-IN"
               WHEN OTHER
                   DISPLAY "U-OUT"
           END-EVALUATE
           STOP RUN.
