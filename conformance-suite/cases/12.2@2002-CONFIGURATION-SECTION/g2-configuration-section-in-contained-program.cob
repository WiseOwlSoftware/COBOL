      *>--- conformance-case
      *> id: g2-configuration-section-in-contained-program
      *> rules: [2002-SR-12.2.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:12.2.2 SR1 - "The configuration section shall not be
      *> specified in a program that is contained within another
      *> program." G2CFGI is contained in G2CFG01 and writes a
      *> CONFIGURATION SECTION header with no paragraph: every paragraph
      *> of §2002:12.2.1 is optional, so the header alone is the
      *> configuration section, and the refusal must key on the section
      *> in a contained program, not on any paragraph. Everything else
      *> is valid at 2002 (the outermost program has none), so SR1 is
      *> the only reason to refuse.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CFG01.
       PROCEDURE DIVISION.
           CALL "G2CFGI"
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CFGI.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       PROCEDURE DIVISION.
           DISPLAY "IN"
           GOBACK.
       END PROGRAM G2CFGI.
       END PROGRAM G2CFG01.
