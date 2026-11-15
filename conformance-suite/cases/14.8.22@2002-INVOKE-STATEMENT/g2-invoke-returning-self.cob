      *>--- conformance-case
      *> id: g2-invoke-returning-self
      *> rules: [2002-SR-14.8.22.2-21]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.22.2 SR21 - "Identifier-4 is a receiving operand",
      *> and §2002:8.4.2.8.2 SR2 says of SELF "This identifier format
      *> shall not be specified as a receiving operand", so RETURNING
      *> SELF at line 40 is refused at 2002 (COBOL3978). A legal
      *> RETURNING into a CD record item is g2-invoke-cd-receiving-
      *> operands A5. Why it can fail: the compiler accepts SELF as the
      *> RETURNING item or reports another code.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2INV04.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "COMPILED".
           STOP RUN.
       END PROGRAM G2INV04.

       IDENTIFICATION DIVISION.
       CLASS-ID. G2INV04C INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. MKOBJ.
       DATA DIVISION.
       LINKAGE SECTION.
       01 RO USAGE OBJECT REFERENCE.
       PROCEDURE DIVISION RETURNING RO.
           SET RO TO SELF.
       END METHOD MKOBJ.
       METHOD-ID. USEIT.
       PROCEDURE DIVISION.
           INVOKE SELF "MKOBJ" RETURNING SELF.
       END METHOD USEIT.
       END OBJECT.
       END CLASS G2INV04C.
