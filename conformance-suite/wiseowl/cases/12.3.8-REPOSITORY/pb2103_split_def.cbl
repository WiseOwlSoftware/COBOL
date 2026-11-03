       >>TURN EC-PROGRAM-ARG-MISMATCH CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2103SDEF AS "P2103SQ".
       DATA DIVISION.
       LINKAGE SECTION.
       01 D-G.
          03 D-S.
             05 D-A PIC 9(2).
          03 D-B PIC X(2).
       PROCEDURE DIVISION USING D-G.
       P-MAIN.
           DISPLAY "A=" D-A " B=" D-B
           ADD 5 TO D-A
           MOVE "ZZ" TO D-B
           GOBACK.
       END PROGRAM P2103SDEF.
