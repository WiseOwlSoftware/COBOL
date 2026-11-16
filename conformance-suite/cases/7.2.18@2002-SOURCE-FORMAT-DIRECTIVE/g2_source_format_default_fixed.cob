      *>--- conformance-case
      *> id: g2_source_format_default_fixed
      *> rules: [2002-GR-7.2.18.2-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.2.18.2 GR2 - "The default reference format of a
      *>   compilation group is fixed form." No SOURCE FORMAT directive
      *>   appears, so every line is read in 2002 fixed form (6.2):
      *>   the sequence number area holds any character (§2002:6.2.1)
      *>   and the indicator area holds the 2002 fixed indicators of
      *>   §2002:6.1.1 - '*' and '/' comment lines and the 'D' and 'd'
      *>   debugging indicators. A debugging line "has the
      *>   characteristics of a comment line if the WITH DEBUGGING MODE
      *>   clause is not specified" (§2002:6.2.7), so it is compiled
      *>   in G2SFD1 and is a comment in G2SFD1N. (Debugging lines are
      *>   obsolete at 2002, 6.1.2 NOTE: an obsolete warning only.)
      *> EXPECTED OUTPUT, DERIVED (why each leg can fail):
      *>   DEBUG-UPPER / DEBUG-LOWER - D and d in column 7 under
      *>     DEBUGGING MODE are debugging lines and compile; read as
      *>     free form the D would be a word and the lines would fail.
      *>   SEQ-AREA-OK - "A1B2C3" and the other labels in columns 1-6
      *>     are sequence numbers; the * and / lines are comments, so
      *>     COMMENT-STAR and COMMENT-SLANT never print.
      *>   N-MAIN - G2SFD1N has no DEBUGGING MODE: its D and d lines
      *>     are comments, so N-DEBUG-UPPER/LOWER never print.
ABCDEF IDENTIFICATION DIVISION.
ABCDEF PROGRAM-ID. G2SFD1.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. ANY-KIND WITH DEBUGGING MODE.
       PROCEDURE DIVISION.
       MAIN-PARA.
XYZ001*    DISPLAY "COMMENT-STAR"
XYZ002/    DISPLAY "COMMENT-SLANT"
SEQ003D    DISPLAY "DEBUG-UPPER"
SEQ004d    DISPLAY "DEBUG-LOWER"
A1B2C3     DISPLAY "SEQ-AREA-OK"
           CALL "G2SFD1N"
           STOP RUN.
       END PROGRAM G2SFD1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SFD1N.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. ANY-KIND.
       PROCEDURE DIVISION.
       MAIN-PARA.
      D    DISPLAY "N-DEBUG-UPPER"
      d    DISPLAY "N-DEBUG-LOWER"
           DISPLAY "N-MAIN"
           GOBACK.
       END PROGRAM G2SFD1N.
