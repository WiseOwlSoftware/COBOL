*>--- conformance-case
*> id: pb3817-initialize-replacing-restricted-function-pointer
*> rules: [SR-14.9.20.3-4, GR-14.9.20.4-4, GR-13.18.60.4-26, SR-14.9.39.3-20]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3817 - the function-pointer sibling of pb3817-initialize-replacing-restricted-program-pointer.
      *> 14.9.20.4 GR4 makes the REPLACING store of a function-pointer "SET receiving-operand TO sending-operand",
      *> and 14.9.39.3 SR20 holds that SET: "The function-prototypes associated with identifier-12 and
      *> identifier-13 shall have the same signature". FP2 is associated with FB (one alphanumeric argument) and
      *> FP1 with FA (one numeric argument), so the signatures differ and FP1's value is not one FP2 may hold.
      *> cite.py: OK  14.9.39.3 20)
      *> Function-pointers exist from COBOL-2014 (13.18.60); refused COBOL1959 (prototype-pointer-signature) there
      *> and at 2023.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PBFA3817.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-ARG PIC 9(9).
       01 L-RES PIC 9(9).
       PROCEDURE DIVISION USING L-ARG RETURNING L-RES.
           GOBACK.
       END FUNCTION PBFA3817.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PBFB3817.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-ARG PIC X(2).
       01 L-RES PIC 9(9).
       PROCEDURE DIVISION USING L-ARG RETURNING L-RES.
           GOBACK.
       END FUNCTION PBFB3817.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3817N2.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION PBFA3817
           FUNCTION PBFB3817.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FP1 USAGE FUNCTION-POINTER TO PBFA3817.
       01 FP2 USAGE FUNCTION-POINTER TO PBFB3817.
       PROCEDURE DIVISION.
       MAIN-P.
           SET FP1 TO ADDRESS OF FUNCTION PBFA3817
           INITIALIZE FP2 REPLACING FUNCTION-POINTER DATA BY FP1
           STOP RUN.
       END PROGRAM PB3817N2.
