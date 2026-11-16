      *>--- conformance-case
      *> id: g2-replace-debugging-line-no-mode
      *> rules: [2002-SR-7.1.3.2-1, 2002-SR-7.1.3.2-3,
      *>     2002-SR-7.1.3.2-5, 2002-SR-7.1.3.2-10]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> No WITH DEBUGGING MODE. §2002:6.2.3: the program-text area of
      *> a line whose indicator area holds a debugging indicator holds
      *> character-strings and separators, and §2002:6.2.7: "After all
      *> COPY and REPLACE statements have been processed, a debugging
      *> line has the characteristics of a comment line".
      *> L1 §2002:7.1.3.2 SR1 - "A REPLACE statement may be specified
      *> anywhere in source text or in library text" a character-string
      *> may appear: the D-line REPLACE ==AAA== BY ==BBB== is processed,
      *> so DISPLAY AAA becomes DISPLAY BBB: NEW (OLD if the D line is
      *> dropped before REPLACE processing).
      *> L2 §2002:7.1.3.2 SR3 - "Pseudo-text-1 shall contain one or more
      *> text-words": its only text-word CCC is on a D line, which
      *> §2002:7.1.3.2 SR10 permits ("Pseudo-text-1 and pseudo-text-2
      *> may contain debugging lines"), and §2002:7.1.3.3 GR8c6 "Text
      *> words within a debugging line participate in the matching":
      *> DISPLAY CCC becomes DISPLAY DDD: NEW2 (a refusal, or OLD2, if
      *> the D line is not read as part of pseudo-text-1).
      *> L3 §2002:7.1.3.2 SR5 - "Partial-word-1 shall consist of one
      *> text-word": the D-line REPLACE LEADING ==GG== BY ==HH== has a
      *> one-word partial-word-1 and is processed (SR1): DISPLAY GGX
      *> becomes DISPLAY HHX: NEW4 (OLD4 if not processed). Its refusal
      *> leg is g2-replace-debugging-line-partial-word-1-refused.
      *> Each format 1 REPLACE without ALSO cancels the one before it
      *> (§2002:7.1.3.3 GR7b); REPLACE OFF ends the last.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2RPL01.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. COMPUTER-1.
       OBJECT-COMPUTER. COMPUTER-1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 AAA PIC X(3) VALUE "OLD".
       01 BBB PIC X(3) VALUE "NEW".
       01 CCC PIC X(4) VALUE "OLD2".
       01 DDD PIC X(4) VALUE "NEW2".
       01 GGX PIC X(4) VALUE "OLD4".
       01 HHX PIC X(4) VALUE "NEW4".
       PROCEDURE DIVISION.
       P1.
           DISPLAY "MAIN".
      D    REPLACE ==AAA== BY ==BBB==.
           DISPLAY AAA.
           REPLACE ==
      D    CCC
           == BY ==DDD==.
           DISPLAY CCC.
      D    REPLACE LEADING ==GG== BY ==HH==.
           DISPLAY GGX.
           REPLACE OFF.
           DISPLAY "END".
           STOP RUN.
