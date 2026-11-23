      *>--- conformance-case
      *> id: pb6107_function_arg_omitted_2002
      *> rules: [2002-GR-8.4.2.2.3-7]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:8.4.2.2.3 GR7 - "If a parameter for which the
      *>   omitted-argument condition is true is referenced in an
      *>   activated function, except as an argument or in the
      *>   omitted-argument condition, the EC-PROGRAM-ARG-OMITTED
      *>   exception condition is set to exist and the results of the
      *>   execution of the function are undefined." COBOL-2002 has ONE
      *>   omitted-argument condition for a called program, a function
      *>   and a method; §2014 split it by element kind
      *>   (EC-FUNCTION-ARG-OMITTED, §2014:E.2 item 18), so the 2002
      *>   program enables and handles the PROGRAM name for a FUNCTION.
      *> DERIVATION of every .out line:
      *>   F-CAUGHT 01 - the first activation omits X and G: MOVE X
      *>     references X, so the condition is set to exist; checking
      *>     is on, so the USE procedure H runs
      *>     (§2002:14.5.12.1.2 item 2) and FUNCTION EXCEPTION-STATUS
      *>     there is the condition's name.
      *>   F-CAUGHT 02 - MOVE G1 references the omitted group G.
      *>   RESUME AT NEXT STATEMENT abandons the interrupted MOVE, so R
      *>     keeps "XOMT", which the IS OMITTED test (the exempt use)
      *>     moved there without raising anything: W=[XOMT].
      *>   W=[GH  ] - the second activation supplies both arguments,
      *>     so nothing is raised and MOVE G1 TO R leaves "GH" padded.
       >>TURN EC-PROGRAM-ARG-OMITTED CHECKING ON
       IDENTIFICATION DIVISION.
       FUNCTION-ID. P6107FN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9(2) VALUE 0.
       LINKAGE SECTION.
       01 X PIC X(4).
       01 G.
          05 G1 PIC X(2).
          05 G2 PIC X(2).
       01 R PIC X(4).
       PROCEDURE DIVISION USING OPTIONAL X OPTIONAL G RETURNING R.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-PROGRAM-ARG-OMITTED.
       H-P.
           ADD 1 TO N
           DISPLAY "F-CAUGHT " N "=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       M-P.
           MOVE "----" TO R
           IF X IS OMITTED MOVE "XOMT" TO R END-IF
           MOVE X TO R
           MOVE G1 TO R
           GOBACK.
       END FUNCTION P6107FN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P6107FM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION P6107FN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC X(4).
       01 WA PIC X(4) VALUE "ABCD".
       01 WG PIC X(4) VALUE "GHIJ".
       PROCEDURE DIVISION.
           MOVE FUNCTION P6107FN(OMITTED, OMITTED) TO W
           DISPLAY "W=[" W "]"
           MOVE FUNCTION P6107FN(WA, WG) TO W
           DISPLAY "W=[" W "]"
           STOP RUN.
       END PROGRAM P6107FM.
