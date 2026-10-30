*>--- conformance-case
*> id: pb3966-initialize-replacing-data-pointer-restricted-sender-names-receiver
*> rules: [SR-14.9.39.3-19, GR-14.9.20.4-4, GR-13.18.60.4-23]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3966 - the converse direction of pb3966-initialize-replacing-data-pointer-names-receiver:
*> 14.9.39.3 SR19's continuation is "If identifier-6 references a restricted data-pointer, either identifier-5
*> shall reference a data-pointer restricted to the same type ...", so a RESTRICTED sender (PRT, restricted to
*> TT: 13.18.60.4 GR23) cannot be stored into an UNRESTRICTED receiver, because that receiver could then be
*> rebased over storage of another type. The receiver is PQ, subordinate to the strongly-typed group GRP that the
*> INITIALIZE names, so the diagnostic has to name the statement and PQ as well as PRT (14.9.20.4 GR4 makes the
*> store "SET receiving-operand TO sending-operand").
*> cite.py: OK  14.9.39.3 19)  /  OK  13.18.60.4 23)  /  OK  14.9.20.4 4)
*> Refused COBOL0869 at every edition that has TYPEDEF; below 2002 the whole family draws the introduction gate.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB3966N2.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 TT TYPEDEF STRONG.
   05 TT-A PIC X(4).
01 PTR-T TYPEDEF USAGE POINTER TO TT.
01 GT TYPEDEF STRONG.
   05 PQ USAGE POINTER.
01 GRP TYPE GT.
01 PRT TYPE PTR-T.
PROCEDURE DIVISION.
MAIN-P.
    INITIALIZE GRP REPLACING DATA-POINTER DATA BY PRT
    STOP RUN.
