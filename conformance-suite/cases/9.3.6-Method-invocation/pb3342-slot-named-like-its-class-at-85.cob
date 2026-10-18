      *>--- conformance-case
      *> id: pb3342-slot-named-like-its-class-at-85
      *> rules: [GR-9.3.6-1]
      *> source-format: fixed
      *>---
      *> kb/Work PB3342 - the edition gate under
      *> cases/9.3.6-Method-invocation/pb3342_slot_named_like_its_class:
      *> an override spelled like its class is legal from COBOL 2002,
      *> which introduced object orientation (the CLASS-ID paragraph,
      *> ISO 11.3), so below 2002 the class definition is refused.
       IDENTIFICATION DIVISION.
       CLASS-ID. K3342N INHERITS BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. M3342N.
       PROCEDURE DIVISION.
           DISPLAY "BASE-M"
           GOBACK.
       END METHOD M3342N.
       END OBJECT.
       END CLASS K3342N.
       IDENTIFICATION DIVISION.
       CLASS-ID. M3342N INHERITS K3342N.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS K3342N.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. M3342N OVERRIDE.
       PROCEDURE DIVISION.
           DISPLAY "SUB-M"
           GOBACK.
       END METHOD M3342N.
       END OBJECT.
       END CLASS M3342N.
