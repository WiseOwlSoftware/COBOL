*>--- conformance-case
*> id: pb5753_repeated_parameter_name_2002
*> rules: [2002-GR-11.2.3-5, 2002-GR-11.5.3-3]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB5753 -- a parameter-name written twice in a USING clause, at COBOL-2002.
      *> §2002:11.2.2 (CLASS-ID) has nine syntax rules and §2002:11.5.2 (INTERFACE-ID) five; none forbids a
      *> repeated parameter-name. §2002:11.2.3 GR5: "Parameter-name-1 is the name given to the formal parameter"
      *> (§2002:11.5.3 GR3 the interface twin; cite.py --check: OK), so `USING P P` names two formal parameters P,
      *> and SR9 / SR4 are met by the one class-specifier `CLASS P`. The rule that refuses it, "A given
      *> parameter-name shall not appear more than once in a USING clause", arrives in COBOL-2014
      *> (§2014:11.3.2 SR9, §2014:11.6.2 SR7; §2023:11.3.3 SR9, §2023:11.6.3 SR7), so this program is refused
      *> at 2014 and 2023 (the diagnostics file of this id). Neither definition references P anywhere else:
      *> such a reference would identify neither formal (§2002:8.4.1; pb5753-repeated-parameter-name-referenced).
      *> DERIVATION OF THE EXPECTED OUTPUT. §2002:12.2.7.3 GR5: PB5753GX is created from PB5753G "by replacing
      *> each specification of the formal parameter by the corresponding actual parameter", and §2002:9.3.12
      *> treats it "in all respects the same as if it were a class that is not a parameterized class": its
      *> factory's NEW makes an object whose SHOW displays "PB5753G SHOW", and its factory counter counts 1, 2.
      *> The parameterized interface PB5753I is expanded by nothing, and is accepted as itself.
       IDENTIFICATION DIVISION.
       CLASS-ID. PB5753A INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       END CLASS PB5753A.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB5753B INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       END CLASS PB5753B.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. PB5753I USING Q Q.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           INTERFACE Q.
       PROCEDURE DIVISION.
       METHOD-ID. PING.
       PROCEDURE DIVISION.
       END METHOD PING.
       END INTERFACE PB5753I.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB5753G INHERITS FROM BASE USING P P.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           CLASS P.
       FACTORY.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 CNT PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       METHOD-ID. BUMP.
       PROCEDURE DIVISION.
           ADD 1 TO CNT.
           DISPLAY "COUNT " CNT.
       END METHOD BUMP.
       END FACTORY.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. SHOW.
       PROCEDURE DIVISION.
           DISPLAY "PB5753G SHOW".
       END METHOD SHOW.
       END OBJECT.
       END CLASS PB5753G.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5753M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB5753A
           CLASS PB5753B
           CLASS PB5753G
           CLASS PB5753GX EXPANDS PB5753G USING PB5753A PB5753B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE PB5753GX.
       PROCEDURE DIVISION.
           INVOKE PB5753GX "NEW" RETURNING O.
           INVOKE O "SHOW".
           INVOKE PB5753GX "BUMP".
           INVOKE PB5753GX "BUMP".
           STOP RUN.
       END PROGRAM PB5753M.
