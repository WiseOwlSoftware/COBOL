      *>--- conformance-case
      *> id: g2-exit-section-outside-section
      *> rules: [2002-SR-14.8.13.2-11]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> NEGATIVE. §2002:14.8.13.2 SR11 - "The EXIT statement with the
      *>   SECTION phrase may be specified only in a section." The
      *>   procedure division below has paragraphs and no section, so
      *>   EXIT SECTION shall be refused (COBOL0827). The accepted form
      *>   is g2_exit_section_in_section.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2EXN11.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "M".
           EXIT SECTION.
       P1.
           DISPLAY "P1" STOP RUN.
