      *>--- conformance-case
      *> id: g2-arg-object
      *> rules: [2002-AR-15.2-10]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:15.2 AR10 - "An object reference shall be specified; the
      *> predefined object reference SUPER shall not be specified":
      *> object references given to LENGTH and BYTE-LENGTH, the two
      *> functions §2002:15.5 Table 22 marks Obj1. The SUPER arm is the
      *> refusal g2-arg-super-refused beside this case.
      *> Expected lines; the size of an object reference is the
      *> implementor's, so the case pins what the text fixes:
      *>   OB1 OK  LENGTH (§2002:15.39.3 RV3) and BYTE-LENGTH
      *>           (§2002:15.12.3 RV1) of a USAGE OBJECT REFERENCE item
      *>           are its size, so positive.
      *>   OB2 OK  two items of the same description have one size.
      *>   OB3 OK  the predefined object reference SELF, in a method of
      *>           the class, is an object reference argument too.
      *> A refused argument fails the case.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2ARG05.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS G2ARG5K.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O1 USAGE OBJECT REFERENCE.
       01 O2 USAGE OBJECT REFERENCE.
       PROCEDURE DIVISION.
       P0.
           IF FUNCTION LENGTH(O1) > 0
              AND FUNCTION BYTE-LENGTH(O1) > 0
               DISPLAY "OB1 OK"
           ELSE
               DISPLAY "OB1 BAD"
           END-IF
           IF FUNCTION LENGTH(O1) = FUNCTION LENGTH(O2)
              AND FUNCTION BYTE-LENGTH(O1) = FUNCTION BYTE-LENGTH(O2)
               DISPLAY "OB2 OK"
           ELSE
               DISPLAY "OB2 BAD"
           END-IF
           INVOKE G2ARG5K "NEW" RETURNING O1
           INVOKE O1 "M1"
           STOP RUN.
       END PROGRAM G2ARG05.

       IDENTIFICATION DIVISION.
       CLASS-ID. G2ARG5K INHERITS BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       IDENTIFICATION DIVISION.
       METHOD-ID. M1.
       PROCEDURE DIVISION.
           IF FUNCTION LENGTH(SELF) > 0
              AND FUNCTION BYTE-LENGTH(SELF) > 0
               DISPLAY "OB3 OK"
           ELSE
               DISPLAY "OB3 BAD"
           END-IF.
       END METHOD M1.
       END OBJECT.
       END CLASS G2ARG5K.
