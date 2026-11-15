      *>--- conformance-case
      *> id: g2inv02-cd-identifier-2-typed-receiver
      *> rules: [2002-SR-14.8.22.2-7]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.22.2 SR7 - "Identifier-2 may be specified only
      *> when identifier-1 is a universal object reference". O is
      *> typed (OBJECT REFERENCE G2INV02C), so INVOKE O MNAME, whose
      *> identifier-2 is a record item under a 2002 CD entry, is
      *> refused at its line. The accepted universal form is
      *> g2inv01-cd-identifier-2-and-single-identifier-args.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2INV02.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS G2INV02C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O  USAGE OBJECT REFERENCE G2INV02C.
       COMMUNICATION SECTION.
       CD CDOUT FOR OUTPUT
           TEXT LENGTH IS OTL.
       01 OUTREC.
          05 MNAME PIC X(5).
       PROCEDURE DIVISION.
       MAIN.
           INVOKE G2INV02C "NEW" RETURNING O.
           MOVE "GREET" TO MNAME.
           INVOKE O MNAME.
           STOP RUN.
       END PROGRAM G2INV02.

       IDENTIFICATION DIVISION.
       CLASS-ID. G2INV02C INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. GREET.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "HI".
       END METHOD GREET.
       END OBJECT.
       END CLASS G2INV02C.
