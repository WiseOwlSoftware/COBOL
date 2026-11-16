      *>--- conformance-case
      *> id: g2ios03-io-section-in-class-outer
      *> rules: [2002-SR-12.3.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:12.3.2 SR1 - an input-output section at the outer level of a class definition is refused.
      *>   cite.py --check "§2002:12.3.2 SR1" "Within a class definition, the input-output section may be specified only in a factory definition or instance definition" -> OK
      *> The class definition's own environment division (outside its
      *> factory and instance definitions) carries an input-output
      *> section; the rule admits it only in a factory or instance
      *> definition, so the only defect is the placement (the
      *> FILE-CONTROL paragraph holds no entry, as §2002:12.3.3.1
      *> permits, so no file description is owed). Expected: a
      *> diagnostic at the class's INPUT-OUTPUT SECTION.
       IDENTIFICATION DIVISION.
       CLASS-ID. G2IOSC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. RUNO.
       PROCEDURE DIVISION.
       MAIN-M.
           CONTINUE.
       END METHOD RUNO.
       END OBJECT.
       END CLASS G2IOSC.
