      *>--- conformance-case
      *> id: g2mid01-override-same-name-superclass
      *> rules: [2002-SR-11.6.2-3]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:11.6.2 SR3 - "If the OVERRIDE phrase is specified, there
      *> shall be a method with the same name as the method declared by
      *> this method definition defined in a superclass. The method in
      *> the superclass shall not be defined with the FINAL clause."
      *> 2002 asks the NAME only (2023 asks the method resolution
      *> signature). Two legal OVERRIDEs, each naming a non-FINAL method
      *> of the direct superclass:
      *>  - G2MIDY's WHO OVERRIDE overrides G2MIDX's WHO, same interface;
      *>  - G2MIDA's GETR OVERRIDE overrides G2MIDB's GETR whose RETURNING
      *>    item is an OBJECT REFERENCE G2MIDX, with a RETURNING item of
      *>    the SUBCLASS G2MIDY: same name, a different but conforming
      *>    interface (§2002:9.3.7.1.2 rule 5c2, "the same class-name or
      *>    a subclass of that class-name"), so SR9 holds and SR3 holds.
      *> §2002:11.6.3 GR2 - "The OVERRIDE phrase indicates that this
      *> method overrides the inherited method." so a GETR invoked through
      *> a G2MIDB reference holding a G2MIDA runs G2MIDA's GETR (a G2MIDY,
      *> whose WHO says "I AM Y"); on a G2MIDB it runs G2MIDB's (a G2MIDX,
      *> "I AM X"). Fails if either OVERRIDE is refused (a signature-keyed
      *> SR3 refuses the covariant one) or if dispatch reaches the
      *> superclass body (line 1 "I AM X").
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2MID02.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS G2MIDB
           CLASS G2MIDA
           CLASS G2MIDX.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  OB USAGE OBJECT REFERENCE G2MIDB.
       01  RX USAGE OBJECT REFERENCE G2MIDX.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE G2MIDA "NEW" RETURNING OB.
           INVOKE OB "GETR" RETURNING RX.
           INVOKE RX "WHO".
           INVOKE G2MIDB "NEW" RETURNING OB.
           INVOKE OB "GETR" RETURNING RX.
           INVOKE RX "WHO".
           STOP RUN.
       END PROGRAM G2MID02.

       IDENTIFICATION DIVISION.
       CLASS-ID. G2MIDX INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. WHO.
       PROCEDURE DIVISION.
           DISPLAY "I AM X".
       END METHOD WHO.
       END OBJECT.
       END CLASS G2MIDX.

       IDENTIFICATION DIVISION.
       CLASS-ID. G2MIDY INHERITS FROM G2MIDX.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS G2MIDX.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. WHO OVERRIDE.
       PROCEDURE DIVISION.
           DISPLAY "I AM Y".
       END METHOD WHO.
       END OBJECT.
       END CLASS G2MIDY.

       IDENTIFICATION DIVISION.
       CLASS-ID. G2MIDB INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           CLASS G2MIDX.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. GETR.
       DATA DIVISION.
       LINKAGE SECTION.
       01  R USAGE OBJECT REFERENCE G2MIDX.
       PROCEDURE DIVISION RETURNING R.
           INVOKE G2MIDX "NEW" RETURNING R.
       END METHOD GETR.
       END OBJECT.
       END CLASS G2MIDB.

       IDENTIFICATION DIVISION.
       CLASS-ID. G2MIDA INHERITS FROM G2MIDB.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS G2MIDB
           CLASS G2MIDY.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. GETR OVERRIDE.
       DATA DIVISION.
       LINKAGE SECTION.
       01  R USAGE OBJECT REFERENCE G2MIDY.
       PROCEDURE DIVISION RETURNING R.
           INVOKE G2MIDY "NEW" RETURNING R.
       END METHOD GETR.
       END OBJECT.
       END CLASS G2MIDA.
