      *>--- conformance-case
      *> id: pb4363_debug_qualified_procedure
      *> rules: []
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> X3.23-1985 USE FOR DEBUGGING ON procedure-name (VCR Table 7 row 7.17; kb/Work PB4363). The
      *> procedure-name is a paragraph-name that may be qualified (ISO/IEC 1989:2023 14.4.1: "It consists of
      *> a paragraph-name (that may be qualified) or a section-name"), here PARA-W OF SEC-A, and a
      *> qualified reference identifies the one paragraph of that section (8.4.2.2.2 Format 4, 8.4.2.2.3).
      *> ISO 2023 prints no debug format, so the facility's spelling follows CLAUDE.md rule 1 to
      *> GnuCOBOL (docs/CONFORMANCE.md D-DEBUG-USE).
      *> Expected output: SEC-A and SEC-B each hold a paragraph PARA-W and the main body performs both
      *> once, but only SEC-A's is the debug subject, so the debugging section runs once: HITS=1.
      *> Why it can fail: a binder that reads a qualified operand as a data item refuses the program
      *> (the pre-fix COBOL1571), one that drops the qualifier finds PARA-W ambiguous, and one that
      *> binds both paragraphs prints HITS=2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4363DBGQ.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. IBM-PC WITH DEBUGGING MODE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  HITS  PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       DBG SECTION.
           USE FOR DEBUGGING ON PARA-W OF SEC-A.
       DBG-BODY.
           ADD 1 TO HITS.
       END DECLARATIVES.
       SEC-A SECTION.
       A-START.
           PERFORM PARA-W OF SEC-A.
           PERFORM PARA-W OF SEC-B.
           DISPLAY "HITS=" HITS.
           STOP RUN.
       PARA-W.
           CONTINUE.
       SEC-B SECTION.
       PARA-W.
           CONTINUE.
