      *>--- conformance-case
      *> id: g2call14_positional_correspondence
      *> rules: [2002-GR-14.8.4.3-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.4.3 GR2 -- "This correspondence is positional and
      *> not by name equivalence." "The first argument corresponds to
      *> the first formal parameter, the second to the second". The rule
      *> is stated for ALL FORMATS, so each 2002 format is shown: format
      *> 1 (ON OVERFLOW, absent from later editions), format 2 (ON
      *> EXCEPTION) and format 3 (AS NESTED). Each callee names its
      *> formal parameters B then A, the reverse of the caller's
      *> arguments A then B, so a name match would show B=BBBB.
      *> Format 1: USING A B (BY REFERENCE assumed, §2002:14.8.4.3
      *>   GR5): the callee sees B=AAAA A=BBBB and its MOVE to its
      *>   first formal lands in the caller's A: A=1111 B=BBBB.
      *> Format 2: USING BY CONTENT B A: the callee sees B=BBBB A=1111;
      *>   BY CONTENT, so the caller's items are unchanged.
      *> Format 3: AS NESTED USING A B: the contained program sees
      *>   B=1111 A=BBBB and its MOVE to its first formal sets A=3333.
      *> Fails if any line shows a match by name or a lost argument.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CAL14.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC X(4) VALUE "AAAA".
       01 B PIC X(4) VALUE "BBBB".
       PROCEDURE DIVISION.
       M-P.
           CALL "G2C14S" USING A B
               ON OVERFLOW DISPLAY "F1 OVERFLOW (WRONG)"
           END-CALL
           DISPLAY "F1 AFTER A=" A " B=" B
           CALL "G2C14S" USING BY CONTENT B A
               ON EXCEPTION DISPLAY "F2 ON-EXC (WRONG)"
           END-CALL
           DISPLAY "F2 AFTER A=" A " B=" B
           CALL "G2C14N" AS NESTED USING A B
           DISPLAY "F3 AFTER A=" A " B=" B
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2C14N.
       DATA DIVISION.
       LINKAGE SECTION.
       01 B PIC X(4).
       01 A PIC X(4).
       PROCEDURE DIVISION USING B A.
       N-P.
           DISPLAY "N SEES B=" B " A=" A
           MOVE "3333" TO B
           GOBACK.
       END PROGRAM G2C14N.
       END PROGRAM G2CAL14.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2C14S.
       DATA DIVISION.
       LINKAGE SECTION.
       01 B PIC X(4).
       01 A PIC X(4).
       PROCEDURE DIVISION USING B A.
       S-P.
           DISPLAY "S SEES B=" B " A=" A
           MOVE "1111" TO B
           GOBACK.
       END PROGRAM G2C14S.
