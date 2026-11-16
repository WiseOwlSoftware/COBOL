      *>--- conformance-case
      *> id: g2mid01_as_alphanumeric_national
      *> rules: [2002-SR-11.6.2-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:11.6.2 SR1 - 'Literal-1 shall be an alphanumeric literal
      *> or a national literal': METHOD-ID ALPHM AS an alphanumeric
      *> literal and NATM AS a national literal are both accepted; the
      *> class is instantiated and the program runs to its DISPLAY. Leg:
      *> a screen that rejects a national literal (or any AS literal)
      *> refuses the program. The methods are not INVOKEd, so the
      *> expectation does not depend on which name INVOKE resolves.
      *> Expected: METHOD-ID AS ACCEPTED.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2MID02.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS G2MIDC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE G2MIDC.
       PROCEDURE DIVISION.
       MAIN-P.
           INVOKE G2MIDC "NEW" RETURNING O.
           DISPLAY "METHOD-ID AS ACCEPTED".
           STOP RUN.
       END PROGRAM G2MID02.

       IDENTIFICATION DIVISION.
       CLASS-ID. G2MIDC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       IDENTIFICATION DIVISION.
       METHOD-ID. ALPHM AS "G2ALPHEXT".
       PROCEDURE DIVISION.
           DISPLAY "ALPHANUMERIC AS".
       END METHOD ALPHM.
       IDENTIFICATION DIVISION.
       METHOD-ID. NATM AS N"G2NATEXT".
       PROCEDURE DIVISION.
           DISPLAY "NATIONAL AS".
       END METHOD NATM.
       END OBJECT.
       END CLASS G2MIDC.
