*>--- conformance-case
*> id: pb3969_format1_numeric_function_value
*> rules: [GR-14.2.3-9]
*> accept: [85, 2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3969, docs/CONFORMANCE.md D-B4: A NUMERIC FUNCTION'S VALUE REACHES A FORMAL AS ITS VALUE, NOT AS THE LEADING POSITIONS OF A
*> WIDE CONTAINER. A Format-1 CALL cannot see the formal, and ISO 14.2.3 GR9's first regime moves a BY CONTENT argument into a
*> record the length of the argument "without conversion":
*>   cite.py --check 14.2.3 "That argument is moved to this allocated record without conversion"
*>   -> OK  14.2.3 9)  (General rules)
*> but a function's returned value has no representation to move: it is "a returned value in a temporary elementary data item"
*> (15.4) whose "characteristics and representation" are implementor-defined under native arithmetic:
*>   cite.py --check 15.4.1 "When native arithmetic is in effect, the characteristics and representation of the returned value are defined by the implementor"
*>   -> OK  15.4.1  (Numeric and integer functions)
*> so the compiler's choice (D-B4) is that the value is the sender and the formal's description the receiver, which is what the
*> second regime states for a numeric formal, a COMPUTE without ROUNDED:
*>   cite.py --check 14.2.3 "if the formal parameter is numeric, a COMPUTE statement without the ROUNDED phrase"
*>   -> OK  14.2.3 9)  (General rules)
*> Before the fix the callee's DISPLAY formal read the first positions of the value's 38-digit container: FUNCTION MAX(W 0)
*> with W = 1.25 showed +0.00 in a PIC S9V99 formal, a COMP formal showed garbage, and a PIC X formal showed 00000.
*> EXPECTED OUTPUT, derived (W PIC S9V99 = 1.25, N PIC S9V99 = -1.25):
*>   A  MAX(W 0) = 1.25 into PIC S9V99                    -> +1.25
*>   A  INTEGER(W) = 1 into PIC S9V99                     -> +1.00
*>   A  MIN(N 0) = -1.25 into PIC S9V99                   -> -1.25
*>   B  MAX(W 0) into PIC 9(3)V99 (unsigned, wider)       -> 001.25
*>   C  MAX(W 0) into PIC S9(4)V99 COMP                   -> +0001.25
*>   D  INTEGER(W) = 1 into PIC X(5): the MOVE of an integer function value, left-aligned and space filled (14.6.8.5)  -> 1 plus four spaces
*>   M  MOVE FUNCTION INTEGER(W) TO X5, the same record the CALL's formal holds -> 1 plus four spaces
IDENTIFICATION DIVISION.
PROGRAM-ID. PB3969M.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 W PIC S9V99 VALUE 1.25.
01 N PIC S9V99 VALUE -1.25.
01 X5 PIC X(5).
PROCEDURE DIVISION.
MAIN.
    CALL "PB3969A" USING BY CONTENT FUNCTION MAX(W 0)
    CALL "PB3969A" USING BY CONTENT FUNCTION INTEGER(W)
    CALL "PB3969A" USING BY CONTENT FUNCTION MIN(N 0)
    CALL "PB3969B" USING BY CONTENT FUNCTION MAX(W 0)
    CALL "PB3969C" USING BY CONTENT FUNCTION MAX(W 0)
    CALL "PB3969D" USING BY CONTENT FUNCTION INTEGER(W)
    MOVE FUNCTION INTEGER(W) TO X5
    DISPLAY "M [" X5 "]"
    STOP RUN.

IDENTIFICATION DIVISION.
PROGRAM-ID. PB3969A.
DATA DIVISION.
LINKAGE SECTION.
01 LF PIC S9V99.
PROCEDURE DIVISION USING LF.
    DISPLAY "A [" LF "]"
    EXIT PROGRAM.
END PROGRAM PB3969A.

IDENTIFICATION DIVISION.
PROGRAM-ID. PB3969B.
DATA DIVISION.
LINKAGE SECTION.
01 LF PIC 9(3)V99.
PROCEDURE DIVISION USING LF.
    DISPLAY "B [" LF "]"
    EXIT PROGRAM.
END PROGRAM PB3969B.

IDENTIFICATION DIVISION.
PROGRAM-ID. PB3969C.
DATA DIVISION.
LINKAGE SECTION.
01 LF PIC S9(4)V99 COMP.
PROCEDURE DIVISION USING LF.
    DISPLAY "C [" LF "]"
    EXIT PROGRAM.
END PROGRAM PB3969C.

IDENTIFICATION DIVISION.
PROGRAM-ID. PB3969D.
DATA DIVISION.
LINKAGE SECTION.
01 LF PIC X(5).
PROCEDURE DIVISION USING LF.
    DISPLAY "D [" LF "]"
    EXIT PROGRAM.
END PROGRAM PB3969D.
END PROGRAM PB3969M.
