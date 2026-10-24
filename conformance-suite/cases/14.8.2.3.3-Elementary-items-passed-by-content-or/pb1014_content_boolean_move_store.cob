*>--- conformance-case
*> id: pb1014_content_boolean_move_store
*> rules: [GR-14.8.2.3.3-L2.2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1014 - ISO 14.8.2.3.3 2) d): "Otherwise, the conformance
      *> rules are the same as for a MOVE statement with the argument as the
      *> sending operand and the corresponding formal parameter as the
      *> receiving operand." A BY CONTENT boolean literal or boolean
      *> expression argument therefore lands in its formal as the MOVE of
      *> that boolean value lands: 14.9.25.4 6) "If the sending item is of
      *> class boolean, its boolean value shall be moved", 14.9.25.4 6) a)
      *> alignment "as defined in 14.6.8" (JUSTIFIED included), 14.6.8.6 for
      *> a boolean receiver, and the receiving item's own EDITING (14.9.25.4
      *> 6): "along with any editing specified for ... the receiving data
      *> item", simple insertion 13.18.40.5). 14.8.2.3.3 2) c) and
      *> 13.18.2.4 1) b): an ANY LENGTH formal takes the length of the
      *> argument.
      *> EXPECTED VALUES, DERIVED (S = B"10", or B1 B-AND B2 = B"11" B-AND
      *> B"10" = B"10"; W = B3 B-AND B4 = B"1011" B-AND B"1111" = B"1011"):
      *>  X1  S into PIC XBX (the B is a space insertion between the two
      *>      data positions)                          => [1 0]
      *>  X2  W into PIC XBX: the four boolean characters are cut to the
      *>      two data positions                        => [1 0]
      *>  N1  S into PIC NBN (national-edited)          => [1 0]
      *>  J1  S into PIC X(4) JUSTIFIED RIGHT           => [  10]
      *>  B1  S into PIC 1(4): zero fill on the right   => [1000]
      *>  B2  W into PIC 1(2): truncated on the right   => [10]
      *>  L1  W into PIC X ANY LENGTH: length 4         => [1011]4
      *>  L2  W into PIC 1 ANY LENGTH: length 4         => [1011]4
      *> The literal S (X1 N1 J1 B1) and the expression S (X1 N1 J1) are
      *> both sent; each line is printed by the CALL lane (C:) and then
      *> the INVOKE lane (I:). Before the fix the editing formals (X1, N1)
      *> printed [10 ] on the INVOKE lane (both senders) and the CALL lane
      *> (the expression): the boolean value was stored by a second, hand
      *> written store that did not edit.
       IDENTIFICATION DIVISION.
       CLASS-ID. PB1014K INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY. CLASS BASE CLASS PB1014K.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       IDENTIFICATION DIVISION.
       METHOD-ID. MXE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC XBX.
       PROCEDURE DIVISION USING L.
           DISPLAY "I:XE=[" L "]"
           GOBACK.
       END METHOD MXE.
       IDENTIFICATION DIVISION.
       METHOD-ID. MNE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC NBN.
       PROCEDURE DIVISION USING L.
           DISPLAY "I:NE=[" L "]"
           GOBACK.
       END METHOD MNE.
       IDENTIFICATION DIVISION.
       METHOD-ID. MJ.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC X(4) JUSTIFIED RIGHT.
       PROCEDURE DIVISION USING L.
           DISPLAY "I:J=[" L "]"
           GOBACK.
       END METHOD MJ.
       IDENTIFICATION DIVISION.
       METHOD-ID. MB4.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC 1(4).
       PROCEDURE DIVISION USING L.
           DISPLAY "I:B4=[" L "]"
           GOBACK.
       END METHOD MB4.
       IDENTIFICATION DIVISION.
       METHOD-ID. MB2.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC 1(2).
       PROCEDURE DIVISION USING L.
           DISPLAY "I:B2=[" L "]"
           GOBACK.
       END METHOD MB2.
       IDENTIFICATION DIVISION.
       METHOD-ID. MLX.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC X ANY LENGTH.
       PROCEDURE DIVISION USING L.
           DISPLAY "I:LX=[" L "]" FUNCTION LENGTH(L)
           GOBACK.
       END METHOD MLX.
       IDENTIFICATION DIVISION.
       METHOD-ID. MLB.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC 1 ANY LENGTH.
       PROCEDURE DIVISION USING L.
           DISPLAY "I:LB=[" L "]" FUNCTION LENGTH(L)
           GOBACK.
       END METHOD MLB.
       END OBJECT.
       END CLASS PB1014K.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1014R.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY. CLASS PB1014K.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE PB1014K.
       01 B1 PIC 1(2) VALUE B"11".
       01 B2 PIC 1(2) VALUE B"10".
       01 B3 PIC 1(4) VALUE B"1011".
       01 B4 PIC 1(4) VALUE B"1111".
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB1014K "NEW" RETURNING O
           CALL "PB1014XE" AS NESTED USING BY CONTENT B"10"
           INVOKE O "MXE" USING BY CONTENT B"10"
           CALL "PB1014XE" AS NESTED USING BY CONTENT B1 B-AND B2
           INVOKE O "MXE" USING BY CONTENT B1 B-AND B2
           CALL "PB1014XE" AS NESTED USING BY CONTENT B3 B-AND B4
           INVOKE O "MXE" USING BY CONTENT B3 B-AND B4
           CALL "PB1014NE" AS NESTED USING BY CONTENT B"10"
           INVOKE O "MNE" USING BY CONTENT B"10"
           CALL "PB1014J" AS NESTED USING BY CONTENT B"10"
           INVOKE O "MJ" USING BY CONTENT B"10"
           CALL "PB1014J" AS NESTED USING BY CONTENT B1 B-AND B2
           INVOKE O "MJ" USING BY CONTENT B1 B-AND B2
           CALL "PB1014B4" AS NESTED USING BY CONTENT B"10"
           INVOKE O "MB4" USING BY CONTENT B"10"
           CALL "PB1014B2" AS NESTED USING BY CONTENT B3 B-AND B4
           INVOKE O "MB2" USING BY CONTENT B3 B-AND B4
           CALL "PB1014LX" AS NESTED USING BY CONTENT B3 B-AND B4
           INVOKE O "MLX" USING BY CONTENT B3 B-AND B4
           CALL "PB1014LB" AS NESTED USING BY CONTENT B3 B-AND B4
           INVOKE O "MLB" USING BY CONTENT B3 B-AND B4
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1014XE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC XBX.
       PROCEDURE DIVISION USING L.
           DISPLAY "C:XE=[" L "]"
           GOBACK.
       END PROGRAM PB1014XE.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1014NE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC NBN.
       PROCEDURE DIVISION USING L.
           DISPLAY "C:NE=[" L "]"
           GOBACK.
       END PROGRAM PB1014NE.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1014J.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC X(4) JUSTIFIED RIGHT.
       PROCEDURE DIVISION USING L.
           DISPLAY "C:J=[" L "]"
           GOBACK.
       END PROGRAM PB1014J.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1014B4.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC 1(4).
       PROCEDURE DIVISION USING L.
           DISPLAY "C:B4=[" L "]"
           GOBACK.
       END PROGRAM PB1014B4.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1014B2.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC 1(2).
       PROCEDURE DIVISION USING L.
           DISPLAY "C:B2=[" L "]"
           GOBACK.
       END PROGRAM PB1014B2.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1014LX.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC X ANY LENGTH.
       PROCEDURE DIVISION USING L.
           DISPLAY "C:LX=[" L "]" FUNCTION LENGTH(L)
           GOBACK.
       END PROGRAM PB1014LX.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1014LB.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC 1 ANY LENGTH.
       PROCEDURE DIVISION USING L.
           DISPLAY "C:LB=[" L "]" FUNCTION LENGTH(L)
           GOBACK.
       END PROGRAM PB1014LB.
       END PROGRAM PB1014R.
