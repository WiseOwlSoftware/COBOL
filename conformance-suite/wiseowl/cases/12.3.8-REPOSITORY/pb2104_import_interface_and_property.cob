      *>--- conformance-case
      *> id: pb2104_import_interface_and_property
      *> rules: [SR-12.3.8.3-9, GR-12.3.8.4-9, SR-12.3.8.3-16]
      *> accept: [2002, 2014, 2023]
      *> files: [pb2104_speaker_lib.cbl]
      *> build: ["pb2104_speaker_lib.cbl > P2104SPK", "{self}"]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB2104 (DESIGN-external-repository 8.4, 8.5, 10.6).
      *> 12.3.8.3 syntax rule 9: "there shall be information in the
      *> external repository for the interface" a specifier names
      *> (the same rule 6 states for a class), and syntax rule 16: a
      *> PROPERTY specifier needs "information in the external
      *> repository for the property ... that is part of one of the
      *> classes or interfaces that are declared in this REPOSITORY
      *> paragraph". The interface P2104-Speaker, the class
      *> P2104-Dog that implements it and the class P2104-Acct with a
      *> property BAL are defined by another compilation group (the
      *> build step); this program names them by other words, SP, DOG
      *> and ACCT, through AS. A reference of the imported interface
      *> holds an object of the imported class (11.8.4 general rule
      *> 1: an object implements the interfaces its class does) and
      *> of a class this program defines itself that implements the
      *> imported interface (11.8.3 syntax rule 1); the property is
      *> set and got through the imported class's accessors (8.4.3.9).
      *> Each line can fail: 01 and 02 if the interface or the class is
      *> not found, or the method crosses wrongly, 03 if the property's
      *> accessors are not the imported class's.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2104IFP.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS DOG AS "P2104-Dog"
           CLASS ACCT AS "P2104-Acct"
           CLASS P2104CAT
           INTERFACE SP AS "P2104-Speaker"
           PROPERTY BAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S OBJECT REFERENCE SP.
       01 A OBJECT REFERENCE ACCT.
       01 R PIC X(5).
       01 W PIC 9(5).
       PROCEDURE DIVISION.
           INVOKE DOG "New" RETURNING S
           INVOKE S "SPEAK" RETURNING R
           DISPLAY "01 IMPORTED " R
           INVOKE P2104CAT "New" RETURNING S
           INVOKE S "SPEAK" RETURNING R
           DISPLAY "02 OWN      " R
           INVOKE ACCT "New" RETURNING A
           MOVE 7 TO BAL OF A
           MOVE BAL OF A TO W
           DISPLAY "03 PROPERTY " W
           STOP RUN.
       END PROGRAM P2104IFP.

       IDENTIFICATION DIVISION.
       CLASS-ID. P2104CAT INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           INTERFACE SP AS "P2104-Speaker".
       OBJECT. IMPLEMENTS SP.
       PROCEDURE DIVISION.
       METHOD-ID. SPEAK.
       DATA DIVISION.
       LINKAGE SECTION.
       01 OUT-TEXT PIC X(5).
       PROCEDURE DIVISION RETURNING OUT-TEXT.
           MOVE "MEOW" TO OUT-TEXT.
       END METHOD SPEAK.
       END OBJECT.
       END CLASS P2104CAT.
