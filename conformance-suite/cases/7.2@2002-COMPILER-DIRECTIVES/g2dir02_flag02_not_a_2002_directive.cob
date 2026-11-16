      *>--- conformance-case
      *> id: g2dir02_flag02_not_a_2002_directive
      *> rules: [2002-FMT-7.2.1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.2.1 with §2002:7.2.2 SR6 - compiler-instruction "is
      *>   composed of compiler-directive words, system-names, and
      *>   user-defined words as specified in the syntax of each
      *>   directive"; the 2002 directives are 7.2.8 to 7.2.19, and
      *>   FLAG-02 is not among them (it is a later edition's word).
      *>   >>FLAG-02 is therefore no 2002 compiler-instruction and the
      *>   program is refused at --std 2002 (the accepted forms are
      *>   g2dir01_every_directive_spacing). It fails if the 2002 mode
      *>   admits a later edition's directive.
       >>FLAG-02 ALL ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2DIR02.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "RAN"
           STOP RUN.
