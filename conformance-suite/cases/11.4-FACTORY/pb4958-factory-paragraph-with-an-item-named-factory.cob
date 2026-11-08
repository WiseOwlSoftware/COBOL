      *>--- conformance-case
      *> id: pb4958-factory-paragraph-with-an-item-named-factory
      *> rules: [FMT-11.4.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4958 - at COBOL-85, where 8.9 leaves FACTORY free, a data item named
      *> FACTORY is a user-defined word, and the class definition whose FACTORY paragraph
      *> (§2023:11.4) holds it, a COBOL-2002 construct, draws the same named edition refusal
      *> COBOL0900 as without the item. The reservation gate retypes the declared free word
      *> to a user-defined word; at the paragraph header that reading fails, so the gate
      *> gives the header back its keyword. Fails if the header draws COBOL5506
      *> "unexpected 'FACTORY'".
       IDENTIFICATION DIVISION.
       CLASS-ID. PB4958F.
       FACTORY.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FACTORY PIC X.
       END FACTORY.
       END CLASS PB4958F.
