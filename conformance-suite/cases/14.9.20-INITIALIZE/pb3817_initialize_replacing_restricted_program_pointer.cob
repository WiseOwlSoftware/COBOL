*>--- conformance-case
*> id: pb3817_initialize_replacing_restricted_program_pointer
*> rules: [SR-14.9.20.3-4, GR-14.9.20.4-4, GR-14.9.20.4-6, GR-13.18.60.4-25, SR-14.9.39.3-22]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3817 - INITIALIZE ... REPLACING PROGRAM-POINTER DATA BY identifier-2 stores identifier-2 into
      *> every program-pointer of identifier-1, and each store is "SET receiving-operand TO sending-operand"
      *> (ISO 14.9.20.4 GR4), held by 14.9.39.3 SR22 and by 13.18.60.4 GR25 ("A restricted program-pointer shall
      *> contain only the predefined address NULL or the address of a program with the same signature as that
      *> identified by the specified program-prototype-name"). This is the ACCEPTED half: every store below is one
      *> a written SET would admit. The refused half is pb3817-initialize-replacing-restricted-program-pointer.
      *>
      *> EXPECTED OUTPUT, derived line by line:
      *>   RR-SAME  RP2 and RP1 are restricted to the SAME prototype, so SR22 admits the implicit SET and GR6b
      *>            makes the stored value identifier-2's: RP2 = RP1.
      *>   RR-NULL  REPLACING ... BY NULL is the implicit SET RP2 TO NULL, which GR25 admits into any restricted
      *>            program-pointer, so RP2 = NULL.
      *>   UR-SAME  an UNRESTRICTED receiver takes any program-pointer (SR22's condition is the receiver's
      *>            restriction), here the restricted RP1: UP1 = RP1.
      *>   UU-SAME  unrestricted into unrestricted: UP2 = UP1 (UP1 holds the address SET ... TO ENTRY gave it).
      *>   GRP-BOTH one INITIALIZE of a group holding a restricted AND an unrestricted program-pointer: both are
      *>            receiving operands (GR5c2), the sender is restricted to the receiver's own prototype, so the
      *>            statement is valid for each and both end equal to RP1.
      *>   CALLED 0042 the restriction is not decoration: GT-R of GP still addresses PBT3817, and a CALL through it
      *>            activates that program.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PBT3817 IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 9(4).
       PROCEDURE DIVISION USING L-X.
       END PROGRAM PBT3817.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3817OK.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM PBT3817.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PPT IS TYPEDEF USAGE PROGRAM-POINTER TO PBT3817.
       01 RP1 TYPE PPT.
       01 RP2 TYPE PPT.
       01 UP1 USAGE PROGRAM-POINTER.
       01 UP2 USAGE PROGRAM-POINTER.
       01 GT IS TYPEDEF STRONG.
          05 GT-U USAGE PROGRAM-POINTER.
          05 GT-R TYPE PPT.
       01 GP TYPE GT.
       01 WS-N PIC 9(4) VALUE 0042.
       PROCEDURE DIVISION.
       MAIN-P.
           SET RP1 TO ADDRESS OF PROGRAM PBT3817
           INITIALIZE RP2 REPLACING PROGRAM-POINTER DATA BY RP1
           IF RP2 = RP1 THEN DISPLAY "RR-SAME" ELSE DISPLAY "RR-DIFF" END-IF
           INITIALIZE RP2 REPLACING PROGRAM-POINTER DATA BY NULL
           IF RP2 = NULL THEN DISPLAY "RR-NULL" ELSE DISPLAY "RR-NOTNULL" END-IF
           INITIALIZE UP1 REPLACING PROGRAM-POINTER DATA BY RP1
           IF UP1 = RP1 THEN DISPLAY "UR-SAME" ELSE DISPLAY "UR-DIFF" END-IF
           SET UP1 TO ENTRY "PBT3817"
           INITIALIZE UP2 REPLACING PROGRAM-POINTER DATA BY UP1
           IF UP2 = UP1 THEN DISPLAY "UU-SAME" ELSE DISPLAY "UU-DIFF" END-IF
           INITIALIZE GP REPLACING PROGRAM-POINTER DATA BY RP1
           IF GT-U OF GP = RP1 AND GT-R OF GP = RP1 THEN DISPLAY "GRP-BOTH" ELSE DISPLAY "GRP-DIFF" END-IF
           CALL GT-R OF GP USING WS-N
           STOP RUN.
       END PROGRAM PB3817OK.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PBT3817.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 9(4).
       PROCEDURE DIVISION USING L-X.
           DISPLAY "CALLED " L-X
           GOBACK.
       END PROGRAM PBT3817.
