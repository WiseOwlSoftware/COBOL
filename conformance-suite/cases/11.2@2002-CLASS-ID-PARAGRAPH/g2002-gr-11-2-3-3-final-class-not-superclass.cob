      *>--- conformance-case
      *> id: g2002-gr-11-2-3-3-final-class-not-superclass
      *> rules: [2002-GR-11.2.3-3]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:11.2.3 GR3 - "If the FINAL clause is specified, the class
      *> shall not be the superclass for any other class." The rule
      *> restricts only the FINAL class's use AS A SUPERCLASS: a FINAL
      *> class may itself inherit, be instantiated and have its methods
      *> invoked. ACCEPTED leg: G2CLFN IS FINAL INHERITS FROM G2CLBS
      *> (a non-FINAL class), and no class names G2CLFN in an INHERITS
      *> clause. Expected, in order:
      *>   "BASE-HELLO"  - INVOKE O "HELLO": HELLO is defined only in
      *>                   G2CLBS and inherited by the FINAL class
      *>                   (§2002:11.2.3 GR2, inheritance per 9.3.9);
      *>   "FINAL-OWN"   - INVOKE O "OWN": the FINAL class's own method.
      *> Fails if FINAL is applied to the class's own inheritance (the
      *> FINAL class refused as a SUBclass) or a FINAL class cannot be
      *> instantiated. The refusal twin
      *> g2002-gr-11-2-3-3-final-class-as-superclass names a FINAL class
      *> in an INHERITS clause and must be refused.
       IDENTIFICATION DIVISION.
       CLASS-ID. G2CLBS INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       IDENTIFICATION DIVISION.
       METHOD-ID. HELLO.
       PROCEDURE DIVISION.
           DISPLAY "BASE-HELLO".
       END METHOD HELLO.
       END OBJECT.
       END CLASS G2CLBS.
       IDENTIFICATION DIVISION.
       CLASS-ID. G2CLFN IS FINAL INHERITS FROM G2CLBS.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS G2CLBS.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       IDENTIFICATION DIVISION.
       METHOD-ID. OWN.
       PROCEDURE DIVISION.
           DISPLAY "FINAL-OWN".
       END METHOD OWN.
       END OBJECT.
       END CLASS G2CLFN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CL01.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS G2CLFN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE G2CLFN.
       PROCEDURE DIVISION.
           INVOKE G2CLFN "NEW" RETURNING O.
           INVOKE O "HELLO".
           INVOKE O "OWN".
           STOP RUN.
       END PROGRAM G2CL01.
