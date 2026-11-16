      *>--- conformance-case
      *> id: g2002_cd_record_object_reference_kinds
      *> rules: [2002-GR-13.16.58.3-14]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.58.3 GR14 - an object reference "shall contain
      *> either null or a reference to an object", within the limits of
      *> its descriptor. Each kind the 2002 format allows outside a
      *> method is declared as a level-1 record of an output CD
      *> (§2002:13.16.58.2 SR13 permits level 1; ACTIVE-CLASS is refused
      *> there by g2002-cd-record-active-class, SR15). The records
      *> implicitly redefine one area (§2002:A.1.2.2 SR9), so each is
      *> set and used before the next is set, and no SET has a sender
      *> sharing that area.
      *> INVOKE resolves on the object held (§2002:14.8.22.3 GR2), so
      *> each line names the class of the object the reference holds:
      *>  1 GR14d 2.b (ONLY, no FACTORY): INVOKE G2UAB "NEW" returns a
      *>    reference to a new instance of G2UAB (§2002:16.1.1.1 GR1)
      *>    into CBO; WHO prints  BASE.
      *>  2 GR14d 1.b (no ONLY, no FACTORY): D holds a G2UAD instance,
      *>    a subclass; SET CB TO D places that reference in CB
      *>    (§2002:14.8.35.3 GR8); WHO prints  DERIVED.
      *>  3 GR14d 1.a (FACTORY, no ONLY): SET CFX TO G2UAD places the
      *>    factory object of the subclass in CFX (§2002:14.8.35.3 GR9);
      *>    TAG prints  FTAG-D.
      *>  4 GR14d 2.a (FACTORY ONLY): SET CFO TO G2UAB, the factory of
      *>    exactly the class; TAG prints  FTAG-B.
      *>  5 GR14c (interface-name-1): SET CI TO D; a G2UAD instance
      *>    implements G2UAI; PING prints  IPING-D.
      *>  6 GR14b (universal): CU holds the factory object of G2UAB
      *>    (TAG prints FTAG-B) and then the G2UAD instance (WHO prints
      *>    DERIVED) - "Its content may be a reference to any object".
      *>  7 GR14 null: SET CU TO NULL; CU = NULL is true, CU NULL.
      *> A line fails if a CD object reference does not keep the
      *> reference placed in it or dispatches on the declared class.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2UA03.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           INTERFACE G2UAI
           CLASS G2UAB
           CLASS G2UAD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  D   USAGE OBJECT REFERENCE G2UAD.
       COMMUNICATION SECTION.
       CD  CO FOR OUTPUT.
       01  CBO USAGE OBJECT REFERENCE G2UAB ONLY.
       01  CB  USAGE OBJECT REFERENCE G2UAB.
       01  CFX  USAGE OBJECT REFERENCE FACTORY OF G2UAB.
       01  CFO USAGE OBJECT REFERENCE FACTORY OF G2UAB ONLY.
       01  CI  USAGE OBJECT REFERENCE G2UAI.
       01  CU  USAGE OBJECT REFERENCE.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE G2UAB "NEW" RETURNING CBO
           INVOKE CBO "WHO"
           INVOKE G2UAD "NEW" RETURNING D
           SET CB TO D
           INVOKE CB "WHO"
           SET CFX TO G2UAD
           INVOKE CFX "TAG"
           SET CFO TO G2UAB
           INVOKE CFO "TAG"
           SET CI TO D
           INVOKE CI "PING"
           SET CU TO G2UAB
           INVOKE CU "TAG"
           SET CU TO D
           INVOKE CU "WHO"
           SET CU TO NULL
           IF CU = NULL
               DISPLAY "CU NULL"
           ELSE
               DISPLAY "CU NOT NULL"
           END-IF
           STOP RUN.
       END PROGRAM G2UA03.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. G2UAI.
       PROCEDURE DIVISION.
       METHOD-ID. PING.
       PROCEDURE DIVISION.
       END METHOD PING.
       END INTERFACE G2UAI.

       IDENTIFICATION DIVISION.
       CLASS-ID. G2UAB INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           INTERFACE G2UAI.
       IDENTIFICATION DIVISION.
       FACTORY.
       PROCEDURE DIVISION.
       METHOD-ID. TAG.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "FTAG-B".
       END METHOD TAG.
       END FACTORY.
       IDENTIFICATION DIVISION.
       OBJECT. IMPLEMENTS G2UAI.
       PROCEDURE DIVISION.
       METHOD-ID. PING.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "IPING-B".
       END METHOD PING.
       METHOD-ID. WHO.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "BASE".
       END METHOD WHO.
       END OBJECT.
       END CLASS G2UAB.

       IDENTIFICATION DIVISION.
       CLASS-ID. G2UAD INHERITS FROM G2UAB.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           INTERFACE G2UAI
           CLASS G2UAB.
       IDENTIFICATION DIVISION.
       FACTORY.
       PROCEDURE DIVISION.
       METHOD-ID. TAG OVERRIDE.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "FTAG-D".
       END METHOD TAG.
       END FACTORY.
       IDENTIFICATION DIVISION.
       OBJECT. IMPLEMENTS G2UAI.
       PROCEDURE DIVISION.
       METHOD-ID. PING OVERRIDE.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "IPING-D".
       END METHOD PING.
       METHOD-ID. WHO OVERRIDE.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "DERIVED".
       END METHOD WHO.
       END OBJECT.
       END CLASS G2UAD.
