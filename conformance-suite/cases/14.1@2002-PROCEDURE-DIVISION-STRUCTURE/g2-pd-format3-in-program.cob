      *>--- conformance-case
      *> id: g2-pd-format3-in-program
      *> rules: [2002-SR-14.1.2-12]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.1.2 SR12 - "Format 3 may be specified in a source
      *>   element if and only if that source element is a factory
      *>   definition, an instance definition, or an interface
      *>   definition, but not in a method definition." A program
      *>   definition is not one of the three: program G2PDF3P's
      *>   Format 3 division holding method SHOWIT is refused (the "only
      *>   if" arm). Formats 1 and 2 (§2002:14.1.1) hold no
      *>   method-definition, so no other reading is legal.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2PDF3P.
       PROCEDURE DIVISION.
       METHOD-ID. SHOWIT.
       PROCEDURE DIVISION.
           DISPLAY "M".
       END METHOD SHOWIT.
       END PROGRAM G2PDF3P.
