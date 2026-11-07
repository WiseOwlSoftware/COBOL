*>--- conformance-case
*> id: pb232-entry-convention-name
*> rules: [GR-11.9.7.4-3, GR-14.9.4.4-10, DOC-A.1-64]
*> source-format: free
*> portable: unverified
*>---
      *> ISO/IEC 1989:2023 §2023:11.9.7.4 GR3: "When entry-convention-name-1 is specified, the meaning of the entry
      *> convention is implementor-defined." WiseOwl COBOL defines no entry-convention-name (docs/CONFORMANCE.md
      *> §7, Annex A.1 item 64); the one convention provided is COBOL. A named convention is refused by name
      *> (COBOL2385) instead of being silently activated with the COBOL convention (kb/Work PB232) - which
      *> is what happened before: the clause was parsed and read by nothing but the edition gate. The clause with
      *> an entry-convention-name is in ISO/IEC 1989:2002 too (11.8.5: ENTRY-CONVENTION IS entry-convention-name-1,
      *> GR2: its meaning is implementor-defined); only the COBOL alternative is a 2014 addition, so 2002 draws
      *> COBOL2385 alone, with no edition diagnostic beside it (kb/Work PB4611).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W56ECNEG.
       OPTIONS.
           ENTRY-CONVENTION IS STDCALL.
       PROCEDURE DIVISION.
           DISPLAY "UNREACHED"
           STOP RUN.
