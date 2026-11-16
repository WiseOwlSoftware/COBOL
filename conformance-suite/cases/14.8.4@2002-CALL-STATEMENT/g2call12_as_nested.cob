      *>--- conformance-case
      *> id: g2call12_as_nested
      *> rules: [2002-SR-14.8.4.2-12]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.4.2 SR12 - "The NESTED phrase may be specified only
      *>   in a program definition." The granted form: CALL literal-1 AS
      *>   NESTED (format 3) written in a program definition, naming a
      *>   program directly contained in the calling program
      *>   (§2002:14.8.4.2 SR13). The case shows the form accepted and
      *>   that control reaches the contained program and returns.
      *> EXPECTED OUTPUT, DERIVED:
      *>   G2CALLIN-ENTERED   the CALL transfers control to the nested
      *>                      program, which displays this line.
      *>   ARG=AFTER          the BY REFERENCE argument (implied) is the
      *>                      nested program's formal; its MOVE is seen
      *>                      by the caller after control returns.
      *>   G2CALL12-DONE      control returns to the caller after the
      *>                      CALL and the program continues.
      *> Each leg can fail: a refusal of AS NESTED in a program
      *> definition rejects the program; a CALL bound to no program, or
      *> to the wrong one, loses line 1; a wrong argument mode loses
      *> line 2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CALL12.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-ARG PIC X(5) VALUE "PRIOR".
       PROCEDURE DIVISION.
       MAIN-PARA.
           CALL "G2CALLIN" AS NESTED USING WS-ARG
           DISPLAY "ARG=" WS-ARG
           DISPLAY "G2CALL12-DONE"
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CALLIN.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-ARG PIC X(5).
       PROCEDURE DIVISION USING L-ARG.
       IN-PARA.
           DISPLAY "G2CALLIN-ENTERED"
           MOVE "AFTER" TO L-ARG
           GOBACK.
       END PROGRAM G2CALLIN.
       END PROGRAM G2CALL12.
