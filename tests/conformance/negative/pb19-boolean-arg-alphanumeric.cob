*> reject-at: 85 2002 2014 2023
*> PB19 - INTEGER-OF-BOOLEAN's argument rule (15.45.3 r1) requires class boolean, and was enforced NOWHERE: the
*> function was absent from IntrinsicArgumentRules.Verified, so CheckArgumentClasses returned at its
*> TryGetValue guard and an operand of the wrong class was silently accepted and reinterpreted.
*> The legal shapes are pinned by conformance:2023/pb19_argument_class_batch5.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPB19.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 X PIC X(8) VALUE "10101010".
01 R PIC 9(4).
PROCEDURE DIVISION.
MAIN.
    COMPUTE R = FUNCTION INTEGER-OF-BOOLEAN(X)
    STOP RUN.
