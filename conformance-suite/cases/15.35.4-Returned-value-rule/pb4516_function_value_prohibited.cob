*>--- conformance-case
*> id: pb4516_function_value_prohibited
*> rules: [RV-15.35.4-1, GR-14.7.4.3-7]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4516 - ROUNDED MODE IS PROHIBITED asks whether the RETURNED VALUE is representable in the receiver.
      *>
      *> ISO 14.7.4.3 r7: "If the PROHIBITED phrase is specified, and the arithmetic value cannot be represented
      *> exactly in the resultant identifier, the EC-SIZE-TRUNCATION exception condition is set to exist, the size
      *> error condition exists, and the content of the resultant identifier is unchanged." The arithmetic value
      *> of FUNCTION EXP10 (-6) is the function's one returned value (15.4), which implementor determination
      *> (docs/CONFORMANCE.md, "An implementor-defined APPROXIMATION is not a data item") reads as its shortest
      *> round-trip decimal, 0.000001: representable at six places, so the COMPUTE lands it (L1).
      *> The binary expansion 9.99999999999999954748E-7 is not representable at six places, and a landing that read
      *> it would take the size error branch. At five places 0.000001 is NOT representable (L2): the size error
      *> condition exists and the receiver keeps its content (99.99999).
      *>
      *> --check validated:
      *>   cite.py --check 14.7.4.3 "If the PROHIBITED phrase is specified" -> OK (rule 7)
      *>   cite.py --check 15.4 "The evaluation of a function produces a returned value in a temporary elementary data item" -> OK
IDENTIFICATION DIVISION.
PROGRAM-ID. PB4516PRH.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 R6  PIC 9(3)V9(6) VALUE 0.
01 R5  PIC 99V9(5) VALUE 99.99999.
01 E6  PIC 9.9(6).
01 E5  PIC 99.9(5).
PROCEDURE DIVISION.
    COMPUTE R6 ROUNDED MODE IS PROHIBITED = FUNCTION EXP10 (-6)
        ON SIZE ERROR DISPLAY "L1 SIZE-ERROR"
        NOT ON SIZE ERROR
            MOVE R6 TO E6
            DISPLAY "L1 " E6
    END-COMPUTE
    COMPUTE R5 ROUNDED MODE IS PROHIBITED = FUNCTION EXP10 (-6)
        ON SIZE ERROR
            MOVE R5 TO E5
            DISPLAY "L2 SIZE-ERROR " E5
        NOT ON SIZE ERROR DISPLAY "L2 LANDED"
    END-COMPUTE
    STOP RUN.
