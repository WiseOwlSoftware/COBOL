      *>--- conformance-case
      *> id: pb1355_library_text_literal
      *> rules: [GR-7.2.3.4-1, FMT-7.2.3.2, DOC-A.1-40]
      *> constructs: [copy-literal-name-2002]
      *> accept: [2002, 2014, 2023]
      *> files: [pb1355c.txt]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB1355 / PB1601 - literal-1 of the COPY statement names
      *> the library text by its VALUE (the DOC-A.1-40 determination in
      *> docs/CONFORMANCE.md, GnuCOBOL's order). RULES (each run through
      *> scripts/spec/cite.py --check):
      *>   7.2.3.4 1) "Text-name-1 or literal-1 identifies the library
      *>     text to be processed by the COPY statement"     -> OK 1)
      *>   7.2.3.2 the general format: COPY { text-name-1 | literal-1 }
      *>     [ { OF | IN } { library-name-1 | literal-2 } ] ...
      *> The literal forms are a COBOL-2002 introduction (constructs row
      *> copy-literal-name-2002, VCR row 7.42, derived): ANSI X3.23-1985's
      *> COPY names its text by a user-defined word only, so the 85 twin
      *> pb1601-copy-literal-name-below-2002 draws COBOLNET0900.
      *> The runner names this directory as a --copy search path.
      *> EXPECTED OUTPUT, DERIVED (why the leg can fail):
      *>   [TXT] literal-1 "pb1355c.txt" contains a period, so it is
      *>         tried only as spelled; a name tried with the suffix
      *>         list would find no pb1355c.txt.cpy and the COPY would
      *>         draw the library-not-found error.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1355LIT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       COPY "pb1355c.txt".
       PROCEDURE DIVISION.
           DISPLAY "[" PB1355C-V "]"
           STOP RUN.
