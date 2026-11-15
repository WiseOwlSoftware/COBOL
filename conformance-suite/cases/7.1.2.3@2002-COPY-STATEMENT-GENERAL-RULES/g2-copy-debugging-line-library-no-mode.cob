      *>--- conformance-case
      *> id: g2-copy-debugging-line-library-no-mode
      *> rules: [2002-GR-7.1.2.3-7, 2002-GR-7.1.2.3-14]
      *> accept: [2002]
      *> files: [G2CBDBG.cpy, G2CBDNEST.cpy, G2CBIN.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.1.2.3 GR7 - "If the REPLACING phrase is not specified,
      *> the library text is included in the resultant text unchanged":
      *> G2CBDBG's D line stays a debugging line, and without WITH
      *> DEBUGGING MODE §2002:6.2.7 makes it a comment "After all COPY
      *> and REPLACE statements have been processed": ALWAYS prints,
      *> DEBUGLINE does not. §2002:7.1.2.3 GR14 - "the library text may
      *> contain a COPY statement that does not include a REPLACING
      *> phrase": G2CBDNEST's COPY G2CBIN is on a D line; its text lands
      *> on debugging lines (§2002:7.1.2.3 GR13), which become comments:
      *> OUTER prints, INNER does not.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CPY12.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. COMPUTER-1.
       OBJECT-COMPUTER. COMPUTER-1.
       PROCEDURE DIVISION.
       P1.
           COPY G2CBDBG.
           COPY G2CBDNEST.
           DISPLAY "END".
           STOP RUN.
