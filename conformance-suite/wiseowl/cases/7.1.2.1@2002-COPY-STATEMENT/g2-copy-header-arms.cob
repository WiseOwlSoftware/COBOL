      *>--- conformance-case
      *> id: g2-copy-header-arms
      *> rules: [2002-FMT-7.1.2.1]
      *> accept: [2002]
      *> files: [G2CPL.cpy, G2LIB/G2CPL.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.1.2.1 - the 2002 COPY header is COPY {literal-1 |
      *> text-name-1} [{OF | IN} {literal-2 | library-name-1}]
      *> [SUPPRESS PRINTING]. One COPY per header arm; the REPLACING
      *> arms are g2-copy-replacing-operand-arms (cases tree).
      *> §2002:7.1.2.3 GR1 - "Text-name-1 or literal-1 identifies the
      *> library text to be processed by the COPY statement."
      *> §2002:7.1.2.3 GR3 - "The implementor shall define the rules for
      *> locating the library text"; "When neither library-name-1 nor
      *> literal-2 is specified, a default COBOL library is used." Where
      *> a library is found is the implementor's, so this test lies in
      *> the WiseOwl tree: library G2LIB is the folder G2LIB beside the
      *> test, the default library is the test's own folder, and a
      *> literal-1 or literal-2 value names the same text or library as
      *> the word of that spelling (implementor-defined, SR5).
      *> §2002:7.1.2.3 GR6 - the library text is copied in, "logically
      *> replacing the entire COPY statement".
      *> §2002:7.1.2.3 GR4 - SUPPRESS affects only the listing, so the
      *> copied text still runs.
      *> EXPECTED OUTPUT, DERIVED (why each leg can fail):
      *> [DEFAULT] COPY G2CPL. - no OF/IN: the default library's G2CPL.
      *> [LIBOK]   literal-1 OF library-name-1 SUPPRESS PRINTING: G2LIB's
      *>           G2CPL (DEFAULT if the qualifier is ignored, no line if
      *>           SUPPRESS drops the text).
      *> [LIBOK]   text-name-1 IN literal-2.
      *> [LIBOK]   text-name-1 OF library-name-1.
      *> [LIBOK]   literal-1 IN literal-2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CPH01.
       PROCEDURE DIVISION.
       P1.
           COPY G2CPL.
           COPY "G2CPL" OF G2LIB SUPPRESS PRINTING.
           COPY G2CPL IN "G2LIB".
           COPY G2CPL OF G2LIB.
           COPY "G2CPL" IN "G2LIB".
           STOP RUN.
