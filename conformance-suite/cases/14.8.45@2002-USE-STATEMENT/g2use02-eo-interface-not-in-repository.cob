      *>--- conformance-case
      *> id: g2use02-eo-interface-not-in-repository
      *> rules: [2002-SR-14.8.45.2-17]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.45.2 SR17 - "Interface-name-1 shall be the name of an
      *>   interface specified in the REPOSITORY paragraph." G2IF02 is an
      *>   interface of this compilation group, but G2USE05's REPOSITORY
      *>   paragraph specifies only CLASS BASE, so the declarative section
      *>   at line 22 (its USE statement) is refused. This refusal stands
      *>   beside the accepted case g2use01-eo-interface-in-repository; it
      *>   is not the closing witness.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2USE05.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       PROCEDURE DIVISION.
       DECLARATIVES.
       EO-SEC SECTION.
           USE AFTER EXCEPTION OBJECT G2IF02.
       EO-P.
           DISPLAY "HANDLER".
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           STOP RUN.
       END PROGRAM G2USE05.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. G2IF02.
       END INTERFACE G2IF02.
