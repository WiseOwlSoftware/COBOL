*> reject-at: 2002 2014 2023
*> The §15.3 class screen extended to the batch-2 functions: ACOS §15.8.3 r1, ASIN §15.10.3 r1 and
*> ATAN §15.11.3 r1 each read "Argument-1 shall be of class numeric". Each was enforced only once its
*> function was added to IntrinsicArgumentRules.Verified - a function ABSENT from that table is screened
*> exactly as before, which is what made PB1 safe to land incrementally.
*> (ACOS/ASIN/ATAN are post-85 intrinsics, so 85 rejects these earlier for a different reason.)
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPB1TRIG.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 A PIC X(3) VALUE "abc".
01 R PIC S9(6)V99.
PROCEDURE DIVISION.
MAIN.
    COMPUTE R = FUNCTION ACOS(A).
    COMPUTE R = FUNCTION ASIN(A).
    COMPUTE R = FUNCTION ATAN(A).
    STOP RUN.
