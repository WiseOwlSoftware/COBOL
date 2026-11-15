      *>--- conformance-case
      *> id: pb5104_release_return_in_performed_range_85
      *> rules: [1985-SR-XI.4.2.3-2, 1985-SR-XI.4.3.3-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5104. The range of an input or output procedure
      *>   "includes all statements that are executed as the result of
      *>   a transfer of control by CALL, EXIT, GO TO, and PERFORM
      *>   statements in the range" (§1985:XI.4.4.4 GR7, GR10), so a
      *>   RELEASE in a paragraph the input procedure PERFORMs and a
      *>   RETURN in a paragraph its output procedure PERFORMs are
      *>   within the range of the SORT that names SORT-A, wherever
      *>   the paragraphs are written. Expected output: the sorted
      *>   records AAA BBB CCC. Fails if the rule is asked of the
      *>   text of the procedure only (a refusal of REL-IT or RET-IT).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5104PF.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SORT-A ASSIGN TO P5104WC.
       DATA DIVISION.
       FILE SECTION.
       SD  SORT-A.
       01  REC-A.
           05  KEY-A PIC X(3).
       WORKING-STORAGE SECTION.
       01  MORE PIC X VALUE "Y".
       PROCEDURE DIVISION.
       MAIN-CTL SECTION.
       M1.
           SORT SORT-A ON ASCENDING KEY KEY-A
               INPUT PROCEDURE IS IN-PROC
               OUTPUT PROCEDURE IS OUT-PROC.
           STOP RUN.
       IN-PROC SECTION.
       I1.
           MOVE "CCC" TO REC-A.
           PERFORM REL-IT.
           MOVE "AAA" TO REC-A.
           PERFORM REL-IT.
           MOVE "BBB" TO REC-A.
           PERFORM REL-IT.
       OUT-PROC SECTION.
       O1.
           PERFORM RET-IT UNTIL MORE = "N".
       HELPERS SECTION.
       REL-IT.
           RELEASE REC-A.
       RET-IT.
           RETURN SORT-A AT END MOVE "N" TO MORE
               NOT AT END DISPLAY KEY-A.
