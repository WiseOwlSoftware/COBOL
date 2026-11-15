      *>--- conformance-case
      *> id: g2cfg05_instance_object_computer
      *> rules: [2002-SR-12.2.2-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:12.2.2 SR3 - "The SOURCE-COMPUTER, OBJECT-COMPUTER, and
      *> REPOSITORY paragraphs shall not be specified in a factory
      *> definition or an instance definition."
      *>   cite.py --check "§2002:12.2.2 SR3" "The SOURCE-COMPUTER,
      *>   OBJECT-COMPUTER, and REPOSITORY paragraphs shall not be
      *>   specified in a factory definition or an instance
      *>   definition" -> OK
      *> The arm exercised: the OBJECT-COMPUTER paragraph
      *> in the instance (OBJECT) definition's own
      *> configuration section (line 27). The class definition
      *> itself is legal 2002 source (the same class with the paragraph
      *> moved to the class definition's own configuration section
      *> compiles clean at --std 2002), so the one diagnosis is SR3.
      *> The six pairs of paragraph and definition are g2cfg01..06.
       IDENTIFICATION DIVISION.
       CLASS-ID. G2CFG05.
       IDENTIFICATION DIVISION.
       OBJECT.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. X.
       END OBJECT.
       END CLASS G2CFG05.
