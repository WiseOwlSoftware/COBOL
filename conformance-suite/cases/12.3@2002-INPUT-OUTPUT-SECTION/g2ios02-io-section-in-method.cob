      *>--- conformance-case
      *> id: g2ios02-io-section-in-method
      *> rules: [2002-SR-12.3.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:12.3.2 SR1 - an input-output section in a method definition is refused.
      *>   cite.py --check "§2002:12.3.2 SR1" "the input-output section may be specified only in a factory definition or instance definition, but not in a method" -> OK
      *> Method RUNO of the instance definition carries its own
      *> environment division with an input-output section (its
      *> FILE-CONTROL paragraph holds no entry, which §2002:12.3.3.1
      *> "FILE-CONTROL. [ file-control-entry ] ..." permits, so no file
      *> section, itself barred from a method, is needed); the same
      *> section one level up, in the OBJECT definition, is legal
      *> (g2ios01_io_section_permitted_placements), so the only defect
      *> is the placement. Expected: a diagnostic at the method's
      *> INPUT-OUTPUT SECTION.
       IDENTIFICATION DIVISION.
       CLASS-ID. G2IOSM INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. RUNO.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
       PROCEDURE DIVISION.
       MAIN-M.
           CONTINUE.
       END METHOD RUNO.
       END OBJECT.
       END CLASS G2IOSM.
