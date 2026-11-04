      *>--- conformance-case
      *> id: pb4509-cancel-own-name-85
      *> rules: [SR-14.9.5.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4509 - at COBOL-85 a CANCEL that names a program by
      *> a word that is no data item is refused, even when the word is
      *> the program's own name. 14.9.5.2's third operand alternative,
      *> program-prototype-name-1, is a 2002 introduction (ISO/IEC
      *> 1989:2002 8.4.5.7; the 2023 8.4.6.8), so at 85 the word is
      *> identifier-1, and 14.9.5.3 SR1 (cite.py --check 14.9.5.3 ->
      *> OK 1)): "Identifier-1 shall be defined as an alphanumeric or
      *> national data item". No data item CNS85 exists (8.4.2.1):
      *> COBOL1639 at 85. The compiler took the word as a program
      *> prototype at 85 and compiled the CANCEL clean.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CNS85.
       PROCEDURE DIVISION.
       MAIN.
           CANCEL CNS85.
           STOP RUN.
