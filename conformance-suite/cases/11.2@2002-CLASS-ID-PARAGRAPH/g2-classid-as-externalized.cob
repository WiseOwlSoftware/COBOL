      *>--- conformance-case
      *> id: g2-classid-as-externalized
      *> rules: [2002-GR-11.2.3-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:11.2.3 GR1 - "Class-name-1 names the class declared by this
      *> class definition. However, literal-1, if specified, is the name of
      *> the class that is externalized to the operating environment."
      *> The class is declared as G2CLN AS "G2CLNEXT"; the client program
      *> names it only through its externalized name, CLASS K AS "G2CLNEXT"
      *> (§2002:12.2.7.3 GR2: literal-1 "is the externalized name by which
      *> the class ... is known to the operating environment"). Fails if
      *> the literal is not the externalized name (K would name no class:
      *> the run directory's control with AS "G2CLN" is refused, COBOL5962)
      *> or if the method of class G2CLN is not reached: the one output
      *> line is HI FROM G2CLN.
       IDENTIFICATION DIVISION.
       CLASS-ID. G2CLN AS "G2CLNEXT" INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       IDENTIFICATION DIVISION.
       METHOD-ID. HI.
       PROCEDURE DIVISION.
           DISPLAY "HI FROM G2CLN".
       END METHOD HI.
       END OBJECT.
       END CLASS G2CLN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CLAS.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS K AS "G2CLNEXT".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE K.
       PROCEDURE DIVISION.
           INVOKE K "NEW" RETURNING O
           INVOKE O "HI"
           STOP RUN.
       END PROGRAM G2CLAS.
