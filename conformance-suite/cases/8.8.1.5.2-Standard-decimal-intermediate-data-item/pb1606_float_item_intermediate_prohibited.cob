*>--- conformance-case
*> id: pb1606_float_item_intermediate_prohibited
*> rules: [GR-8.8.1.5.2-1]
*> accept: [2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1606 - ISO 8.8.1.5.2 rule 1: a float operand whose value "cannot be expressed exactly in an SDIDI" is
      *> "rounded according to the rules in 11.9.11, INTERMEDIATE ROUNDING clause"; 11.9.11.2 rule 3 d): "If the
      *> PROHIBITED phrase is specified and an intermediate value cannot be represented exactly in SDIDI form, the
      *> EC-SIZE-TRUNCATION exception condition is set to exist and the results of the operation are undefined." The
      *> size error condition (14.7.5) exists, so ON SIZE ERROR takes its branch and the receiver keeps its value.
      *> WiseOwl COBOL converts a floating-point data item by its exact binary value (docs/CONFORMANCE.md, section 3).
      *>
      *> HAND-DERIVED: the double nearest 0.1 is 0.1000000000000000055511151231257827021181583404541015625 - 55
      *> significant digits, so it CANNOT be expressed exactly in the 34 digits of an SDIDI: the lone operand, the
      *> operand beside an operator and the ADD ... GIVING operand each take the size error condition. 0.5 is exactly representable (one digit): no condition, and .5 is stored. 2**-50
      *> (8.8817841970012523233890533447265625E-16, 35 digits) is one digit too many: the condition again.
      *> Each leg can fail on its own: a conversion through the shortest decimal image would call 0.1 exact.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB1606P.
OPTIONS.
    ARITHMETIC IS STANDARD-DECIMAL
    INTERMEDIATE ROUNDING IS PROHIBITED.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 F1   USAGE FLOAT-LONG VALUE 0.1.
01 FH   USAGE FLOAT-LONG VALUE 0.5.
01 FT   USAGE FLOAT-LONG VALUE 8.8817841970012523233890533447265625E-16.
01 D19  PIC V9(19) VALUE 0.7.
PROCEDURE DIVISION.
MAIN.
    COMPUTE D19 = F1
        ON SIZE ERROR DISPLAY "LONE=SIZE-ERROR"
        NOT ON SIZE ERROR DISPLAY "LONE=OK"
    END-COMPUTE
    COMPUTE D19 = F1 + 0
        ON SIZE ERROR DISPLAY "PLUS0=SIZE-ERROR"
        NOT ON SIZE ERROR DISPLAY "PLUS0=OK"
    END-COMPUTE
    ADD F1 GIVING D19
        ON SIZE ERROR DISPLAY "GIVING=SIZE-ERROR"
        NOT ON SIZE ERROR DISPLAY "GIVING=OK"
    END-ADD
    COMPUTE D19 = FT
        ON SIZE ERROR DISPLAY "TIE=SIZE-ERROR"
        NOT ON SIZE ERROR DISPLAY "TIE=OK"
    END-COMPUTE
    DISPLAY "UNCHANGED=" D19
    COMPUTE D19 = FH
        ON SIZE ERROR DISPLAY "HALF=SIZE-ERROR"
        NOT ON SIZE ERROR DISPLAY "HALF=OK"
    END-COMPUTE
    DISPLAY "STORED=" D19
    STOP RUN.
