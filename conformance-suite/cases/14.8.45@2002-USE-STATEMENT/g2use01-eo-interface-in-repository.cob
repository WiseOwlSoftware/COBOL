      *>--- conformance-case
      *> id: g2use01-eo-interface-in-repository
      *> rules: [2002-SR-14.8.45.2-17]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.45.2 SR17 - "Interface-name-1 shall be the name of an
      *>   interface specified in the REPOSITORY paragraph." G2IF01 is a
      *>   non-parameterized interface specified in G2USE04's REPOSITORY,
      *>   so both USE AFTER EXCEPTION OBJECT G2IF01 and its synonym
      *>   USE AFTER EO G2IF01 (§2002:14.8.45.2 SR15) are ACCEPTED, one in
      *>   each of the two programs below.
      *> That the name means the interface is shown by running it:
      *>   G2CL01 is described with IMPLEMENTS G2IF01, so a RAISE of an
      *>   instance of it selects the declarative (§2002:14.8.45.3 GR13b:
      *>   "is described with an IMPLEMENTS clause that references
      *>   interface-name-1, the associated declarative is executed") and,
      *>   once it completes, execution continues after the RAISE
      *>   (§2002:14.5.12.1.4). Expected: IFACE-1, AFTER-1, IFACE-2,
      *>   AFTER-2. A compiler that refused the interface name fails to
      *>   compile; one that resolved it to anything else does not run the
      *>   declarative.
      *> Refused beside it (not the closing witness): an interface defined
      *>   in the compilation group but not specified in the REPOSITORY
      *>   paragraph, g2use02-eo-interface-not-in-repository. The
      *>   unexpanded parameterized interface is WiseOwl's recorded
      *>   determination (kb/Work PB5625 reading A), not this case's.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2USE04.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS G2CL01
           INTERFACE G2IF01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  OX              USAGE OBJECT REFERENCE G2CL01.
       PROCEDURE DIVISION.
       DECLARATIVES.
       IF-SEC SECTION.
           USE AFTER EXCEPTION OBJECT G2IF01.
       IF-P.
           DISPLAY "IFACE-1".
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           INVOKE G2CL01 "NEW" RETURNING OX
           RAISE OX
           DISPLAY "AFTER-1"
           CALL "G2USE01B"
           STOP RUN.
       END PROGRAM G2USE04.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2USE01B.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS G2CL01
           INTERFACE G2IF01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  OY              USAGE OBJECT REFERENCE G2CL01.
       PROCEDURE DIVISION.
       DECLARATIVES.
       EO-SEC SECTION.
           USE AFTER EO G2IF01.
       EO-P.
           DISPLAY "IFACE-2".
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           INVOKE G2CL01 "NEW" RETURNING OY
           RAISE OY
           DISPLAY "AFTER-2"
           GOBACK.
       END PROGRAM G2USE01B.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. G2IF01.
       END INTERFACE G2IF01.

       IDENTIFICATION DIVISION.
       CLASS-ID. G2CL01 INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           INTERFACE G2IF01.
       IDENTIFICATION DIVISION.
       OBJECT.
       IMPLEMENTS G2IF01.
       END OBJECT.
       END CLASS G2CL01.
