      *>--- conformance-case
      *> id: pb5307-cd-name-declared-twice-1985
      *> rules: [1985-SR-IV.4.3.8.1-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5307 - §1985:IV.4.3.8.1 SR1 (cite.py --check OK): "Every
      *> user-defined name explicitly referenced in a COBOL source program
      *> must be uniquely referenced". A cd-name has no qualifier, so two CD
      *> entries named XCD leave `SEND XCD` (and every statement naming it)
      *> without a reference that identifies one of them. The second CD entry
      *> is refused (COBOL4250); a compiler that lets the second replace the
      *> first, so that every statement uses the last CD, fails.
      *> Fails if: the program compiles.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5307CD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  MSG PIC X(10) VALUE "HELLO".
       COMMUNICATION SECTION.
       CD  XCD FOR OUTPUT
           TEXT LENGTH IS OLEN
           STATUS KEY IS OST.
       CD  XCD FOR OUTPUT
           TEXT LENGTH IS PLEN
           STATUS KEY IS PST.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 5 TO OLEN
           SEND XCD FROM MSG WITH EMI
           STOP RUN.
