      *>--- conformance-case
      *> id: pb1086-inherits-class-not-in-repository
      *> rules: [SR-11.3.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB1086. ISO 1989:2023 11.3.3 syntax rule 2: "Object-
      *> class-name-2 shall be the name of a class specified in the
      *> REPOSITORY paragraph of this source element" (cite.py --check
      *> 11.3.3 -> OK 2)). The class PB1086BA is defined by this very
      *> compilation group, but the REPOSITORY paragraph of PB1086BD
      *> does not specify it, so the INHERITS clause names a class the
      *> rule does not allow: refused with COBOL0821
      *> (wiseowl/diagnostics/pb1086-inherits-class-not-in-repository.json).
      *> The positive twin is 9.3.6-Method-invocation/oo_inherit, whose
      *> subclass specifies its superclass in its REPOSITORY paragraph.
       IDENTIFICATION DIVISION.
       CLASS-ID. PB1086BA INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       END CLASS PB1086BA.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1086BD INHERITS FROM PB1086BA.
       END CLASS PB1086BD.
