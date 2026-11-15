      *>--- conformance-case
      *> id: g2fce02-select-not-first
      *> rules: [2002-SR-12.3.4.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:12.3.4.2 SR1 - a file control entry that does not open
      *> with its SELECT clause is refused.
      *>   cite.py --check "§2002:12.3.4.2 SR1" "The SELECT clause shall be
      *>   specified first in the file control entry" -> OK
      *> The entry for F1 writes its PADDING CHARACTER clause (a 2002
      *> Format 3 clause) before SELECT; the same entry with SELECT first
      *> is accepted (g2fce01_clauses_any_order_with_padding), so the
      *> only defect is the clause order. Expected: the syntax
      *> diagnostic at the misplaced clause.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2FCE02.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           PADDING CHARACTER IS "*" SELECT F1
               ASSIGN TO "G2FCE2N.DAT".
       DATA DIVISION.
       FILE SECTION.
       FD  F1.
       01  R1          PIC X(4).
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT F1.
           CLOSE F1.
           STOP RUN.
