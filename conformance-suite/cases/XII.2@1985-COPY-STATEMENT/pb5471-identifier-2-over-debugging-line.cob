      *>--- conformance-case
      *> id: pb5471-identifier-2-over-debugging-line
      *> rules: [1985-GR-XII.2.4-11, 1985-GR-XII.2.4-9]
      *> accept: [85]
      *> files: [PB5471LB.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XII.2.4 GR11 (cite.py --check: OK): "the indicator area of
      *> the introduced line contains the same character as the line on
      *> which the text being replaced begins", and GR9: "only those text
      *> words that are specified on debugging lines where the debugging
      *> line is within pseudo-text-2 appear on debugging lines". The
      *> identifier-2 A1 OF B1 OF C2 is written over a source line and a
      *> debugging line of the COPY statement; it is not pseudo-text-2, and
      *> the replaced XX is on a source line (space indicator), so every
      *> line it introduces is a source line and its qualifiers stay in the
      *> program without WITH DEBUGGING MODE (kb/Work PB5471). Expected:
      *> the A1 of C2, [Z]. A compiler that keeps the D hides OF B1 OF C2
      *> and refuses the ambiguous A1 (COBOL1639).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5471C.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. X.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  C1.
           05  B1.
               10  A1 PIC X VALUE "A".
       01  C2.
           05  B1.
               10  A1 PIC X VALUE "Z".
       PROCEDURE DIVISION.
       P1.
           COPY PB5471LB REPLACING XX BY A1
      D        OF B1 OF C2.
           STOP RUN.
