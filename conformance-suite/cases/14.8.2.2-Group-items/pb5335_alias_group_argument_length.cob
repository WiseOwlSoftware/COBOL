      *>--- conformance-case
      *> id: pb5335_alias_group_argument_length
      *> rules: [SR-14.8.2.2-1, GR-13.18.45.4-1]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *>---
       >>TURN EC-PROGRAM-ARG-MISMATCH CHECKING ON
      *> 13.18.45.4 GR1: "When the THROUGH phrase is not specified, all
      *> of the data attributes of data-name-2 become the data
      *> attributes of data-name-1", so GT-ALIAS is the group GT, six
      *> bytes long. A CALL by data-name locates its program at run
      *> time, where 14.9.4.4 GR3d applies the conformance rules of
      *> 14.8.2: by 14.8.2.2 SR1 a group passed by reference "shall be
      *> described with the same number or a smaller number of bytes"
      *> in the formal, so PB5335S6 (six bytes) takes GT-ALIAS and
      *> PB5335S8 (eight) raises EC-PROGRAM-ARG-MISMATCH for it exactly
      *> as for GT (kb/Work PB5335: the alias stated no length, so the
      *> mismatch went undetected and the formal was laid over eight
      *> bytes of a six-byte argument). Each EC= line fails if the
      *> mismatch is not raised (a T= line instead); each T= line
      *> fails if the alias were refused or passed short.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5335M.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-SIX PIC X(8) VALUE "PB5335S6".
       01 WS-EIGHT PIC X(8) VALUE "PB5335S8".
       01 EX PIC X(31).
       01 REC.
          05 GT.
             10 T PIC X(2) OCCURS 3 TIMES.
       66 GT-ALIAS RENAMES GT.
       PROCEDURE DIVISION.
       DECLARATIVES.
       ERR-SECT SECTION.
           USE AFTER EXCEPTION CONDITION EC-PROGRAM-ARG-MISMATCH.
       ERR-PARA.
           MOVE FUNCTION EXCEPTION-STATUS TO EX
           DISPLAY "EC=" EX
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN-SECT SECTION.
       P0.
           MOVE "AABBCC" TO GT
           CALL WS-SIX USING GT
           CALL WS-SIX USING GT-ALIAS
           CALL WS-EIGHT USING GT
           CALL WS-EIGHT USING GT-ALIAS
           DISPLAY "END"
           STOP RUN.
       END PROGRAM PB5335M.
       >>TURN EC-PROGRAM-ARG-MISMATCH CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5335S6.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L6.
          05 L PIC X(3) OCCURS 2 TIMES.
       PROCEDURE DIVISION USING L6.
       S0.
           DISPLAY "T=" L6
           GOBACK.
       END PROGRAM PB5335S6.
       >>TURN EC-PROGRAM-ARG-MISMATCH CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5335S8.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L8.
          05 L PIC X(4) OCCURS 2 TIMES.
       PROCEDURE DIVISION USING L8.
       S0.
           DISPLAY "T=" L8
           GOBACK.
       END PROGRAM PB5335S8.
