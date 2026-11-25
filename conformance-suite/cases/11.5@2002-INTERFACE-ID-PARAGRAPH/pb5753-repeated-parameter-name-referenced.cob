*>--- conformance-case
*> id: pb5753-repeated-parameter-name-referenced
*> rules: [2002-GR-11.5.3-3]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB5753 -- a REFERENCE to a parameter-name the USING clause writes twice, at COBOL-2002.
      *> §2002:11.5.3 GR3: "Parameter-name-1 is the name given to the formal parameter" (cite.py --check: OK), so
      *> `INTERFACE-ID. PB5753R USING P P` names two formal parameters P. `USAGE OBJECT REFERENCE P` refers to a
      *> formal, and identifies neither one: "In order to use a resource, a statement shall contain a reference that
      *> uniquely identifies that resource" (§2002:8.4.1; cite.py --check: OK), and a parameter-name cannot be
      *> qualified. An expansion would replace it by "the corresponding actual parameter" (§2002:12.2.7.3 GR8),
      *> and there would be two -> COBOL2239 at the reference. From COBOL-2014 the USING clause itself is refused
      *> (§2014:11.6.2 SR7, §2023:11.6.3 SR7), also COBOL2239.
       IDENTIFICATION DIVISION.
       INTERFACE-ID. PB5753R USING P P.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS P.
       PROCEDURE DIVISION.
       METHOD-ID. PUT.
       DATA DIVISION.
       LINKAGE SECTION.
       01 X USAGE OBJECT REFERENCE P.
       PROCEDURE DIVISION USING X.
       END METHOD PUT.
       END INTERFACE PB5753R.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5753RM.
       PROCEDURE DIVISION.
           STOP RUN.
       END PROGRAM PB5753RM.
