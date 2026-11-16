      *>--- conformance-case
      *> id: g2-lk-call-using-access
      *> rules: [2002-GR-13.6.3-3]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.6.3 GR3 - "In a program definition, access to formal
      *> parameters and the returning item is guaranteed if and only if
      *> the program is to execute under the control of a CALL
      *> statement, and the CALL statement contains a USING phrase."
      *> G2LK02S is a program definition activated four times by a
      *> CALL that contains a USING phrase, once in each 2002 form that
      *> can carry one: format 1 (on-overflow) without and with its
      *> ON OVERFLOW phrase - a form 2023 removed - format 2
      *> (on-exception) with NOT ON EXCEPTION, and format 3
      *> (program-prototype) as CALL literal-1 AS NESTED, legal because
      *> G2LK02S is directly contained in G2LK04 (§2002:14.8.4.1,
      *> §2002:14.8.4.2 SR12 and SR13).  Each
      *> time the callee reads both formals (GOT ...), writes them and
      *> writes its returning item; the caller then shows what the
      *> callee wrote through each by-reference argument and the
      *> returning item (BACK ...).  Call 3 passes A1 BY CONTENT, so
      *> the callee reads BYCON but the caller's A1 keeps BYCON.
      *> The "undefined" leg (an operating-system activation) has no
      *> expectation and is not exercised; the implementor-defined leg
      *> (a non-COBOL activator) is a .NET host, documented in
      *> DOC-A.1-116 and outside a COBOL source program.
      *> Fails if a formal does not read the argument, if a write to a
      *> by-reference formal or the returning item is lost, if the
      *> ON OVERFLOW form is refused or takes its overflow branch, or if
      *> the by-content argument is changed.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2LK04.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A1 PIC X(5) VALUE "HELLO".
       01 A2 PIC 9(3) VALUE 7.
       01 R1 PIC X(4) VALUE "....".
       PROCEDURE DIVISION.
       MAIN-P.
           CALL "G2LK02S" USING A1 A2 RETURNING R1
           DISPLAY "BACK1 " A1 " " A2 " " R1
           MOVE "HELLO" TO A1
           MOVE "...." TO R1
           CALL "G2LK02S" USING BY REFERENCE A1 A2 RETURNING R1
               ON OVERFLOW DISPLAY "OVERFLOW"
           END-CALL
           DISPLAY "BACK2 " A1 " " A2 " " R1
           MOVE "BYCON" TO A1
           MOVE "...." TO R1
           CALL "G2LK02S" USING BY CONTENT A1 BY REFERENCE A2
               RETURNING R1
               ON EXCEPTION DISPLAY "EXCEPTION"
               NOT ON EXCEPTION DISPLAY "NOT EXCEPTION"
           END-CALL
           DISPLAY "BACK3 " A1 " " A2 " " R1
           MOVE "HELLO" TO A1
           MOVE "...." TO R1
           CALL "G2LK02S" AS NESTED USING A1 A2 RETURNING R1
               NOT ON EXCEPTION DISPLAY "NOT EXCEPTION4"
           END-CALL
           DISPLAY "BACK4 " A1 " " A2 " " R1
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2LK02S.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N1 PIC 9 VALUE 0.
       LINKAGE SECTION.
       01 P1 PIC X(5).
       01 P2 PIC 9(3).
       01 RT PIC X(4).
       PROCEDURE DIVISION USING P1 P2 RETURNING RT.
       MAIN-S.
           ADD 1 TO N1
           DISPLAY "GOT" N1 " " P1 " " P2
           MOVE "WORLD" TO P1
           ADD 1 TO P2
           MOVE "RTN" TO RT
           MOVE N1 TO RT(4:1)
           GOBACK.
       END PROGRAM G2LK02S.
       END PROGRAM G2LK04.
