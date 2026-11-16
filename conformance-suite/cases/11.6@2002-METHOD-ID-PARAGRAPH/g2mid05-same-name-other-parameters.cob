      *>--- conformance-case
      *> id: g2mid05-same-name-other-parameters
      *> rules: [2002-SR-11.6.2-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:11.6.2 SR4a - "no inherited method shall have the same name
      *> as the method declared by this method definition". 2002 keys on
      *> the NAME: G2MIDP's M takes one USING parameter where the
      *> inherited G2MIDO M takes none. Under 2023 (method resolution
      *> signature) that is a distinct method; under 2002 it is a
      *> same-name method without OVERRIDE and is refused. SR9 is broken
      *> as well (the interfaces do not conform, §2002:9.3.7.1.2 rule 1);
      *> the refusal witnesses SR4a because its diagnostic is the SR4
      *> one. Expected on G2MIDP's METHOD-ID line.
       IDENTIFICATION DIVISION.
       CLASS-ID. G2MIDO INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. M.
       PROCEDURE DIVISION.
           DISPLAY "O-M".
       END METHOD M.
       END OBJECT.
       END CLASS G2MIDO.

       IDENTIFICATION DIVISION.
       CLASS-ID. G2MIDP INHERITS FROM G2MIDO.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS G2MIDO.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. M.
       DATA DIVISION.
       LINKAGE SECTION.
       01  P PIC X.
       PROCEDURE DIVISION USING P.
           DISPLAY "P-M " P.
       END METHOD M.
       END OBJECT.
       END CLASS G2MIDP.
