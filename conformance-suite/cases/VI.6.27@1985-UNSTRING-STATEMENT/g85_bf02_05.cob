      *>--- conformance-case
      *> id: g85_bf02_05
      *> rules: [1985-GR-VI.6.27.4-8]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.27.4 GR8 - a figurative constant delimiter stands for
      *> ONE character: SPACE (A) and ZERO (D) split at every single
      *> occurrence, so two contiguous ones leave an empty (space-filled)
      *> field; with ALL, contiguous occurrences of literal-1 (figurative
      *> or not) count as one (B, C, E) and one occurrence is moved to the
      *> DELIMITER IN item (C: '*', E: '0', each space filled by the MOVE).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF0205.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-S1   PIC X(9) VALUE "AB  CD EF".
       01  WS-S2   PIC X(7) VALUE "AB***CD".
       01  WS-S3   PIC X(5) VALUE "10020".
       01  WS-R1   PIC X(3).
       01  WS-R2   PIC X(3).
       01  WS-R3   PIC X(3).
       01  WS-D1   PIC X(3).
       01  WS-D2   PIC X(3).
       01  WS-C1   PIC 9.
       01  WS-T    PIC 99.
       PROCEDURE DIVISION.
       MAIN-PARA.
           PERFORM CLEAR-PARA.
           UNSTRING WS-S1 DELIMITED BY SPACE
               INTO WS-R1 WS-R2 WS-R3 TALLYING IN WS-T.
           DISPLAY "A [" WS-R1 "][" WS-R2 "][" WS-R3 "] " WS-T.
           PERFORM CLEAR-PARA.
           UNSTRING WS-S1 DELIMITED BY ALL SPACE
               INTO WS-R1 WS-R2 WS-R3 TALLYING IN WS-T.
           DISPLAY "B [" WS-R1 "][" WS-R2 "][" WS-R3 "] " WS-T.
           PERFORM CLEAR-PARA.
           UNSTRING WS-S2 DELIMITED BY ALL "*"
               INTO WS-R1 DELIMITER IN WS-D1 COUNT IN WS-C1
                    WS-R2.
           DISPLAY "C [" WS-R1 "][" WS-D1 "]" WS-C1 "[" WS-R2 "]".
           PERFORM CLEAR-PARA.
           UNSTRING WS-S3 DELIMITED BY ZERO
               INTO WS-R1 WS-R2 WS-R3.
           DISPLAY "D [" WS-R1 "][" WS-R2 "][" WS-R3 "]".
           PERFORM CLEAR-PARA.
           UNSTRING WS-S3 DELIMITED BY ALL ZERO
               INTO WS-R1 DELIMITER IN WS-D1
                    WS-R2 DELIMITER IN WS-D2.
           DISPLAY "E [" WS-R1 "][" WS-D1 "][" WS-R2 "][" WS-D2 "]".
           STOP RUN.
       CLEAR-PARA.
           MOVE "***" TO WS-R1 WS-R2 WS-R3.
           MOVE "###" TO WS-D1 WS-D2.
           MOVE 0 TO WS-C1 WS-T.
