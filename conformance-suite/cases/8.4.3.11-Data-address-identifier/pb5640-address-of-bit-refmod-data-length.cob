*>--- conformance-case
*> id: pb5640-address-of-bit-refmod-data-length
*> rules: [SR-8.4.3.11.3-4]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB5640. §2023:8.4.3.11.3 SR4 a): "subscripting and reference modification in identifier-1 consist of only fixed-point numeric
      *> literals or arithmetic expressions in which all operands are fixed-point numeric literals and the exponentiation operator is not
      *> specified" — the leftmost position AND the length (§2014:8.4.2.11.2 SR4, §2002:8.4.2.11.2 SR3 say the same). Unlike CALL and INVOKE
      *> at 2023, ADDRESS OF does not narrow the rule to the leftmost position, so BX(1:N) is refused at every edition (COBOL2786); BX(1:8)
      *> is legal.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5640C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BX PIC 1(16) USAGE BIT.
       01 N PIC 9 VALUE 8.
       01 P USAGE POINTER.
       PROCEDURE DIVISION.
       MAIN.
           SET P TO ADDRESS OF BX(1:8)
           SET P TO ADDRESS OF BX(1:N)
           STOP RUN.
