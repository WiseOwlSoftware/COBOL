      *>--- conformance-case
      *> id: g2inv-sr20-cd-sending-operands
      *> rules: [2002-SR-14.8.22.2-20]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.22.2 SR20 - "Identifier-5 and any identifier
      *>   specified in arithmetic-expression-1 or boolean-expression-1
      *>   is a sending operand." §2002:14.8.22.3 GR7a: identifier-5
      *>   and the expressions are evaluated at the beginning of the
      *>   INVOKE and their values "are made available to the invoked
      *>   method". Every operand here is an item of a 2002 output CD
      *>   (the communication-section reach the 2023 twin lacks): the
      *>   CD record items R4, RN, RB, the level-1 object reference
      *>   record CDOBJ (§2002:14.8.22.2 SR15 admits class object BY
      *>   VALUE) and the CD's implicit TEXT LENGTH item TL.
      *> Each method alters its formal parameter; a sending operand is
      *>   only read, so the caller's item keeps its value:
      *>   R4 BY CONTENT: method sees CCCC, caller still R4=CCCC.
      *>   RN BY VALUE: method sees 0007, caller still RN=0007.
      *>   RN * 2 BY VALUE and RN + 3 BY CONTENT: values 0014, 0010;
      *>     RN (inside the expressions) still 0007.
      *>   RB B-AND B"0110" BY CONTENT with RB = 1010: value 0010;
      *>     RB still 1010.
      *>   TL + 1 BY CONTENT and TL BY VALUE with TL = 5: values
      *>     0006 and 0005; TL still 0005.
      *>   CDOBJ BY VALUE: method sees an object and sets its
      *>     parameter to NULL; CDOBJ still references O (CDOBJ SAME).
      *> §2002:A.1.2.2 SR9: the CD records "implicitly redefine the
      *>   record area established by the output CD entry", which also
      *>   holds TL, so the legs run in an order that never reads an
      *>   item after a write to an overlapping one.
      *> A compiler that passes a CD operand by reference, or refuses
      *>   a CD operand or the level-1 object record, fails a line.
      *> The CD draws the obsolete warning COBOL0903 at 2002.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2INVS20.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS G2CS20.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O  USAGE OBJECT REFERENCE G2CS20.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           TEXT LENGTH IS TL.
       01  CDREC.
           05 R4 PIC X(4).
           05 RN PIC 9(4).
           05 RB PIC 1(4).
       01  CDOBJ USAGE OBJECT REFERENCE G2CS20.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE G2CS20 "NEW" RETURNING O
           MOVE "CCCC" TO R4
           INVOKE O "SETX" USING BY CONTENT R4
           DISPLAY "R4=" R4
           MOVE 7 TO RN
           INVOKE O "SETV" USING BY VALUE RN
           DISPLAY "RN=" RN
           INVOKE O "SETV" USING BY VALUE RN * 2
           INVOKE O "SETN" USING BY CONTENT RN + 3
           DISPLAY "RN=" RN
           MOVE B"1010" TO RB
           INVOKE O "SETB" USING BY CONTENT RB B-AND B"0110"
           DISPLAY "RB=" RB
           MOVE 5 TO TL
           INVOKE O "SETN" USING BY CONTENT TL + 1
           INVOKE O "SETV" USING BY VALUE TL
           DISPLAY "TL=" TL
           SET CDOBJ TO O
           INVOKE O "TAKE" USING BY VALUE CDOBJ
           IF CDOBJ = O
               DISPLAY "CDOBJ SAME"
           ELSE
               DISPLAY "CDOBJ CHANGED"
           END-IF
           STOP RUN.
       END PROGRAM G2INVS20.

       IDENTIFICATION DIVISION.
       CLASS-ID. G2CS20 INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. SETX.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-X PIC X(4).
       PROCEDURE DIVISION USING LK-X.
           DISPLAY "SETX GOT " LK-X
           MOVE "ZZZZ" TO LK-X.
       END METHOD SETX.
       METHOD-ID. SETV.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-V PIC 9(4).
       PROCEDURE DIVISION USING BY VALUE LK-V.
           DISPLAY "SETV GOT " LK-V
           ADD 1 TO LK-V.
       END METHOD SETV.
       METHOD-ID. SETN.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-N PIC 9(4).
       PROCEDURE DIVISION USING LK-N.
           DISPLAY "SETN GOT " LK-N
           ADD 1 TO LK-N.
       END METHOD SETN.
       METHOD-ID. SETB.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-B PIC 1(4).
       PROCEDURE DIVISION USING LK-B.
           DISPLAY "SETB GOT " LK-B
           MOVE B"1111" TO LK-B.
       END METHOD SETB.
       METHOD-ID. TAKE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-O USAGE OBJECT REFERENCE G2CS20.
       PROCEDURE DIVISION USING BY VALUE LK-O.
           IF LK-O = NULL
               DISPLAY "TAKE GOT NULL"
           ELSE
               DISPLAY "TAKE GOT OBJECT"
           END-IF
           SET LK-O TO NULL.
       END METHOD TAKE.
       END OBJECT.
       END CLASS G2CS20.
