      *>--- conformance-case
      *> id: g2mid04-same-name-no-override
      *> rules: [2002-SR-11.6.2-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:11.6.2 SR4a - "if this method definition is contained in a
      *> class definition, no inherited method shall have the same name
      *> as the method declared by this method definition". G2MIDD
      *> inherits M from G2MIDC and redeclares M with the SAME (empty)
      *> interface and no OVERRIDE: SR9 holds (identical interfaces
      *> conform), so SR4a is the only rule violated. The refusal is
      *> expected on G2MIDD's METHOD-ID line.
       IDENTIFICATION DIVISION.
       CLASS-ID. G2MIDC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. M.
       PROCEDURE DIVISION.
           DISPLAY "C-M".
       END METHOD M.
       END OBJECT.
       END CLASS G2MIDC.

       IDENTIFICATION DIVISION.
       CLASS-ID. G2MIDD INHERITS FROM G2MIDC.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS G2MIDC.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. M.
       PROCEDURE DIVISION.
           DISPLAY "D-M".
       END METHOD M.
       END OBJECT.
       END CLASS G2MIDD.
