*>--- conformance-case
*> id: pb3817-initialize-replacing-restricted-program-pointer
*> rules: [SR-14.9.20.3-4, GR-14.9.20.4-4, GR-13.18.60.4-25, SR-14.9.39.3-22]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3817 - ISO 14.9.20.4 GR4: "If the category of a receiving-operand is data-pointer, function-pointer,
      *> message-tag, object-reference, or program-pointer, the implicit statement is: SET receiving-operand TO
      *> sending-operand". Each program-pointer INITIALIZE reaches is therefore a SET receiver, and 13.18.60.4 GR25
      *> ("A restricted program-pointer shall contain only the predefined address NULL or the address of a program
      *> with the same signature as that identified by the specified program-prototype-name") and 14.9.39.3 SR22
      *> ("If identifier-7 references a restricted program-pointer, identifier-8 shall be the predefined address
      *> NULL or shall reference a program-pointer and the program-prototypes associated with identifier-7 and
      *> identifier-8 shall have the same signature") hold it. PU is unrestricted, so it is associated with no
      *> program-prototype; the restricted GT-R would be handed an address GR25 forbids it to contain.
      *> cite.py: OK  13.18.60.4 25)  /  OK  14.9.39.3 22)  /  OK  14.9.20.4 4)
      *> GT-U, the group's FIRST program-pointer, is unrestricted and a valid receiver; the refusal is asked of the
      *> SECOND, restricted one, so a check made only of the first receiver (or of the phrase) would pass this.
      *> Refused COBOL1959 (prototype-pointer-signature) at every edition that has the program-pointer.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PBT3817N IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 9(4).
       PROCEDURE DIVISION USING L-X.
       END PROGRAM PBT3817N.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3817N1.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM PBT3817N.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PPT IS TYPEDEF USAGE PROGRAM-POINTER TO PBT3817N.
       01 GT IS TYPEDEF STRONG.
          05 GT-U USAGE PROGRAM-POINTER.
          05 GT-R TYPE PPT.
       01 GP TYPE GT.
       01 PU USAGE PROGRAM-POINTER.
       PROCEDURE DIVISION.
       MAIN-P.
           INITIALIZE GP REPLACING PROGRAM-POINTER DATA BY PU
           STOP RUN.
       END PROGRAM PB3817N1.
