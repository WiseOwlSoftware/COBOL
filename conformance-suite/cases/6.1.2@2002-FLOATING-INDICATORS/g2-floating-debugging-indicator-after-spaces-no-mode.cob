*>--- conformance-case
*> id: g2-floating-debugging-indicator-after-spaces-no-mode
*> rules: [2002-SR-6.1.2.1-7]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
>>SOURCE FORMAT FREE
*> §2002:6.1.2.1 SR7 - "A floating debugging indicator may be preceded
*> on a line only by zero or more space characters", in free form, where
*> §2002:6.3.4 reads "a debugging indicator, optionally preceded by one or
*> more space characters". §2002:12.2.4.3 GR5 - without DEBUGGING MODE
*> "any debugging lines are compiled as if they were comment lines".
*> Column 1 (zero spaces) and an indented >>D each head a debugging line
*> whose DISPLAY is a comment: only BEFORE and AFTER print. A >>D not
*> read as a debugging indicator fails the compile; a debugging line
*> compiled without the mode prints D-ZERO or D-SPACES.
IDENTIFICATION DIVISION.
PROGRAM-ID. G2FDI02.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
SOURCE-COMPUTER. COMPUTER-1.
PROCEDURE DIVISION.
P1.
    DISPLAY "BEFORE".
>>D DISPLAY "D-ZERO".
          >>D DISPLAY "D-SPACES".
    DISPLAY "AFTER".
    STOP RUN.
