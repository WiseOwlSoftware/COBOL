      *>--- conformance-case
      *> id: g2imi04-inline-invocation-returning-any-length
      *> rules: [2002-SR-8.4.2.4.2-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:8.4.2.4.2 SR4 - "The data item referenced in the RETURNING
      *> phrase of the invoked method's procedure division header shall
      *> not be described with the ANY LENGTH clause." GETV returns R,
      *> described PIC X ANY LENGTH. The method itself is legal:
      *> §2002:13.16.2.2 SR1 admits ANY LENGTH in an elementary level 1
      *> linkage entry of a method that is not a property method, and SR2
      *> lets that entry be the returning item; INVOKE OBJ "GETV"
      *> RETURNING W is accepted (it is the companion run noted in the
      *> adjudication). So the only ground for refusing OBJ :: "GETV" is
      *> SR4, and the refusal is expected on the MOVE line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2IMI04.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS G2IMI04C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  OBJ USAGE OBJECT REFERENCE G2IMI04C.
       01  W   PIC X(5).
       PROCEDURE DIVISION.
       MAIN-PARA.
           INVOKE G2IMI04C "NEW" RETURNING OBJ.
           MOVE OBJ :: "GETV" TO W.
           DISPLAY "W=" W.
           STOP RUN.
       END PROGRAM G2IMI04.

       IDENTIFICATION DIVISION.
       CLASS-ID. G2IMI04C INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. GETV.
       DATA DIVISION.
       LINKAGE SECTION.
       01  R PIC X ANY LENGTH.
       PROCEDURE DIVISION RETURNING R.
           MOVE "AB" TO R.
       END METHOD GETV.
       END OBJECT.
       END CLASS G2IMI04C.
