      *>--- conformance-case
      *> id: pb4677_mcs_transaction
      *> rules: []
      *> accept: [85, 2002]
      *> files: [mcs.json]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4677 - an input-output CD, ISO/IEC 1989:2002 A.1.2.3 GR24-GR34 and
      *> E.5.6: the positions of its area, its terminal's messages, Table A.1's SEND
      *> input-output-cd column (a disabled terminal takes no action) and GR31: after
      *> the first statement a changed SYMBOLIC TERMINAL makes each statement
      *> unsuccessful with the unknown status, 21 for RECEIVE and 20 for SEND.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4677TX.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W12 PIC X(12).
       01 T-B PIC X(5) VALUE "ABCDE".
       COMMUNICATION SECTION.
       CD CIO FOR I-O
           MESSAGE DATE IS MD
           MESSAGE TIME IS MT
           SYMBOLIC TERMINAL IS TN
           TEXT LENGTH IS TL
           END KEY IS EK
           STATUS KEY IS SK.
       CD CX I-O
           SYMBOLIC TERMINAL IS XN
           STATUS KEY IS XK.
       PROCEDURE DIVISION.
       MAIN.
           MOVE "TERM1" TO TN.
           MOVE SPACES TO W12.
           RECEIVE CIO MESSAGE INTO W12.
           DISPLAY "[" W12 "] " TL " " EK " " SK " " MD " " MT.
           RECEIVE CIO MESSAGE INTO W12
               NO DATA DISPLAY "NODATA".
           DISPLAY TL " " SK.
           MOVE 3 TO TL.
           SEND CIO FROM T-B WITH EMI.
           DISPLAY "SEND " SK.
           SEND CIO FROM T-B.
           DISPLAY "PORTION " SK.
           PURGE CIO.
           DISPLAY "PURGE " SK.
           PURGE CIO.
           DISPLAY "PURGE " SK.
           MOVE "TERM2" TO TN.
           RECEIVE CIO MESSAGE INTO W12
               NO DATA DISPLAY "NODATA".
           DISPLAY "CHANGED " SK.
           SEND CIO FROM T-B WITH EMI.
           DISPLAY "CHANGED " SK.
           DISABLE I-O TERMINAL CIO.
           DISPLAY "CHANGED " SK.
           MOVE "TERM1" TO TN.
           DISABLE I-O TERMINAL CIO.
           DISPLAY "DISABLE " SK.
           DISABLE I-O TERMINAL CIO.
           DISPLAY "DISABLE " SK.
           SEND CIO FROM T-B WITH EMI.
           DISPLAY "DISABLED " SK.
           ENABLE I-O TERMINAL CIO.
           DISPLAY "ENABLE " SK.
           MOVE "NOPE" TO XN.
           ENABLE I-O TERMINAL CX.
           DISPLAY "UNKNOWN " XK.
           STOP RUN.
