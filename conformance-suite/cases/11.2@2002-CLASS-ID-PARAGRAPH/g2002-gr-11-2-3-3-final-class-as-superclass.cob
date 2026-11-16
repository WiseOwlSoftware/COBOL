      *>--- conformance-case
      *> id: g2002-gr-11-2-3-3-final-class-as-superclass
      *> rules: [2002-GR-11.2.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:11.2.3 GR3 - "If the FINAL clause is specified, the class
      *> shall not be the superclass for any other class." REFUSED leg
      *> (beside the accepted g2002-gr-11-2-3-3-final-class-not-
      *> superclass): G2CLSB names the FINAL class G2CLFX in its
      *> INHERITS clause, which makes G2CLFX a superclass. The syntax
      *> rule that diagnoses it is §2002:11.2.2 SR5 "Class-name-2 shall
      *> not be the name of a class defined with the FINAL clause."
      *> Fails if the subclass is accepted.
       IDENTIFICATION DIVISION.
       CLASS-ID. G2CLFX IS FINAL INHERITS FROM BASE.
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
           DISPLAY "FX-M1".
       END METHOD M1.
       END OBJECT.
       END CLASS G2CLFX.
       IDENTIFICATION DIVISION.
       CLASS-ID. G2CLSB INHERITS FROM G2CLFX.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS G2CLFX.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       IDENTIFICATION DIVISION.
       METHOD-ID. M2.
       PROCEDURE DIVISION.
           DISPLAY "SB-M2".
       END METHOD M2.
       END OBJECT.
       END CLASS G2CLSB.
