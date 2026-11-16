      *>--- conformance-case
      *> id: g2ev01_copy_operand_forms
      *> rules: [2002-GR-7.2.10.3-1]
      *> accept: [2002]
      *> files: [g2ev01cb.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.2.10.3 GR1 - "Any text words in text-1 or text-2 that
      *>   do not form a compiler directive line are subject to the
      *>   matching and replacing rules of the COPY statement and the
      *>   REPLACE statement." The 2002 COPY REPLACING operands include
      *>   literal-3, word-1 and text-1 (an identifier, §2002:7.1.2.2
      *>   SR9), each matched as pseudo-text of its own text-words
      *>   (§2002:7.1.2.3 GR9), and "A debugging indicator shall be
      *>   treated as if it did not appear in the line" (§2002:7.1.2.3
      *>   GR10c), so text-words on a debugging line in the EVALUATE
      *>   text are matched too.
      *> Support file: g2ev01cb.cpy holds >>EVALUATE 2 / >>WHEN 2 whose
      *>   text-1 uses "OLD-LIT3", OLDWD, OLDA OF OLDG and a D line with
      *>   "OLD-DLINE"; its WHEN OTHER text-2 is omitted (GR4).
      *> EXPECTED OUTPUT, DERIVED:
      *>   GR1-LIT3-OK    literal-3 "OLD-LIT3" BY literal-4 in text-1.
      *>   W1-OK          word-1 OLDWD BY word-2 NEWWD: the MOVE and the
      *>                  DISPLAY name NEWWD (OLDWD is not declared, so
      *>                  a missed replacement fails the compile).
      *>   TX-OK          text-1 OLDA OF OLDG BY text-2 NEWA OF NEWG.
      *>   GR1-DLINE-OK   the D line is compiled (WITH DEBUGGING MODE,
      *>                  §2002:6.2.7) and its literal was replaced.
      *> The WHEN OTHER text-2 ("OLD-LIT3" again) is omitted, so the
      *> literal appears once only.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2EV01.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. G2-COMPUTER WITH DEBUGGING MODE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NEWWD PIC X(5).
       01 NEWG.
          05 NEWA PIC X(5).
       PROCEDURE DIVISION.
       MAIN-PARA.
           COPY g2ev01cb REPLACING "OLD-LIT3" BY "GR1-LIT3-OK"
                                   OLDWD BY NEWWD
                                   OLDA OF OLDG BY NEWA OF NEWG
                                   "OLD-DLINE" BY "GR1-DLINE-OK".
           STOP RUN.
