      *>--- conformance-case
      *> id: g2-copy-replacing-word-and-nesting-mode
      *> rules: [2002-GR-7.1.2.3-6, 2002-GR-7.1.2.3-7,
      *>     2002-GR-7.1.2.3-11, 2002-GR-7.1.2.3-14]
      *> accept: [2002]
      *> files: [G2CBW.cpy, G2CBLIT.cpy, G2CBDBG.cpy, G2CBDNEST.cpy,
      *>     G2CBIN.cpy, G2CBN1.cpy, G2CBN2.cpy, G2CBN3.cpy, G2CBN4.cpy,
      *>     G2CBN5.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.1.2.3 GR6 - the library text is copied "logically
      *> replacing the entire COPY statement beginning with the reserved
      *> word COPY and ending with the separator period, inclusive": the
      *> 2002 word-1 BY literal-4 form COPY G2CBW REPLACING AAA BY
      *> "HELLO". leaves only G2CBW's DISPLAY AAA. with AAA replaced
      *> (HELLO). Left- over REPLACING words or period fail the compile.
      *> §2002:7.1.2.3 GR11 - "If the REPLACING phrase is specified, the
      *> library text shall not contain a COPY statement": G2CBLIT holds
      *> the word COPY only inside an alphanumeric literal, which is no
      *> COPY statement, so the REPLACING COPY is accepted and prints
      *> COPY G2CBW.-HELLO (the literal unchanged, AAA replaced).
      *> §2002:7.1.2.3 GR7 - "If the REPLACING phrase is not specified,
      *> the library text is included in the resultant text unchanged":
      *> G2CBDBG's D line stays a debugging line, compiled WITH
      *> DEBUGGING MODE (ALWAYS, DEBUGLINE). A dropped D line loses
      *> DEBUGLINE. §2002:7.1.2.3 GR14 - "the library text may contain a
      *> COPY statement that does not include a REPLACING phrase"; "The
      *> implementation shall support nesting of at least 5 levels,
      *> including the first COPY statement in the sequence":
      *> G2CBDNEST's nested COPY sits on a D line and is processed
      *> before the line becomes a comment or is compiled (§2002:6.2.7),
      *> here compiled (OUTER, INNER); COPY G2CBN1 is level 1 and
      *> G2CBN4's COPY G2CBN5 level 5 (LEVEL-1 .. LEVEL-5).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CPY11.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. COMPUTER-1 WITH DEBUGGING MODE.
       OBJECT-COMPUTER. COMPUTER-1.
       PROCEDURE DIVISION.
       P1.
           COPY G2CBW REPLACING AAA BY "HELLO".
           COPY G2CBLIT REPLACING AAA BY "-HELLO".
           COPY G2CBDBG.
           COPY G2CBDNEST.
           COPY G2CBN1.
           STOP RUN.
