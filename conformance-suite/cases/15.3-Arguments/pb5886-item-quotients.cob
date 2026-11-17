      *>--- conformance-case
      *> id: pb5886-item-quotients
      *> rules: [AR-15.3-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5886 - an integer argument the type 6 screen
      *> does not show to be always an integer is refused
      *> (COBOL1627) at every edition.
      *>
      *> §2023:15.3 AR6 (cite.py --check): "An arithmetic
      *> expression that will always result in an integer value
      *> or an integer data item shall be specified"; §1989:A.2.2
      *> AR4 (cite.py --check): "An arithmetic expression which
      *> will always result in an integer value must be specified".
      *> The screen fails closed (kb/Work PB5886): a quotient is an
      *> integer term only when both operands are literals whose
      *> quotient is an integer, a function term is judged by the
      *> function's own type, and a term it cannot show to be an
      *> integer is not one.
      *> I / I is a quotient by an item, no integer term: at I = 0
      *> it is a size error, not an integer.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5886H.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  I  PIC 9(4) VALUE 6.
       01  R  PIC S9(18).
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE R = FUNCTION MOD (I / I + 65, 7).
           STOP RUN.
