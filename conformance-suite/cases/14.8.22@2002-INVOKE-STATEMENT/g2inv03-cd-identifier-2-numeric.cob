      *>--- conformance-case
      *> id: g2inv03-cd-identifier-2-numeric
      *> rules: [2002-SR-14.8.22.2-8]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.22.2 SR8 - "Identifier-2 shall reference an
      *> alphanumeric or national data item". MNUM, a record item
      *> under a 2002 CD entry, is PIC 9(5) (numeric), so INVOKE U
      *> MNUM is refused at its line although U is universal (SR7
      *> holds). The accepted alphanumeric and national forms are
      *> g2inv01-cd-identifier-2-and-single-identifier-args.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2INV03.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS G2INV03C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O  USAGE OBJECT REFERENCE G2INV03C.
       01 U  USAGE OBJECT REFERENCE.
       COMMUNICATION SECTION.
       CD CDOUT FOR OUTPUT
           TEXT LENGTH IS OTL.
       01 OUTREC.
          05 MNUM  PIC 9(5).
       PROCEDURE DIVISION.
       MAIN.
           INVOKE G2INV03C "NEW" RETURNING O.
           SET U TO O.
           MOVE 1 TO MNUM.
           INVOKE U MNUM.
           STOP RUN.
       END PROGRAM G2INV03.

       IDENTIFICATION DIVISION.
       CLASS-ID. G2INV03C INHERITS FROM BASE.
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
       END CLASS G2INV03C.
