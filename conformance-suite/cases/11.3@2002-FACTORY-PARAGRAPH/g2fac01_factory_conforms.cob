      *>--- conformance-case
      *> id: g2fac01_factory_conforms
      *> rules: [2002-SR-11.3.2-2, 2002-GR-11.3.3-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:11.3.2 SR2 - a factory that IMPLEMENTS an interface whose
      *> factory interface conforms to it (9.3.7.1.2) is accepted, and
      *> §2002:11.3.3 GR1 - the factory object then implements G2FACI.
      *> G2FACI has SPEAK (USING PIC 9(4)), GETV (RETURNING PIC 99) and
      *> HELLO. G2FACC's factory writes SPEAK and GETV with the same
      *> PICTURE/USAGE and RETURNING presence (9.3.7.1.2 rules 1, 3, 4);
      *> HELLO is not written there but inherited from G2FACB's factory,
      *> and §2002:9.3.7.1.1 puts inherited methods in the interface.
      *> Legs: a compiler that omits inherited factory methods from the
      *> factory interface refuses the program (SR2 wrongly applied);
      *> one that does not bind S to the factory fails SET (GR1/GR2a).
      *> Expected: SPEAK shows P (PIC 9(4), value 42) -> FAC SPEAK 0042;
      *> GETV moves 7 to R PIC 99 -> GOT 07; HELLO -> BASE HELLO.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2FAC01.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS G2FACC
           INTERFACE G2FACI.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S USAGE OBJECT REFERENCE G2FACI.
       01 N PIC 9(4) VALUE 42.
       01 V PIC 99 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-P.
           SET S TO G2FACC.
           INVOKE S "SPEAK" USING N.
           INVOKE S "GETV" RETURNING V.
           DISPLAY "GOT " V.
           INVOKE S "HELLO".
           STOP RUN.
       END PROGRAM G2FAC01.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. G2FACI.
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
       END INTERFACE G2FACI.

       IDENTIFICATION DIVISION.
       CLASS-ID. G2FACB INHERITS FROM BASE.
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
       END CLASS G2FACB.

       IDENTIFICATION DIVISION.
       CLASS-ID. G2FACC INHERITS FROM G2FACB.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS G2FACB
           INTERFACE G2FACI.
       IDENTIFICATION DIVISION.
       FACTORY. IMPLEMENTS G2FACI.
       PROCEDURE DIVISION.
       IDENTIFICATION DIVISION.
       METHOD-ID. SPEAK.
       DATA DIVISION.
       LINKAGE SECTION.
       01 P PIC 9(4).
       PROCEDURE DIVISION USING P.
           DISPLAY "FAC SPEAK " P.
       END METHOD SPEAK.
       IDENTIFICATION DIVISION.
       METHOD-ID. GETV.
       DATA DIVISION.
       LINKAGE SECTION.
       01 R PIC 99.
       PROCEDURE DIVISION RETURNING R.
           MOVE 7 TO R.
       END METHOD GETV.
       END FACTORY.
       END CLASS G2FACC.
