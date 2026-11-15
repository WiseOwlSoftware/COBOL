      *>--- conformance-case
      *> id: g2inv07-sr10-instance-ws-universal-receiver
      *> rules: [2002-SR-14.8.22.2-10]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.22.2 SR10 - "Identifier-3 shall not reference a
      *> data item defined in the file or working-storage section of a
      *> factory or instance object". LU is universal,
      *> so by §2002:14.8.22.2 SR6 the bare argument OD is passed BY
      *> REFERENCE (assumed implicitly): OD, in the instance object's
      *> working-storage, is identifier-3 and is refused at the INVOKE
      *> line in RUNIT.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2INV07.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS G2INV07C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T USAGE OBJECT REFERENCE G2INV07C.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE G2INV07C "NEW" RETURNING T.
           INVOKE T "RUNIT".
           STOP RUN.
       END PROGRAM G2INV07.

       IDENTIFICATION DIVISION.
       CLASS-ID. G2INV07C INHERITS FROM BASE.
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
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 LU USAGE OBJECT REFERENCE.
       PROCEDURE DIVISION.
       MAIN.
           SET LU TO SELF.
           INVOKE LU "BUMP" USING OD.
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
       END CLASS G2INV07C.
