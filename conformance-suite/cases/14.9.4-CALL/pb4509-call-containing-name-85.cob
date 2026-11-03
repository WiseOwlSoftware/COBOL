      *>--- conformance-case
      *> id: pb4509-call-containing-name-85
      *> rules: [SR-14.9.4.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4509 - at COBOL-85 a nested program's CALL of its
      *> containing program BY NAME is refused. COBOL-85 has no
      *> program-prototype-names (ISO/IEC 1989:2002 introduced them,
      *> its 8.4.5.7; the 2023 8.4.6.8), so the word after CALL is
      *> identifier-1, and 14.9.4.3 SR1 (cite.py --check 14.9.4.3 ->
      *> OK 1)): "Identifier-1 shall be defined as an alphanumeric,
      *> national, or program-pointer data item". No data item PPNO
      *> exists, so the reference identifies nothing (8.4.2.1):
      *> COBOL1639 at 85. From 2002 the same CALL is Format 2 and is
      *> accepted (pb4509_call_containing_program_by_name). The
      *> compiler took it as a prototype CALL at 85 and compiled it
      *> clean.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PPNO.
       PROCEDURE DIVISION.
       MAIN.
           CALL "PPNI".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PPNI.
       PROCEDURE DIVISION.
       P.
           CALL PPNO.
           EXIT PROGRAM.
       END PROGRAM PPNI.
       END PROGRAM PPNO.
