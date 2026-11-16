      *>--- conformance-case
      *> id: g2mid03-override-final-method
      *> rules: [2002-SR-11.6.2-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:11.6.2 SR3 - "The method in the superclass shall not be
      *> defined with the FINAL clause." G2MIDF's M is IS FINAL; its
      *> subclass G2MIDG declares M OVERRIDE with the same (empty)
      *> interface, so the first sentence of SR3 and SR9 hold and only
      *> the FINAL ban is violated. The refusal is expected on G2MIDG's
      *> METHOD-ID line.
       IDENTIFICATION DIVISION.
       CLASS-ID. G2MIDF INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. M IS FINAL.
       PROCEDURE DIVISION.
           DISPLAY "F-M".
       END METHOD M.
       END OBJECT.
       END CLASS G2MIDF.

       IDENTIFICATION DIVISION.
       CLASS-ID. G2MIDG INHERITS FROM G2MIDF.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS G2MIDF.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. M OVERRIDE.
       PROCEDURE DIVISION.
           DISPLAY "G-M".
       END METHOD M.
       END OBJECT.
       END CLASS G2MIDG.
