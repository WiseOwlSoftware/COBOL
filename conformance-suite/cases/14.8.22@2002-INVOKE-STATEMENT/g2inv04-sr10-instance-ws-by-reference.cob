      *>--- conformance-case
      *> id: g2inv04-sr10-instance-ws-by-reference
      *> rules: [2002-SR-14.8.22.2-10]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.22.2 SR10 - "Identifier-3 shall not reference a
      *> data item defined in the file or working-storage section of a
      *> factory or instance object". OD is in the instance
      *> object's working-storage and is written BY REFERENCE, so it is
      *> identifier-3: refused at the INVOKE line in RUNIT.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2INV12.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS G2INV04C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T USAGE OBJECT REFERENCE G2INV04C.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE G2INV04C "NEW" RETURNING T.
           INVOKE T "RUNIT".
           STOP RUN.
       END PROGRAM G2INV12.

       IDENTIFICATION DIVISION.
       CLASS-ID. G2INV04C INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 OD PIC 9(2) VALUE 5.
       PROCEDURE DIVISION.
       METHOD-ID. RUNIT.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE SELF "BUMP" USING BY REFERENCE OD.
           DISPLAY "OD=" OD.
       END METHOD RUNIT.
       METHOD-ID. BUMP.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-N PIC 9(2).
       PROCEDURE DIVISION USING LK-N.
       MAIN.
           ADD 1 TO LK-N.
       END METHOD BUMP.
       END OBJECT.
       END CLASS G2INV04C.
