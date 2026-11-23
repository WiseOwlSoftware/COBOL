      *>--- conformance-case
      *> id: g2mov20_national_edited_sender_multi_receiver
      *> rules: [2002-GR-14.8.24.3-1,
      *>    GR-14.9.25.4-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5678. §2002:14.8.24.3 GR1 - "Literal-1 or the
      *>   content of the data item referenced by identifier-1 is
      *>   moved to the data item referenced by each identifier-2 in
      *>   the order specified." (cite.py: OK). The content of a
      *>   national-edited sender is the national characters it holds,
      *>   the inserted B blank included, and of an alphanumeric-edited
      *>   one the same. With two receivers the intermediate result
      *>   item holds the sender; one that edits it again stores
      *>   "C" for "C D" and "A" for "A B".
      *>   SNE PIC NBN holds N"C D"; SXE PIC XBX holds "A B".
      *>   1 SNE to RN RN2 (national, five positions): [C D  ] twice.
      *>   2 SXE to RX RX2: [A B  ] twice.
      *>   3 SNE to RN and to the national-edited RNE (PIC NBN),
      *>     whose own editing (an insertion B between its two
      *>     positions) is the one editing applied, once: the
      *>     receiver takes the first two characters of the content,
      *>     "C" and " ", with the B inserted between them: [C  ].
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2MOV20.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 SNE  PIC NBN  VALUE N"C D".
       01 SXE  PIC XBX  VALUE "A B".
       01 RN   PIC N(5).
       01 RN2  PIC N(5).
       01 RX   PIC X(5).
       01 RX2  PIC X(5).
       01 RNE  PIC NBN.
       PROCEDURE DIVISION.
       MAIN.
           MOVE SNE TO RN RN2
           DISPLAY "1 [" RN "][" RN2 "]"
           MOVE SXE TO RX RX2
           DISPLAY "2 [" RX "][" RX2 "]"
           MOVE SNE TO RN RNE
           DISPLAY "3 [" RN "][" RNE "]"
           STOP RUN.
