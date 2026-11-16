      *>--- conformance-case
      *> id: g2ios04-io-section-in-interface
      *> rules: [2002-SR-12.3.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:12.3.2 SR1 - an input-output section in an interface definition is refused.
      *>   cite.py --check "§2002:12.3.2 SR1" "The input-output section shall not be specified within an interface definition" -> OK
      *> The interface definition's environment division carries an
      *> input-output section; without it the interface is legal, so
      *> the only defect is the placement (the FILE-CONTROL paragraph
      *> holds no entry, as §2002:12.3.3.1 permits, so no file
      *> description is owed). Expected: a diagnostic at the
      *> interface's INPUT-OUTPUT SECTION.
       IDENTIFICATION DIVISION.
       INTERFACE-ID. G2IOSI.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
       PROCEDURE DIVISION.
       METHOD-ID. M1.
       PROCEDURE DIVISION.
       END METHOD M1.
       END INTERFACE G2IOSI.
