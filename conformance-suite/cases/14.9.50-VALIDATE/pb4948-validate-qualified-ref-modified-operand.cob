      *>--- conformance-case
      *> id: pb4948-validate-qualified-ref-modified-operand
      *> rules: [SR-14.9.50.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> A reference-modified identifier-1 written with a qualifier: VALIDATE WS-A OF WS-G (1:2). Expected: COBOL4777.
      *> ISO §2023:14.9.50.3 SR1 - "Identifier-1 shall not be reference-modified." The qualification absorbs the
      *> reference modifier in the parse tree (qualification : (OF | IN) cobolWord (subscriptPart | refModPart)*),
      *> so the screen must read both spellings (kb/Work PB4948); the unqualified spelling is
      *> pb3568-validate-ref-modified-operand. The statement is the only violation; the program is otherwise legal.
      *> VALIDATE exists from COBOL-2002; at --std 85 it is a user word, so no 85 refusal is named.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4948A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-G.
          05 WS-A PIC X(5) VALUE "ABCDE".
       PROCEDURE DIVISION.
       MAIN-P.
           VALIDATE WS-A OF WS-G (1:2).
           STOP RUN.
