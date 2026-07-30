      *> PB2 - a FLOATING-POINT argument to an exact-family intrinsic.
      *> ISO 15.7.3 r1 and its siblings require class NUMERIC; a COMP-2 item IS class numeric (8.5.2.1
      *> Table 2), so every call below is legal COBOL. Before this fix the emitter dispatched on the
      *> FUNCTION's family rather than the ARGUMENT's type and handed a double to an Int128 parameter, so
      *> ten of these produced a raw Roslyn CS1503 that escaped as a backend error on legal source.
      *>
      *> Every expected value is DERIVED FROM THE SPEC, not observed: the sign-discriminating pairs are the
      *> point of the fixture, because a wrong body agrees with a right one on positive operands.
      *>   15.64.4 MOD          floored  - result takes the sign of argument-2 => MOD(-7,3)  =  2
      *>   15.77.4 REM          truncated- result takes the sign of argument-1 => REM(-7,3)  = -1
      *>   15.44   INTEGER      greatest integer NOT GREATER than the argument => INTEGER(-3.5)      = -4
      *>   15.49   INTEGER-PART the integer part, truncated toward zero        => INTEGER-PART(-3.5) = -3
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2FLOATARG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A    USAGE COMP-2 VALUE -7.0.
       01 B    USAGE COMP-2 VALUE 3.0.
       01 C    USAGE COMP-2 VALUE -3.5.
       01 D    USAGE COMP-2 VALUE 3.5.
       01 E    USAGE COMP-2 VALUE 7.25.
       01 R    USAGE COMP-2.
       01 FX   PIC S9(4)V99 VALUE 0.
       PROCEDURE DIVISION.
           COMPUTE R = FUNCTION MOD(A B)
           DISPLAY R
           COMPUTE R = FUNCTION REM(A B)
           DISPLAY R
           COMPUTE R = FUNCTION INTEGER(C)
           DISPLAY R
           COMPUTE R = FUNCTION INTEGER-PART(C)
           DISPLAY R
           COMPUTE R = FUNCTION SIGN(C)
           DISPLAY R
           COMPUTE R = FUNCTION ABS(C)
           DISPLAY R
           COMPUTE R = FUNCTION FRACTION-PART(D)
           DISPLAY R
           COMPUTE R = FUNCTION MAX(D E)
           DISPLAY R
           COMPUTE R = FUNCTION MIN(D E)
           DISPLAY R
           COMPUTE R = FUNCTION SUM(D E)
           DISPLAY R
           COMPUTE R = FUNCTION MEAN(D E)
           DISPLAY R
           COMPUTE R = FUNCTION RANGE(D E)
           DISPLAY R
           COMPUTE R = FUNCTION MEDIAN(D E)
           DISPLAY R
           COMPUTE R = FUNCTION MIDRANGE(D E)
           DISPLAY R
           COMPUTE R = FUNCTION ORD-MAX(D E)
           DISPLAY R
           COMPUTE R = FUNCTION ORD-MIN(D E)
           DISPLAY R
           COMPUTE FX = FUNCTION ABS(C)
           DISPLAY FX
           STOP RUN.
