      *>--- conformance-case
      *> id: g2tn01_default_checking_off
      *> rules: [2002-GR-7.2.19.3-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.2.19.3 GR1 - "The default TURN directive is '>>TURN
      *>   EC-ALL CHECKING OFF'." §2002:14.5.12.1: "By default, checking
      *>   is not enabled for any exception condition" and "if checking
      *>   for an exception that occurs is not enabled, no exception
      *>   condition is raised". No TURN precedes M1, so checking for
      *>   EC-OVERFLOW-STRING is off there.
      *> The STRING moves 7 characters into a 3-character receiver:
      *>   §2002:14.8.39.3 GR8a "No further data is transferred" after
      *>   ABC, GR8b "The EC-OVERFLOW-STRING exception condition is set
      *>   to exist", GR8c the ON OVERFLOW phrase, when specified, gets
      *>   control. EC-OVERFLOW-STRING is non-fatal (table 14, NF), so
      *>   §2002:14.5.12.1.3 "If it is not enabled, processing proceeds
      *>   as indicated for the statement": the USE declarative is NOT
      *>   executed and the last exception status keeps "no exception"
      *>   (set by SET LAST EXCEPTION TO OFF, §2002:14.5.12.1), for
      *>   which EXCEPTION-STATUS returns 31 spaces (§2002:15.28.2 RV1).
      *> M2 is the control: after >>TURN EC-OVERFLOW-STRING CHECKING ON
      *>   (§2002:7.2.19.3 GR4) the same STRING, with no ON OVERFLOW,
      *>   executes the declarative (§2002:14.5.12.1.3 item 2), which
      *>   sees EXCEPTION-STATUS = EC-OVERFLOW-STRING, and then execution
      *>   continues. It shows the declarative and the status can be
      *>   reached, so the default-off legs can fail.
      *> EXPECTED OUTPUT, DERIVED:
      *>   DEFAULT W-DST=[ABC] HIT=N         GR8a; declarative not run
      *>   DEFAULT STATUS=[<31 spaces>]       nothing raised (GR1)
      *>   DEFAULT ON-OVERFLOW                GR8c runs with checking off
      *>   DEFAULT STATUS2=[<31 spaces>]      still nothing raised
      *>   DECL [EC-OVERFLOW-STRING<13 sp>]   control: checking on
      *>   ENABLED HIT=Y                      execution continued
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2TN01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-DST PIC X(3).
       01 W-HIT PIC X VALUE "N".
       PROCEDURE DIVISION.
       DECLARATIVES.
       D1 SECTION.
           USE AFTER EXCEPTION CONDITION EC-OVERFLOW-STRING.
       D1P.
           MOVE "Y" TO W-HIT
           DISPLAY "DECL [" FUNCTION EXCEPTION-STATUS "]".
       END DECLARATIVES.
       MAIN SECTION.
       M1.
           SET LAST EXCEPTION TO OFF
           MOVE SPACES TO W-DST
           STRING "ABCDEFG" DELIMITED BY SIZE INTO W-DST
           END-STRING
           DISPLAY "DEFAULT W-DST=[" W-DST "] HIT=" W-HIT
           DISPLAY "DEFAULT STATUS=[" FUNCTION EXCEPTION-STATUS "]"
           STRING "ABCDEFG" DELIMITED BY SIZE INTO W-DST
               ON OVERFLOW DISPLAY "DEFAULT ON-OVERFLOW"
           END-STRING
           DISPLAY "DEFAULT STATUS2=[" FUNCTION EXCEPTION-STATUS "]".
       >>TURN EC-OVERFLOW-STRING CHECKING ON
       M2.
           STRING "ABCDEFG" DELIMITED BY SIZE INTO W-DST
           END-STRING
           DISPLAY "ENABLED HIT=" W-HIT
           STOP RUN.
