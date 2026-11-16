      *>--- conformance-case
      *> id: g2inv41-fmt-every-argument-form
      *> rules: [2002-FMT-14.8.22.1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.22.1 - the INVOKE general format: INVOKE
      *> {class-name-1 | identifier-1} {identifier-2 | literal-1}
      *> [USING {[BY REFERENCE] {identifier-3 | OMITTED}
      *>       | [BY CONTENT] {arithmetic-expression-1 |
      *>         boolean-expression-1 | identifier-5 | literal-2}
      *>       | [BY VALUE] {arithmetic-expression-1 | identifier-5 |
      *>         literal-2}} ...] [RETURNING identifier-4].
      *> Every alternative is written once below, each BY keyword
      *> both present and absent, and must be accepted.
      *> Values follow §2002:14.1.3 GR8 (by reference: same storage),
      *> GR9 (by content: a copy made by COMPUTE for a numeric formal,
      *> MOVE otherwise), §2002:14.8.22.3 GR2 (identifier-2's content
      *> names the method), GR8 (RETURNING) and GR9 with
      *> §2002:8.8.4.1.7 (OMITTED makes "X IS OMITTED" true).
      *> ADDR adds 1 to X (by reference), returns X + Y + Z and then
      *> zeroes its copy Y; MULU returns X * Y; OPT shows its formals.
      *> EXPECTED:
      *>   1 A=005 R=035       class-name-1 + literal-1 + RETURNING;
      *>                       identifier-1 (universal OU) +
      *>                       identifier-2 MN ("MULU"); BY REFERENCE
      *>                       identifier-3 A B R: R = 5 * 7
      *>   2 A=006 B=007 R=017 BY REFERENCE A (6), BY CONTENT
      *>                       arithmetic-expression B + 1 (8), BY VALUE
      *>                       literal-2 3: 6 + 8 + 3; B untouched
      *>   3 A=007 R=031       keywordless A (BY REFERENCE implied,
      *>                       §2002:14.8.22.2 SR14), BY CONTENT
      *>                       literal-2 10, BY VALUE
      *>                       arithmetic-expression B * 2 (14)
      *>   4 A=008 B=007 R=022 BY CONTENT identifier-5 B (7) and BY
      *>                       VALUE identifier-5 B (7): 8 + 7 + 7
      *>   (5) OPT OMITTED Q=1010 OMITTED, BY CONTENT identifier-5 BB
      *>   (6) OPT OMITTED Q=0010 BY REFERENCE OMITTED, BY CONTENT
      *>                          boolean-expression BB B-AND B"0110"
      *>   (7) OPT X=008 Q=0001   BY REFERENCE A, BY CONTENT literal-2
      *>                          B"0001"
      *>   5 A=009 R=033       every argument keywordless, so its manner
      *>                       follows §2002:14.8.22.3 GR6: A meets SR9
      *>                       and SR10 against BY REFERENCE X (GR6a1,
      *>                       A = 9); literal-2 10 does not, so BY
      *>                       CONTENT (GR6a2, Y = 10); arithmetic-
      *>                       expression B * 2 meets BY VALUE Z (GR6b,
      *>                       Z = 14): 9 + 10 + 14
      *>   (9) OPT X=009 Q=0010   keywordless A (BY REFERENCE) and
      *>                          keywordless boolean-expression
      *>                          BB B-AND B"0110" (BY CONTENT, GR6a2)
      *> Why each leg can fail: any alternative refused, an argument
      *> passed in the wrong manner (A, B or R differ), an expression
      *> mis-evaluated, or OMITTED not seen as omitted.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2INV41.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS G2INV41C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 OB USAGE OBJECT REFERENCE G2INV41C.
       01 OU USAGE OBJECT REFERENCE.
       01 MN PIC X(4) VALUE "MULU".
       01 A  PIC 9(3) VALUE 5.
       01 B  PIC 9(3) VALUE 7.
       01 R  PIC 9(3) VALUE 0.
       01 BB PIC 1(4) VALUE B"1010".
       PROCEDURE DIVISION.
       MAIN.
           INVOKE G2INV41C "NEW" RETURNING OB.
           SET OU TO OB.
           INVOKE OU MN USING BY REFERENCE A B R.
           DISPLAY "1 A=" A " R=" R.
           INVOKE OB "ADDR" USING BY REFERENCE A BY CONTENT B + 1
               BY VALUE 3 RETURNING R.
           DISPLAY "2 A=" A " B=" B " R=" R.
           INVOKE OB "ADDR" USING A BY CONTENT 10 BY VALUE B * 2
               RETURNING R.
           DISPLAY "3 A=" A " R=" R.
           INVOKE OB "ADDR" USING A BY CONTENT B BY VALUE B
               RETURNING R.
           DISPLAY "4 A=" A " B=" B " R=" R.
           INVOKE OB "OPT" USING OMITTED BY CONTENT BB.
           INVOKE OB "OPT" USING BY REFERENCE OMITTED
               BY CONTENT BB B-AND B"0110".
           INVOKE OB "OPT" USING BY REFERENCE A BY CONTENT B"0001".
           INVOKE OB "ADDR" USING A 10 B * 2 RETURNING R.
           DISPLAY "5 A=" A " R=" R.
           INVOKE OB "OPT" USING A BB B-AND B"0110".
           STOP RUN.
       END PROGRAM G2INV41.

       IDENTIFICATION DIVISION.
       CLASS-ID. G2INV41C INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. ADDR.
       DATA DIVISION.
       LINKAGE SECTION.
       01 X  PIC 9(3).
       01 Y  PIC 9(3).
       01 Z  PIC 9(3).
       01 RR PIC 9(3).
       PROCEDURE DIVISION USING BY REFERENCE X Y BY VALUE Z
               RETURNING RR.
           ADD 1 TO X.
           COMPUTE RR = X + Y + Z.
           MOVE 0 TO Y.
       END METHOD ADDR.
       METHOD-ID. MULU.
       DATA DIVISION.
       LINKAGE SECTION.
       01 X  PIC 9(3).
       01 Y  PIC 9(3).
       01 RR PIC 9(3).
       PROCEDURE DIVISION USING X Y RR.
           COMPUTE RR = X * Y.
       END METHOD MULU.
       METHOD-ID. OPT.
       DATA DIVISION.
       LINKAGE SECTION.
       01 X PIC 9(3).
       01 Q PIC 1(4).
       PROCEDURE DIVISION USING OPTIONAL X Q.
           IF X IS OMITTED
               DISPLAY "OPT OMITTED Q=" Q
           ELSE
               DISPLAY "OPT X=" X " Q=" Q
           END-IF.
       END METHOD OPT.
       END OBJECT.
       END CLASS G2INV41C.
