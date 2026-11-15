      *>--- conformance-case
      *> id: pb5104_release_return_through_goto_85
      *> rules: [1985-SR-XI.4.2.3-2, 1985-SR-XI.4.3.3-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5104. §1985:XI.4.4.4 GR7: the range of the input procedure
      *>   includes all statements executed as the result of a transfer of
      *>   control by GO TO in the range. G1 (in IN-PROC) is a GO TO that
      *>   M1 ALTERs, BEFORE the SORT, to REL-IT, a paragraph written
      *>   outside IN-PROC; the RELEASE there executes in the range. The
      *>   output procedure reaches its RETURN (in HELPERS) by a plain
      *>   GO TO. Expected output: BBB. Fails if a GO TO is not followed,
      *>   or an ALTER outside the range is not (COBOL7081 at either).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5104GT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SORT-A ASSIGN TO P5104GT.
       DATA DIVISION.
       FILE SECTION.
       SD  SORT-A.
       01  REC-A.
           05  KEY-A PIC X(3).
       PROCEDURE DIVISION.
       MAIN-CTL SECTION.
       M1.
           ALTER G1 TO PROCEED TO REL-IT.
           SORT SORT-A ON ASCENDING KEY KEY-A
               INPUT PROCEDURE IS IN-PROC
               OUTPUT PROCEDURE IS OUT-PROC.
           STOP RUN.
       REL-IT.
           MOVE "BBB" TO REC-A.
           RELEASE REC-A.
           GO TO I-END.
       IN-PROC SECTION.
       G1.
           GO TO I-DEFAULT.
       I-DEFAULT.
           MOVE "AAA" TO REC-A.
           RELEASE REC-A.
       I-END.
           EXIT.
       OUT-PROC SECTION.
       O1.
           GO TO RET-IT.
       O9.
           EXIT.
       HELPERS SECTION.
       RET-IT.
           RETURN SORT-A AT END GO TO O9.
           DISPLAY KEY-A.
           GO TO RET-IT.
