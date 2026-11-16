      *>--- conformance-case
      *> id: g2dir01_every_directive_spacing
      *> rules: [2002-FMT-7.2.1, 2002-SR-7.2.2-5]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.2.1 - the general format of a compiler directive is
      *>   ">>compiler-instruction", and compiler-instruction "is
      *>   composed of compiler-directive words, system-names, and
      *>   user-defined words as specified in the syntax of each
      *>   directive" (§2002:7.2.2 SR6). The 2002 directives are
      *>   7.2.8 to 7.2.19: CALL-CONVENTION, DEFINE, EVALUATE, FLAG-85,
      *>   FLAG-NATIVE-ARITHMETIC, IF, LEAP-SECOND, LISTING, PAGE,
      *>   PROPAGATE, SOURCE FORMAT, TURN. Every one is written below,
      *>   each in its 2002 general format, FLAG-85 and
      *>   FLAG-NATIVE-ARITHMETIC (2002 only) among them.
      *> §2002:7.2.2 SR5 - "The compiler directive indicator shall be
      *>   treated as though it were followed by a space if no space is
      *>   specified after the indicator": every directive is written
      *>   once as ">>WORD" and once as ">> WORD", and the IF/ELSE/
      *>   END-IF and EVALUATE/WHEN/END-EVALUATE phrases are written
      *>   both ways, so a compiler that recognizes only one spelling
      *>   refuses the program or selects the wrong lines.
      *> Placement: LEAP-SECOND and PROPAGATE "shall not be specified
      *>   within a compilation unit" (§2002:7.2.14.2 SR1,
      *>   §2002:7.2.17.2 SR1), so they precede IDENTIFICATION DIVISION;
      *>   FLAG-85 and FLAG-NATIVE-ARITHMETIC sit "only between
      *>   statements in the procedure division" (§2002:7.2.11.2 SR1,
      *>   §2002:7.2.12.2 SR1). LISTING, PAGE, CALL-CONVENTION, TURN,
      *>   LEAP-SECOND, PROPAGATE, FLAG-85 and FLAG-NATIVE-ARITHMETIC
      *>   select no lines and print nothing; native arithmetic is in
      *>   effect (no ARITHMETIC clause), so "no flagging of native
      *>   arithmetic occurs" (§2002:7.2.12.3 GR4).
      *> EXPECTED OUTPUT, DERIVED:
      *>   A          the first DISPLAY.
      *>   IF1-TRUE   V1 (>>DEFINE, no space) is 1: >>IF TRUE includes
      *>              text-1 (§2002:7.2.13.3 GR2).
      *>   IF2-FALSE  V2 (>> DEFINE, a space) is 2, so >> IF V2 = 1 is
      *>              FALSE and the >> ELSE text-2 is included
      *>              (§2002:7.2.13.3 GR3).
      *>   EV1-TWO    >>EVALUATE V2: the >>WHEN 2 is equal to the
      *>              subject (§2002:7.2.10.3 GR4a).
      *>   EV2-OTHER  >> EVALUATE V1: >> WHEN 2 is not equal, so the
      *>              >> WHEN OTHER text-2 is included (GR5).
      *>   2          N is 1 and ADD 1 TO N makes it 2.
       >>SOURCE FORMAT IS FIXED
       >> SOURCE FORMAT IS FIXED
       >>CALL-CONVENTION COBOL
       >> CALL-CONVENTION COBOL
       >>DEFINE V1 AS 1
       >> DEFINE V2 AS 2
       >>LEAP-SECOND OFF
       >> LEAP-SECOND OFF
       >>PROPAGATE OFF
       >> PROPAGATE OFF
       >>LISTING ON
       >> LISTING ON
       >>TURN EC-SIZE CHECKING ON
       >> TURN EC-SIZE CHECKING ON
       >>PAGE comment text without a space
       >> PAGE comment text after a space
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2DIR01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "A"
           >>FLAG-85 ALL ON
           >> FLAG-85 MOVE OFF
           >>FLAG-NATIVE-ARITHMETIC ON
           ADD 1 TO N
           >> FLAG-NATIVE-ARITHMETIC OFF
           >>IF V1 = 1
           DISPLAY "IF1-TRUE"
           >>ELSE
           DISPLAY "IF1-FALSE"
           >>END-IF
           >> IF V2 = 1
           DISPLAY "IF2-TRUE"
           >> ELSE
           DISPLAY "IF2-FALSE"
           >> END-IF
           >>EVALUATE V2
           >>WHEN 1
           DISPLAY "EV1-ONE"
           >>WHEN 2
           DISPLAY "EV1-TWO"
           >>WHEN OTHER
           DISPLAY "EV1-OTHER"
           >>END-EVALUATE
           >> EVALUATE V1
           >> WHEN 2
           DISPLAY "EV2-TWO"
           >> WHEN OTHER
           DISPLAY "EV2-OTHER"
           >> END-EVALUATE
           >>LISTING OFF
           DISPLAY N
           STOP RUN.
