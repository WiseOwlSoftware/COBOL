*>--- conformance-case
*> id: pb3966-initialize-replacing-data-pointer-names-receiver
*> rules: [SR-14.9.39.3-19, GR-14.9.20.4-4, GR-13.18.60.4-23]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3966 - the refusal of pb3817-initialize-replacing-restricted-data-pointer, now for a restricted
*> pointer that is SUBORDINATE to the INITIALIZE operand. 14.9.20.4 GR4 makes the REPLACING store, per receiving
*> operand, "SET receiving-operand TO sending-operand"; 14.9.39.3 SR19 holds that SET ("If identifier-5
*> references a restricted data-pointer, identifier-6 shall be the predefined address NULL or shall reference a
*> data-pointer restricted to the same type") and 13.18.60.4 GR23 says a restricted data-pointer "shall contain
*> only the predefined address NULL or the address of a data item of the specified type". GRP is a strongly-typed
*> group (13.18.60.3 SR14 admits a pointer clause only in one) and the receiver is PR, which the INITIALIZE
*> never names: PR is restricted to TT, PU is not. The program-pointer, function-pointer and object-reference
*> arms of the same screen already name the statement and the receiver; the data-pointer arm named neither, so
*> the user was told about 'PU' alone and could not find PR in a group of many pointers.
*> cite.py: OK  14.9.39.3 19)  /  OK  13.18.60.4 23)  /  OK  14.9.20.4 4)
*> Refused COBOL0869 at every edition that has TYPEDEF; below 2002 the whole family draws the introduction gate.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB3966N1.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 TT TYPEDEF STRONG.
   05 TT-A PIC X(4).
01 PTR-T TYPEDEF USAGE POINTER TO TT.
01 GT TYPEDEF STRONG.
   05 PR TYPE PTR-T.
   05 PQ USAGE POINTER.
01 GRP TYPE GT.
01 PU USAGE POINTER.
PROCEDURE DIVISION.
MAIN-P.
    INITIALIZE GRP REPLACING DATA-POINTER DATA BY PU
    STOP RUN.
