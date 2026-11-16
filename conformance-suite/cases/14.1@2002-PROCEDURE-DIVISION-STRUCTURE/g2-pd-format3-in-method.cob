      *>--- conformance-case
      *> id: g2-pd-format3-in-method
      *> rules: [2002-SR-14.1.2-12]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.1.2 SR12 - "Format 3 may be specified in a source
      *>   element if and only if that source element is a factory
      *>   definition, an instance definition, or an interface
      *>   definition, but not in a method definition." Factory method
      *>   OUTER writes a Format 3 division holding method INNER: the
      *>   "but not in a method definition" arm, refused although OUTER
      *>   sits inside a factory definition. Formats 1 and 2
      *>   (§2002:14.1.1) hold no method-definition, so no other reading
      *>   is legal.
       IDENTIFICATION DIVISION.
       CLASS-ID. G2PDF3R INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       FACTORY.
       PROCEDURE DIVISION.
       METHOD-ID. OUTER.
       PROCEDURE DIVISION.
       METHOD-ID. INNER.
       PROCEDURE DIVISION.
           DISPLAY "INNER".
       END METHOD INNER.
       END METHOD OUTER.
       END FACTORY.
       END CLASS G2PDF3R.
