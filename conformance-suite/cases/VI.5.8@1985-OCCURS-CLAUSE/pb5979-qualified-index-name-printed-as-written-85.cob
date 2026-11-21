      *>--- conformance-case
      *> id: pb5979-qualified-index-name-printed-as-written-85
      *> rules: [1985-SR-VI.5.8.3-13]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5979. A refusal at 85. §1985:VI.5.8.3 SR13 (cite.py:
      *> OK): the index-name, "not being data, cannot be associated with
      *> any data hierarchy", is no identifier, so MOVE X OF T TO Y
      *> (X an index-name) is refused. The refusal names the operand
      *> as the programmer wrote it, 'X OF T' with its words apart,
      *> never the run-together 'XOFT' that a parse node's token text
      *> prints, a name that is not in the program. A compiler whose
      *> refusal prints the glued text fails.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5979A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
          02 E PIC X OCCURS 3 INDEXED BY X.
       01 Y PIC 9.
       PROCEDURE DIVISION.
       P-1.
           MOVE X OF T TO Y
           STOP RUN.
