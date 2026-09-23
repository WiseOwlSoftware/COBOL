      *> reject-at: 2002 2014 2023
      *> kb/Work PB1010 - ISO 14.2.2 SR10 admits the declaratives format
      *> "if and only if" the source element is a function, method or
      *> program definition (or a prototype); SR12/SR13 make an instance
      *> definition's own procedure division the object-oriented format,
      *> which holds method definitions only - so DECLARATIVES written in
      *> the OBJECT paragraph outside any method are refused.
       IDENTIFICATION DIVISION.
       CLASS-ID. PB1010NO.
       OBJECT.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D1 SECTION.
           USE AFTER EXCEPTION CONDITION EC-ALL.
       END DECLARATIVES.
       END OBJECT.
       END CLASS PB1010NO.
