      *>--- conformance-case
      *> id: g2002-call-as-nested
      *> rules: [2002-SR-14.8.4.2-13]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.4.2 SR13 - "If the NESTED phrase is specified, literal-1
      *> shall be specified. Literal-1 shall be the same as the program-name
      *> specified in a PROGRAM-ID paragraph of a common program as specified
      *> in 8.4.5.2, Scope of program-names, or of a program that is directly
      *> contained in the calling program." Format 3 (program-prototype).
      *> EXPECTED VALUES, DERIVED FROM THE 2002 RULES:
      *>  1. G2CAL03 calls G2CAL03D, directly contained in it (SR13 second
      *>     arm). GR6: that program gives the called program's
      *>     characteristics. N BY CONTENT is passed as 0005, M BY REFERENCE
      *>     as MMMM: "D GOT 0005 MMMM".
      *>  2. G2CAL03D calls G2CAL03C, a COMMON program directly contained in
      *>     G2CAL03, which contains G2CAL03D: §2002:8.4.5.2 rule 2 lets it
      *>     be referenced there (SR13 first arm): "C GOT 0042".
      *>  3. D's ADD 1 TO P does not reach N (BY CONTENT), its MOVE reaches M
      *>     (BY REFERENCE): "N=0005 M=DDDD".
      *>  4. G2CAL03 calls the directly contained common program with a BY
      *>     CONTENT arithmetic-expression-1, a Format 3 operand:
      *>     "C GOT 0006".
      *> A leg fails if a permitted AS NESTED target is refused or called
      *> with the wrong values. The refusals are g2002-call-as-nested-identifier
      *> (sentence 1) and g2002-call-as-nested-scope (sentence 2).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CAL03.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9(4) VALUE 5.
       01 M PIC X(4) VALUE "MMMM".
       PROCEDURE DIVISION.
       MAIN.
           CALL "G2CAL03D" AS NESTED USING BY CONTENT N BY REFERENCE M
           DISPLAY "N=" N " M=" M
           CALL "G2CAL03C" AS NESTED USING BY CONTENT (N + 1)
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CAL03C IS COMMON PROGRAM.
       DATA DIVISION.
       LINKAGE SECTION.
       01 P PIC 9(4).
       PROCEDURE DIVISION USING P.
       MAIN.
           DISPLAY "C GOT " P
           GOBACK.
       END PROGRAM G2CAL03C.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CAL03D.
       DATA DIVISION.
       LINKAGE SECTION.
       01 P PIC 9(4).
       01 Q PIC X(4).
       PROCEDURE DIVISION USING P Q.
       MAIN.
           DISPLAY "D GOT " P " " Q
           ADD 1 TO P
           MOVE "DDDD" TO Q
           CALL "G2CAL03C" AS NESTED USING BY CONTENT 42
           GOBACK.
       END PROGRAM G2CAL03D.
       END PROGRAM G2CAL03.
