      *> reject-at: 2002 2014 2023
      *> ISO §8.4.3.11.3 SR5 data-address-identifier — ADDRESS OF as a
      *>   receiving operand
      *> Rule: "This identifier format shall not be specified as a
      *>   receiving operand."
      *>   cite.py --check 8.4.3.11.3 "This identifier format shall not
      *>     be specified as a
      *>   receiving operand."  -> OK  §8.4.3.11.3 5)  (Syntax rules)
      *> INITIALIZE identifier-1 is a receiving operand, and a
      *>   pointer-class operand is
      *> otherwise admitted (a data-pointer is initialized to NULL):
      *>   cite.py --check 14.9.20.3 "The data item referenced by
      *>     identifier-1 is the
      *>   receiving operand."  -> OK  §14.9.20.3 7)  (Syntax rules)
      *>   cite.py --check 14.9.20.3 "Identifier-1 shall be strongly
      *>     typed or of class
      *>   alphabetic, alphanumeric, boolean, message-tag, national,
      *>     numeric, object, or
      *>   pointer."  -> OK  §14.9.20.3 1)  (Syntax rules)
      *> ADDRESS OF W is of class pointer, category data-pointer
      *>   (§8.4.3.11.4 GR1), so the
      *> ONLY rule the statement breaks is SR5. Expected: rejected with
      *>   the pointer/address
      *> operand band COBOLNET0869, whose catalog scope names §8.4.3.11.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C06H.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-W PIC X(4).
       PROCEDURE DIVISION.
       MAIN-P.
           INITIALIZE ADDRESS OF WS-W.
           STOP RUN.
