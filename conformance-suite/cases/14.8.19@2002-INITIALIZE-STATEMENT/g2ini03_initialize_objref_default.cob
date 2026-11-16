      *>--- conformance-case
      *> id: g2ini03_initialize_objref_default
      *> rules: [2002-GR-14.8.19.3-6]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.19.3 GR6c, the Object-reference row of the table:
      *> "Object-reference | Predefined object reference NULL". A bare
      *> INITIALIZE O1 makes O1 a receiving-operand (GR5c4), no VALUE
      *> or REPLACING phrase applies (GR6a, GR6b), so the sender is
      *> the predefined object reference NULL and the implicit
      *> statement is SET O1 TO NULL (GR4).
      *> EXPECTED, derived before the run:
      *>   O1 BEFORE SET    O1 holds the object INVOKE NEW returned.
      *>   O1 AFTER NULL    GR6c Object-reference row.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2INZ03.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS G2INZ3C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O1 USAGE OBJECT REFERENCE G2INZ3C.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE G2INZ3C "NEW" RETURNING O1
           IF O1 = NULL DISPLAY "O1 BEFORE NULL"
                   ELSE DISPLAY "O1 BEFORE SET" END-IF
           INITIALIZE O1
           IF O1 = NULL DISPLAY "O1 AFTER NULL"
                   ELSE DISPLAY "O1 AFTER SET" END-IF
           STOP RUN.
       END PROGRAM G2INZ03.

       IDENTIFICATION DIVISION.
       CLASS-ID. G2INZ3C INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. PING.
       PROCEDURE DIVISION.
       M-MAIN.
           DISPLAY "PINGED".
       END METHOD PING.
       END OBJECT.
       END CLASS G2INZ3C.
