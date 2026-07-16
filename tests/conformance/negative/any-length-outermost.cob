*> reject-at: 2002 2014 2023
*> ISO 1989:2023 13.18.2.3 SR2 + NOTE: the ANY LENGTH clause may be specified only in the linkage
*> section of a FUNCTION, a CONTAINED program, or a non-property METHOD - never an OUTERMOST program
*> (a prototype-less CALL cannot associate arguments with an ANY LENGTH formal). COBOLNET1542.
IDENTIFICATION DIVISION.
PROGRAM-ID. ALNOUTP9AL.
DATA DIVISION.
LINKAGE SECTION.
01 L PIC X ANY LENGTH.
PROCEDURE DIVISION USING L.
MAIN.
    STOP RUN.
END PROGRAM ALNOUTP9AL.
