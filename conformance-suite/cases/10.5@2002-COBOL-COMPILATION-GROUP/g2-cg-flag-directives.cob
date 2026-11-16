      *>--- conformance-case
      *> id: g2-cg-flag-directives
      *> rules: [2002-SR-10.5.2-6]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:10.5.2 SR6 - "Compiler directives may appear in a structured
      *> compilation group as specified in 7.2, Compiler directives." The
      *> 2002-only directives of 7.2 (FLAG-85, FLAG-NATIVE-ARITHMETIC) in a
      *> compilation group of two source units (a program prototype, which
      *> 10.5.2 SR1 puts first, then a program definition), each written
      *> where its own syntax rule places it: §2002:7.2.11.2 SR1 and
      *> §2002:7.2.12.2 SR1 "only between clauses in divisions other than
      *> the procedure division and only between statements in the
      *> procedure division". Fails if either directive is refused at a
      *> permitted place or disturbs the program: the output is the
      *> DISPLAYed values X=1, Y=2 and OK, in order. The flagging directives
      *> only enable warnings (§2002:7.2.11.3 GR1, §2002:7.2.12.3 GR1); no
      *> flagged construct is written, so the output is unchanged.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CGPF IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 P1 PIC X.
       PROCEDURE DIVISION USING P1.
       END PROGRAM G2CGPF.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CGFD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC 9
       >>FLAG-85 ALL ON
           VALUE 1.
       01 Y PIC 9
       >>FLAG-NATIVE-ARITHMETIC ON
           VALUE 2.
       PROCEDURE DIVISION.
           DISPLAY "X=" X
       >>FLAG-85 ALL OFF
           DISPLAY "Y=" Y
       >>FLAG-NATIVE-ARITHMETIC OFF
           DISPLAY "OK"
           STOP RUN.
       END PROGRAM G2CGFD.
