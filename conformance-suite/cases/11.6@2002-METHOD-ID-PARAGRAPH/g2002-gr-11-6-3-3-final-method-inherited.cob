      *>--- conformance-case
      *> id: g2002-gr-11-6-3-3-final-method-inherited
      *> rules: [2002-GR-11.6.3-3]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:11.6.3 GR3 - "The FINAL clause indicates that this method
      *> shall not be overridden in any subclasses." FINAL forbids the
      *> override only: the subclass still inherits the FINAL method,
      *> and a sibling method WITHOUT FINAL stays overridable
      *> (§2002:11.6.3 GR2 "The OVERRIDE phrase indicates that this
      *> method overrides the inherited method"). ACCEPTED leg: G2MFA
      *> defines M1 IS FINAL and M2; subclass G2MFB overrides M2 only.
      *> Through a G2MFB object, expected in order:
      *>   "A-M1"  - M1 is G2MFA's FINAL method, inherited, not
      *>             overridden;
      *>   "B-M2"  - M2 is overridden in G2MFB (no FINAL on G2MFA's M2).
      *> Fails if FINAL suppresses inheritance of M1, if the override of
      *> the non-FINAL M2 is refused or not bound, or if FINAL leaks to
      *> M2. The refusal twin g2002-gr-11-6-3-3-final-method-overridden
      *> overrides the FINAL M1 and must be refused.
       IDENTIFICATION DIVISION.
       CLASS-ID. G2MFA INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       IDENTIFICATION DIVISION.
       METHOD-ID. M1 IS FINAL.
       PROCEDURE DIVISION.
           DISPLAY "A-M1".
       END METHOD M1.
       IDENTIFICATION DIVISION.
       METHOD-ID. M2.
       PROCEDURE DIVISION.
           DISPLAY "A-M2".
       END METHOD M2.
       END OBJECT.
       END CLASS G2MFA.
       IDENTIFICATION DIVISION.
       CLASS-ID. G2MFB INHERITS FROM G2MFA.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS G2MFA.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       IDENTIFICATION DIVISION.
       METHOD-ID. M2 OVERRIDE.
       PROCEDURE DIVISION.
           DISPLAY "B-M2".
       END METHOD M2.
       END OBJECT.
       END CLASS G2MFB.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2MF01.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS G2MFB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE G2MFB.
       PROCEDURE DIVISION.
           INVOKE G2MFB "NEW" RETURNING O.
           INVOKE O "M1".
           INVOKE O "M2".
           STOP RUN.
       END PROGRAM G2MF01.
