*> reject-at: 85 2002 2014 2023
*> PB19 - LOWER-CASE's argument rule (15.57.3 r1) requires class alphabetic, alphanumeric, or national, and was enforced NOWHERE: the
*> function was absent from IntrinsicArgumentRules.Verified, so CheckArgumentClasses returned at its
*> TryGetValue guard and an operand of the wrong class was silently accepted and reinterpreted.
*> The legal shapes are pinned by conformance:2023/pb19_argument_class_batch5.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPB19.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 N PIC 9(4) VALUE 1234.
01 S PIC X(8).
PROCEDURE DIVISION.
MAIN.
    MOVE FUNCTION LOWER-CASE(N) TO S
    STOP RUN.
