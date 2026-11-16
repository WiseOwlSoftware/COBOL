      *>--- conformance-case
      *> id: g2mid02-override-no-superclass-method
      *> rules: [2002-SR-11.6.2-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:11.6.2 SR3 - "If the OVERRIDE phrase is specified, there
      *> shall be a method with the same name as the method declared by
      *> this method definition defined in a superclass." G2MIDN's only
      *> superclass is BASE, which defines no method named ZZQ, so ZZQ
      *> OVERRIDE violates SR3 and nothing else (no PROPERTY, no FINAL,
      *> not a prototype; SR9 needs an inherited name). The refusal is
      *> expected on the METHOD-ID line.
       IDENTIFICATION DIVISION.
       CLASS-ID. G2MIDN INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. ZZQ OVERRIDE.
       PROCEDURE DIVISION.
           DISPLAY "ZZQ".
       END METHOD ZZQ.
       END OBJECT.
       END CLASS G2MIDN.
