      *>--- conformance-case
      *> id: g2fac04-factory-method-missing
      *> rules: [2002-SR-11.3.2-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:11.3.2 SR2 - refused: G2F4XC's factory implements G2F4XI
      *> but neither writes nor inherits GETV; §2002:9.3.7.1.2 rule 1:
      *> 'For every method in interface-2 there is a method in
      *> interface-1 with the same name' fails, so no conformance.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2F4X01.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS G2F4XC
           INTERFACE G2F4XI.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S USAGE OBJECT REFERENCE G2F4XI.
       01 N PIC 9(4) VALUE 42.
       01 V PIC 99 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-P.
           SET S TO G2F4XC.
           INVOKE S "SPEAK" USING N.
           INVOKE S "GETV" RETURNING V.
           DISPLAY "GOT " V.
           INVOKE S "HELLO".
           STOP RUN.
       END PROGRAM G2F4X01.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. G2F4XI.
       PROCEDURE DIVISION.
       IDENTIFICATION DIVISION.
       METHOD-ID. SPEAK.
       DATA DIVISION.
       LINKAGE SECTION.
       01 P PIC 9(4).
       PROCEDURE DIVISION USING P.
       END METHOD SPEAK.
       IDENTIFICATION DIVISION.
       METHOD-ID. GETV.
       DATA DIVISION.
       LINKAGE SECTION.
       01 R PIC 99.
       PROCEDURE DIVISION RETURNING R.
       END METHOD GETV.
       IDENTIFICATION DIVISION.
       METHOD-ID. HELLO.
       PROCEDURE DIVISION.
       END METHOD HELLO.
       END INTERFACE G2F4XI.

       IDENTIFICATION DIVISION.
       CLASS-ID. G2F4XB INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       FACTORY.
       PROCEDURE DIVISION.
       IDENTIFICATION DIVISION.
       METHOD-ID. HELLO.
       PROCEDURE DIVISION.
           DISPLAY "BASE HELLO".
       END METHOD HELLO.
       END FACTORY.
       END CLASS G2F4XB.

       IDENTIFICATION DIVISION.
       CLASS-ID. G2F4XC INHERITS FROM G2F4XB.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS G2F4XB
           INTERFACE G2F4XI.
       IDENTIFICATION DIVISION.
       FACTORY. IMPLEMENTS G2F4XI.
       PROCEDURE DIVISION.
       IDENTIFICATION DIVISION.
       METHOD-ID. SPEAK.
       DATA DIVISION.
       LINKAGE SECTION.
       01 P PIC 9(4).
       PROCEDURE DIVISION USING P.
           DISPLAY "FAC SPEAK " P.
       END METHOD SPEAK.
       END FACTORY.
       END CLASS G2F4XC.
