*>--- conformance-case
*> id: pb4611-entry-convention-cobol-below-2014
*> rules: [FMT-11.9.7.2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO/IEC 1989:2023 §11.9.7.2: ENTRY-CONVENTION IS { COBOL | entry-convention-name-1 }, and §11.9.7.4 GR2:
      *> "When COBOL is specified, the naming convention and mapping of method-names and program-names are as
      *> specified in 8.3.2.2". The COBOL ALTERNATIVE is a COBOL-2014 addition: the 2014 FCD format lists it
      *> (t2014.txt:13144-13146) and ISO/IEC 1989:2002 (11.8.5) has only ENTRY-CONVENTION IS entry-convention-name-1.
      *> So this program, the clause with the COBOL alternative, is refused below 2014 (COBOL0900) and accepted
      *> from 2014 (pb232_entry_convention_cobol). The CLAUSE itself is a 2002 clause: a named convention draws
      *> no edition diagnostic at 2002, only the implementor's COBOL2385 (pb232-entry-convention-name; kb/Work
      *> PB4611 - the whole clause used to be gated at 2014).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4611N1.
       OPTIONS.
           ENTRY-CONVENTION IS COBOL.
       PROCEDURE DIVISION.
           STOP RUN.
