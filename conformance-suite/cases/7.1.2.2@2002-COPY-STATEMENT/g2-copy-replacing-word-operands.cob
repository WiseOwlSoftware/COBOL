      *>--- conformance-case
      *> id: g2-copy-replacing-word-operands
      *> rules: [2002-SR-7.1.2.2-10, 2002-GR-7.1.2.3-8]
      *> accept: [2002]
      *> files: [G2CWA.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.1.2.2 SR10 - "Word-1 or word-2 may be any single COBOL
      *> word except 'COPY', the compiler directive indicator, the
      *> floating debug indicator, or the comment indicator": the form
      *> word-1 BY word-2 is GRANTED for any other COBOL word, reserved
      *> words included. The refusal of the excepted words is
      *> pb5085-replacing-directive-indicator; this case shows the
      *> granted form ACCEPTED and applied (§2002:7.1.2.3 GR8, GR9):
      *>   MOVE BY ADD - a reserved word (a verb, not a figurative
      *>     constant, so no reading as literal-3/literal-4 of SR4
      *>     arises) as word-1 and as word-2: MOVE 1 TO G2W-N becomes
      *>     ADD 1 TO G2W-N, and G2W-N (VALUE 5) -> N=6. Unreplaced,
      *>     N=1.
      *>   SHOWIT BY DISPLAY - a user word as word-1, a reserved word
      *>     as word-2: unreplaced, SHOWIT is no verb and the compile
      *>     fails.
      *>   XXW BY YYW - user word by user word: Y=[yw]; the text-word
      *>     XXW-2 is not the word XXW (§2002:7.1.2.3 GR10b, whole
      *>     text-words only) and keeps its value -> [x2].
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CPW10.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G2W-N PIC 9 VALUE 5.
       01 XXW PIC X(2) VALUE "xw".
       01 YYW PIC X(2) VALUE "yw".
       01 XXW-2 PIC X(2) VALUE "x2".
       PROCEDURE DIVISION.
       P1.
           COPY G2CWA REPLACING MOVE BY ADD
                SHOWIT BY DISPLAY XXW BY YYW.
           STOP RUN.
