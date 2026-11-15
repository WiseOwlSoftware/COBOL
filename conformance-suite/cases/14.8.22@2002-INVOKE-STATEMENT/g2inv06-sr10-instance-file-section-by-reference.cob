      *>--- conformance-case
      *> id: g2inv06-sr10-instance-file-section-by-reference
      *> rules: [2002-SR-14.8.22.2-10]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.22.2 SR10 - "Identifier-3 shall not reference a
      *> data item defined in the file or working-storage section of a
      *> factory or instance object". OF1-REC is a
      *> record in the instance object's file section and is written
      *> BY REFERENCE, so it is identifier-3: refused at the INVOKE
      *> line in RUNIT.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2INV06.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS G2INV06C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T USAGE OBJECT REFERENCE G2INV06C.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE G2INV06C "NEW" RETURNING T.
           INVOKE T "RUNIT".
           STOP RUN.
       END PROGRAM G2INV06.

       IDENTIFICATION DIVISION.
       CLASS-ID. G2INV06C INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT OF1 ASSIGN TO "g2inv06.dat".
       DATA DIVISION.
       FILE SECTION.
       FD  OF1.
       01  OF1-REC PIC X(4).
       PROCEDURE DIVISION.
       METHOD-ID. RUNIT.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE SELF "TAKE" USING BY REFERENCE OF1-REC.
       END METHOD RUNIT.
       METHOD-ID. TAKE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-X PIC X(4).
       PROCEDURE DIVISION USING LK-X.
       MAIN.
           MOVE "ZZZZ" TO LK-X.
       END METHOD TAKE.
       END OBJECT.
       END CLASS G2INV06C.
