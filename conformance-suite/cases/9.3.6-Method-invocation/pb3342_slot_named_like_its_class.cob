      *>--- conformance-case
      *> id: pb3342_slot_named_like_its_class
      *> rules: [GR-9.3.6-1, SR-11.7.3-3]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *>---
      *> kb/Work PB3342 - a method spelled like a class (or interface)
      *> that declares it, an override of it or an implementation of
      *> it is legal COBOL, and each invocation binds the method of the
      *> class of the object (9.3.6).
      *> cite.py --check 8.3.2.2 "Within a source element" -> OK: one
      *>   word as two types of user-defined word is barred only within
      *>   one source element;
      *> cite.py --check 10.5 "A source element is a source unit
      *>   excluding any contained source units" -> OK: a METHOD-ID's
      *>   method-name and the CLASS-ID's object-class-name sit in
      *>   different source elements;
      *> cite.py --check 8.4.6.1 "independent of the use of these
      *>   user-defined words by other source elements" -> OK;
      *> cite.py --check 11.7.3 "If the OVERRIDE phrase is specified,
      *>   there shall be a method with the same method resolution
      *>   signature as the method declared by this method definition
      *>   defined in a superclass" -> OK 11.7.3 3);
      *> cite.py --check 9.3.6 "it is the class of the actual object
      *>   referenced at runtime that is used in resolving a method
      *>   invocation to a particular method implementation" -> OK.
      *> DERIVATION, one line per INVOKE, in order:
      *>   K3342B object, method M3342 -> its own: BASE-M
      *>   M3342 object (a subclass named M3342 overriding M3342),
      *>     typed M3342 -> the override: SUB-M
      *>   the same object through a K3342B reference -> SUB-M
      *>   the same object through a universal reference -> SUB-M
      *>   N3342 object through interface I3342N (N3342 implements
      *>     I3342N's prototype N3342, spelled like the class) -> IMPL-N
      *>   R3342 object through interface Q3342 (Q3342's prototype is
      *>     spelled like the interface) -> R-Q
      *>   Q3342S object (named unlike Q3342, overriding it) through
      *>     Q3342 -> S-Q
      *>   the factory method Q3342S of class Q3342S -> FACTORY-S
      *>   BASE's FactoryObject overridden in a class named
      *>     FACTORYOBJECT, returning NULL -> FO-OVERRIDE, then FO-NULL
      *> Every leg fails at compile time when the slot keeps a name a
      *> declaring type spells (CS0542 / CS0535) or is refused.
       IDENTIFICATION DIVISION.
       CLASS-ID. K3342B INHERITS BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. M3342.
       PROCEDURE DIVISION.
           DISPLAY "BASE-M"
           GOBACK.
       END METHOD M3342.
       END OBJECT.
       END CLASS K3342B.
       IDENTIFICATION DIVISION.
       CLASS-ID. M3342 INHERITS K3342B.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS K3342B.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. M3342 OVERRIDE.
       PROCEDURE DIVISION.
           DISPLAY "SUB-M"
           GOBACK.
       END METHOD M3342.
       END OBJECT.
       END CLASS M3342.
       IDENTIFICATION DIVISION.
       INTERFACE-ID. I3342N.
       PROCEDURE DIVISION.
       METHOD-ID. N3342.
       PROCEDURE DIVISION.
       END METHOD N3342.
       END INTERFACE I3342N.
       IDENTIFICATION DIVISION.
       CLASS-ID. N3342 INHERITS BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           INTERFACE I3342N.
       IDENTIFICATION DIVISION.
       OBJECT. IMPLEMENTS I3342N.
       PROCEDURE DIVISION.
       METHOD-ID. N3342.
       PROCEDURE DIVISION.
           DISPLAY "IMPL-N"
           GOBACK.
       END METHOD N3342.
       END OBJECT.
       END CLASS N3342.
       IDENTIFICATION DIVISION.
       INTERFACE-ID. Q3342.
       PROCEDURE DIVISION.
       METHOD-ID. Q3342.
       PROCEDURE DIVISION.
       END METHOD Q3342.
       END INTERFACE Q3342.
       IDENTIFICATION DIVISION.
       CLASS-ID. R3342 INHERITS BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           INTERFACE Q3342.
       IDENTIFICATION DIVISION.
       OBJECT. IMPLEMENTS Q3342.
       PROCEDURE DIVISION.
       METHOD-ID. Q3342.
       PROCEDURE DIVISION.
           DISPLAY "R-Q"
           GOBACK.
       END METHOD Q3342.
       END OBJECT.
       END CLASS R3342.
       IDENTIFICATION DIVISION.
       CLASS-ID. Q3342S INHERITS R3342.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS R3342.
       IDENTIFICATION DIVISION.
       FACTORY.
       PROCEDURE DIVISION.
       METHOD-ID. Q3342S.
       PROCEDURE DIVISION.
           DISPLAY "FACTORY-S"
           GOBACK.
       END METHOD Q3342S.
       END FACTORY.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. Q3342 OVERRIDE.
       PROCEDURE DIVISION.
           DISPLAY "S-Q"
           GOBACK.
       END METHOD Q3342.
       END OBJECT.
       END CLASS Q3342S.
       IDENTIFICATION DIVISION.
       CLASS-ID. FACTORYOBJECT INHERITS BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. FACTORYOBJECT OVERRIDE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 OUTF USAGE OBJECT REFERENCE FACTORY OF ACTIVE-CLASS.
       PROCEDURE DIVISION RETURNING OUTF.
           DISPLAY "FO-OVERRIDE"
           SET OUTF TO NULL
           GOBACK.
       END METHOD FACTORYOBJECT.
       END OBJECT.
       END CLASS FACTORYOBJECT.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P3342G.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS K3342B
           CLASS M3342
           CLASS N3342
           CLASS R3342
           CLASS Q3342S
           CLASS FACTORYOBJECT
           INTERFACE I3342N
           INTERFACE Q3342.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 OB USAGE OBJECT REFERENCE K3342B.
       01 OM USAGE OBJECT REFERENCE M3342.
       01 OU USAGE OBJECT REFERENCE.
       01 ON3 USAGE OBJECT REFERENCE N3342.
       01 OI USAGE OBJECT REFERENCE I3342N.
       01 OR3 USAGE OBJECT REFERENCE R3342.
       01 OQ USAGE OBJECT REFERENCE Q3342S.
       01 OIQ USAGE OBJECT REFERENCE Q3342.
       01 OF3 USAGE OBJECT REFERENCE FACTORYOBJECT.
       01 FF USAGE OBJECT REFERENCE.
       PROCEDURE DIVISION.
           INVOKE K3342B "NEW" RETURNING OB
           INVOKE OB "M3342"
           INVOKE M3342 "NEW" RETURNING OM
           INVOKE OM "M3342"
           SET OB TO OM
           INVOKE OB "M3342"
           SET OU TO OM
           INVOKE OU "M3342"
           INVOKE N3342 "NEW" RETURNING ON3
           SET OI TO ON3
           INVOKE OI "N3342"
           INVOKE R3342 "NEW" RETURNING OR3
           SET OIQ TO OR3
           INVOKE OIQ "Q3342"
           INVOKE Q3342S "NEW" RETURNING OQ
           SET OIQ TO OQ
           INVOKE OIQ "Q3342"
           INVOKE Q3342S "Q3342S"
           INVOKE FACTORYOBJECT "NEW" RETURNING OF3
           INVOKE OF3 "FACTORYOBJECT" RETURNING FF
           IF FF = NULL
               DISPLAY "FO-NULL"
           END-IF
           STOP RUN.
       END PROGRAM P3342G.
