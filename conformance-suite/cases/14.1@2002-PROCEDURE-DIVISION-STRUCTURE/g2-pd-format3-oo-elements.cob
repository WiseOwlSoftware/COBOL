      *>--- conformance-case
      *> id: g2-pd-format3-oo-elements
      *> rules: [2002-SR-14.1.2-12]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.1.2 SR12 - "Format 3 may be specified in a source
      *>   element if and only if that source element is a factory
      *>   definition, an instance definition, or an interface
      *>   definition, but not in a method definition." The GRANTED
      *>   arms, all three: Format 3 (§2002:14.1.1: PROCEDURE DIVISION.
      *>   [ { method-definition } ... ]) in the factory definition of
      *>   G2PDF3C, in its instance (OBJECT) definition, and in the
      *>   interface definition G2PDF3I. (§2002:14.1.2 SR13 makes the
      *>   object-oriented format the only one those three may take.)
      *> DERIVATION - each line is printed by a method defined inside
      *>   one of the Format 3 procedure divisions, so each line shows
      *>   that division accepted and its method definition bound:
      *>   FACTORY-METHOD - INVOKE G2PDF3C "FM" (factory Format 3).
      *>   INSTANCE-METHOD - INVOKE O "IM" (instance Format 3).
      *>   VIA-INTERFACE - INVOKE S "SPEAK" through a reference typed
      *>     by G2PDF3I, whose Format 3 division holds the SPEAK
      *>     method prototype the OBJECT paragraph IMPLEMENTS.
      *> Fails if: any of the three Format 3 divisions is refused, or a
      *>   method inside one is not bound (a line missing).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2PDF3M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS G2PDF3C
           INTERFACE G2PDF3I.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE G2PDF3C.
       01 S USAGE OBJECT REFERENCE G2PDF3I.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE G2PDF3C "FM"
           INVOKE G2PDF3C "NEW" RETURNING O
           INVOKE O "IM"
           SET S TO O
           INVOKE S "SPEAK"
           STOP RUN.
       END PROGRAM G2PDF3M.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. G2PDF3I.
       PROCEDURE DIVISION.
       METHOD-ID. SPEAK.
       PROCEDURE DIVISION.
       END METHOD SPEAK.
       END INTERFACE G2PDF3I.

       IDENTIFICATION DIVISION.
       CLASS-ID. G2PDF3C INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           INTERFACE G2PDF3I.
       IDENTIFICATION DIVISION.
       FACTORY.
       PROCEDURE DIVISION.
       METHOD-ID. FM.
       PROCEDURE DIVISION.
       FM-P.
           DISPLAY "FACTORY-METHOD".
       END METHOD FM.
       END FACTORY.
       IDENTIFICATION DIVISION.
       OBJECT. IMPLEMENTS G2PDF3I.
       PROCEDURE DIVISION.
       METHOD-ID. IM.
       PROCEDURE DIVISION.
       IM-P.
           DISPLAY "INSTANCE-METHOD".
       END METHOD IM.
       METHOD-ID. SPEAK.
       PROCEDURE DIVISION.
       SP-P.
           DISPLAY "VIA-INTERFACE".
       END METHOD SPEAK.
       END OBJECT.
       END CLASS G2PDF3C.
