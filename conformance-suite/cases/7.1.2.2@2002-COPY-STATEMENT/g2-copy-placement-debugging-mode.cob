      *>--- conformance-case
      *> id: g2-copy-placement-debugging-mode
      *> rules: [2002-SR-7.1.2.2-1, 2002-SR-7.1.2.2-2,
      *>     2002-SR-7.1.2.2-5, 2002-SR-7.1.2.2-13,
      *>     2002-SR-7.1.2.2-14, 2002-SR-7.1.2.2-15]
      *> accept: [2002]
      *> files: [G2CPHEAD.cpy, G2CPMID.cpy, G2CPN.cpy, G2CPL.cpy,
      *>     G2CPL2.cpy, G2CPNEST.cpy, G2CPIN.cpy, G2CPPW.cpy,
      *>     G2CPPZ.cpy, G2CPA.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.1.2.2 SR1 - "A COPY statement may be specified anywhere
      *> in source text or in library text" that a character-string or a
      *> separator may appear: mid-statement (DISPLAY / COPY G2CPMID.
      *> prints MID), inside a parenthesis (prints 1), on a fixed D
      *> debugging line (FROMLIB), on a >>D floating debugging line
      *> (FROMLIT) and in library text (G2CPNEST holds COPY G2CPIN:
      *> OUTER-LIB then INNER-LIB). A 2002 debugging line carries
      *> character-strings, and §2002:6.2.7 turns it into a comment only
      *> "After all COPY and REPLACE statements have been processed";
      *> WITH DEBUGGING MODE compiles the copied text. A COPY left
      *> unprocessed on a D line, or a nested COPY not expanded, drops a
      *> line. §2002:7.1.2.2 SR2 - "A COPY statement shall be preceded
      *> by a space except when it is the first statement in a
      *> compilation group": the program opens with COPY G2CPHEAD (its
      *> IDENTIFICATION DIVISION and PROGRAM-ID), and "( COPY G2CPN. )"
      *> is preceded by a space. A refusal of either fails the compile.
      *> §2002:7.1.2.2 SR5 - "Literal-1 and literal-2 shall be
      *> alphanumeric literals": COPY "G2CPL2" on the >>D line names its
      *> library text by an alphanumeric literal (FROMLIT).
      *> §2002:7.1.2.2 SR13 - "Compiler directive lines shall not be
      *> specified within pseudo-text-1, pseudo-text-2, partial-word-1,
      *> or partial-word-2"; a >>D line is a debugging line, which SR12
      *> lets pseudo-text hold ("Pseudo-text-1 and pseudo-text-2 may
      *> contain debugging lines"), not a directive line. Pseudo-text-2
      *> below holds ">>D AAA. DISPLAY", which by §2002:7.1.2.3 GR13
      *> stays on a debugging line and compiles in debugging mode:
      *> G2CPA's DISPLAY AAA. becomes DISPLAY AAA. DISPLAY BBB. (OLD,
      *> NEW). Treated as a directive it is refused; dropped, only NEW
      *> prints. §2002:7.1.2.2 SR14 - "Partial-word-1 shall consist of
      *> one text-word" and SR15 - "Partial-word-2 shall consist of zero
      *> or one text-word": on D lines, LEADING ==XX== BY ==YY== turns
      *> XXITEM into YYITEM (PW1-NEW), and LEADING ==ZZ== BY == == (zero
      *> text-words) deletes the matched characters, §2002:7.1.2.3 GR10f
      *> "deleted when partial-word-2 consists of zero text-words":
      *> ZZW-ITEM becomes W-ITEM (PW2-NEW). An unreplaced word prints
      *> the OLD value.
       COPY G2CPHEAD.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. COMPUTER-1 WITH DEBUGGING MODE.
       OBJECT-COMPUTER. COMPUTER-1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC 9 VALUE 0.
       01 XXITEM PIC X(7) VALUE "PW1-OLD".
       01 YYITEM PIC X(7) VALUE "PW1-NEW".
       01 ZZW-ITEM PIC X(7) VALUE "PW2-OLD".
       01 W-ITEM PIC X(7) VALUE "PW2-NEW".
       01 AAA PIC X(3) VALUE "OLD".
       01 BBB PIC X(3) VALUE "NEW".
       PROCEDURE DIVISION.
       P1.
           DISPLAY
           COPY G2CPMID.
           COMPUTE X = ( COPY G2CPN. )
           DISPLAY X.
      D    COPY G2CPL.
       >>D COPY "G2CPL2".
           COPY G2CPNEST.
      D    COPY G2CPPW REPLACING LEADING ==XX== BY ==YY==.
      D    COPY G2CPPZ REPLACING LEADING ==ZZ== BY == ==.
           COPY G2CPA REPLACING ==AAA== BY ==
       >>D AAA. DISPLAY
           BBB==.
           STOP RUN.
