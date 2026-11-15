      *>--- conformance-case
      *> id: g2inv05-sr10-factory-ws-by-reference
      *> rules: [2002-SR-14.8.22.2-10]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.22.2 SR10 - "Identifier-3 shall not reference a
      *> data item defined in the file or working-storage section of a
      *> factory or instance object". FD1 is in the factory
      *> object's working-storage and is written BY REFERENCE, so it is
      *> identifier-3: refused at the INVOKE line in FRUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2INV05.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS G2INV05C.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE G2INV05C "FRUN".
           STOP RUN.
       END PROGRAM G2INV05.

       IDENTIFICATION DIVISION.
       CLASS-ID. G2INV05C INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       FACTORY.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FD1 PIC 9(2) VALUE 5.
       PROCEDURE DIVISION.
       METHOD-ID. FRUN.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE SELF "FBUMP" USING BY REFERENCE FD1.
           DISPLAY "FD1=" FD1.
       END METHOD FRUN.
       METHOD-ID. FBUMP.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-N PIC 9(2).
       PROCEDURE DIVISION USING LK-N.
       MAIN.
           ADD 1 TO LK-N.
       END METHOD FBUMP.
       END FACTORY.
       END CLASS G2INV05C.
