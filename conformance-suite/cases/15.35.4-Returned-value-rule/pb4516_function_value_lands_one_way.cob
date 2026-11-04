*>--- conformance-case
*> id: pb4516_function_value_lands_one_way
*> rules: [RV-15.35.4-1, RV-15.4.1-1]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4516 - ONE returned value lands ONE way, whatever the shape of its receiver.
      *>
      *> ISO 15.4: "The evaluation of a function produces a returned value in a temporary elementary data item."
      *> The temporary holds ONE value. 15.4.1: "When native arithmetic is in effect, the characteristics and
      *> representation of the returned value are defined by the implementor." 15.35.4 r1 gives EXP10 the
      *> equivalent arithmetic expression (10 ** (argument-1)), so EXP10 (-6) is one millionth, 0.000001.
      *> Implementor determination (docs/CONFORMANCE.md, "An implementor-defined APPROXIMATION is not a data
      *> item"): the binary64 the engine computes for it is an approximation whose value is its SHORTEST
      *> ROUND-TRIP DECIMAL, 0.000001 - not the 9.99999999999999954748E-7 of the double's binary expansion.
      *> The double nearest 10 ** -6 sits BELOW that value, so a landing that reads the expansion truncates it
      *> to 0.000000 at six places while the operand lift beside an operator (0 + EXP10 (-6)) reads 0.000001.
      *>
      *> Every leg stores the same returned value into a receiver of a different shape and must show 0.000001:
      *>   L1 a bare COMPUTE (the final transfer, no ROUNDED phrase: 14.7.4.3 r2, TRUNCATION)
      *>   L2 COMPUTE with an operator operand, the shape that always agreed
      *>   L3 MOVE (14.6.8.2 r4, truncation on either end)
      *>   L4 COMPUTE under ON SIZE ERROR (the checked landing)
      *>   L5 a numeric-edited receiver by MOVE, L6 by COMPUTE
      *>   L7 one MOVE into two receivers
      *>   L8 CALL BY CONTENT into a fixed-point formal of a contained program (14.2.3 GR9/GR10: "a COMPUTE
      *>      statement without the ROUNDED phrase", whose sending operand is the same returned value)
      *> Each leg can fail on its own: a landing that reads the binary expansion shows 0.000000 (L1, L3, L4,
      *> L5, L6, L7, L8). The PROHIBITED leg, which needs ROUNDED MODE IS (2014), is pb4516_function_value_prohibited.
      *>
      *> --check validated:
      *>   cite.py --check 15.4 "The evaluation of a function produces a returned value in a temporary elementary data item" -> OK
      *>   cite.py --check 15.4.1 "the characteristics and representation of the returned value are defined by the implementor" -> OK
      *>   cite.py --check 14.2.3 "a COMPUTE statement without the ROUNDED phrase" -> OK (rule 9)
IDENTIFICATION DIVISION.
PROGRAM-ID. PB4516ONE.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 R   PIC 9(3)V9(6) VALUE 0.
01 R2  PIC 9(3)V9(6) VALUE 0.
01 E   PIC 9.9(6).
01 E2  PIC 9.9(6).
01 EA  PIC 9.9(6).
01 EB  PIC 9.9(6).
PROCEDURE DIVISION.
    COMPUTE R = FUNCTION EXP10 (-6)
    MOVE R TO E
    DISPLAY "L1 " E
    COMPUTE R = 0 + FUNCTION EXP10 (-6)
    MOVE R TO E
    DISPLAY "L2 " E
    MOVE ZERO TO R
    MOVE FUNCTION EXP10 (-6) TO R
    MOVE R TO E
    DISPLAY "L3 " E
    MOVE ZERO TO R
    COMPUTE R = FUNCTION EXP10 (-6)
        ON SIZE ERROR DISPLAY "L4 SIZE-ERROR"
        NOT ON SIZE ERROR
            MOVE R TO E
            DISPLAY "L4 " E
    END-COMPUTE
    MOVE FUNCTION EXP10 (-6) TO E2
    DISPLAY "L5 " E2
    COMPUTE EA = FUNCTION EXP10 (-6)
    DISPLAY "L6 " EA
    MOVE ZERO TO R R2
    MOVE FUNCTION EXP10 (-6) TO R R2
    MOVE R TO E
    MOVE R2 TO EB
    DISPLAY "L7 " E " " EB
    CALL "PB4516SUB" USING BY CONTENT FUNCTION EXP10 (-6)
    STOP RUN.

IDENTIFICATION DIVISION.
PROGRAM-ID. PB4516SUB.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 LE  PIC 9.9(6).
LINKAGE SECTION.
01 L   PIC 9(3)V9(6).
PROCEDURE DIVISION USING L.
    MOVE L TO LE
    DISPLAY "L8 " LE
    GOBACK.
END PROGRAM PB4516SUB.
END PROGRAM PB4516ONE.
