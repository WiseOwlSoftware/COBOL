      *>--- conformance-case
      *> id: g2-replace-result-literal-and-debugging-line
      *> rules: [2002-GR-7.1.3.3-9, 2002-SR-7.1.3.2-10]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.1.3.3 GR9 "The text produced as a result of processing
      *> a REPLACE statement shall not contain a COPY statement, a
      *> REPLACE statement, a SOURCE FORMAT directive, a comment, or a
      *> blank line" - the accepted side of the prohibition (its
      *> refusals at 2002: cases/7.2.4-REPLACE/pb1356-replace-produces-
      *> replace and pb1356-replace-produces-comment).
      *> LEG 1 - a literal is one text-word (§2002:7.1.1.4), so a
      *> pseudo-text-2 literal holding the characters of a COPY
      *> statement, a REPLACE statement and a comment produces none of
      *> them: accepted, DISPLAY prints the literal unchanged. A
      *> compiler that scanned literal content would refuse or cut it.
      *> LEG 2 - §2002:7.1.3.2 SR10 "Pseudo-text-1 and pseudo-text-2
      *> may contain debugging lines", without WITH DEBUGGING MODE.
      *> §2002:6.2.7: a debugging line is like a comment line only
      *> "After all COPY and REPLACE statements have been processed",
      *> so the produced text holds no comment and GR9 does not refuse
      *> it. Its D line holds only CONTINUE, so the output (R1, Z) is
      *> the same wherever the D-line text is placed (§2002:H.2 calls
      *> that placement ambiguous). Treating the D line as a comment
      *> at REPLACE time refuses the program under GR9.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2RPL08.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. COMPUTER-1.
       PROCEDURE DIVISION.
       P1.
           REPLACE ==TAGA== BY =="COPY X. REPLACE OFF. *> NOT A NOTE"==
                   ==TAGB== BY ==DISPLAY "R1"
      D                          CONTINUE==.
           DISPLAY TAGA.
           TAGB
           DISPLAY "Z".
           STOP RUN.
