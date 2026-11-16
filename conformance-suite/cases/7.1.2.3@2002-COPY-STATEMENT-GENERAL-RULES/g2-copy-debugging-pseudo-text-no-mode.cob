      *>--- conformance-case
      *> id: g2-copy-debugging-pseudo-text-no-mode
      *> rules: [2002-SR-7.1.2.2-12, 2002-GR-7.1.2.3-10,
      *>     2002-GR-7.1.2.3-13]
      *> accept: [2002]
      *> files: [G2CRD1.cpy, G2CRD2.cpy, G2CRD3.cpy, G2CRD4.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> Compiled WITHOUT DEBUGGING MODE; its twin is the same program
      *> compiled the other way, and only the PAIR shows which lines the
      *> resultant text puts on debugging lines (a debugging line is
      *> compiled WITH DEBUGGING MODE and is a comment without it,
      *> §2002:6.2.7). The fixed D indicator and the clause are obsolete
      *> at 2002 (warning COBOL0903, which does not fail the case).
      *> §2002:7.1.2.2 SR12 - "Pseudo-text-1 and pseudo-text-2 may
      *> contain debugging lines": both forms below are ACCEPTED.
      *> COPY 1 - pseudo-text-1 =="P1" / D "Q1"== spans a D line.
      *>   §2002:7.1.2.3 GR10c "A debugging indicator shall be treated
      *>   as if it did not appear in the line. Text-words within a
      *>   debugging line participate in the matching": it matches the
      *>   library's "P1" "Q1" in either mode. S1 and T1 stand on that
      *>   D line physically, yet no case of GR13 puts them on a
      *>   debugging line: the D line begins inside pseudo-text-1,
      *>   before pseudo-text-2's opening delimiter, so it is not within
      *>   pseudo-text-2 (§2002:7.1.2.2 SR12 "A debugging line is
      *>   specified within pseudo-text if the debugging line begins
      *>   after the opening pseudo-text delimiter"); the first library
      *>   text-word replaced, "P1", is on no debugging line; and the
      *>   COPY statement does not begin on one -> S1T1Z1 in BOTH modes.
      *>   A matcher that dropped the D line's words would print S1Q1Z1
      *>   or no match P1Q1Z1.
      *> COPY 2 - pseudo-text-2 =="S2" / D "T2"==. §2002:7.1.2.3 GR13
      *>   "only those text-words that are specified on debugging lines
      *>   where the debugging line is within pseudo-text-2 appear on
      *>   debugging lines": T2 alone is on a debugging line ->
      *>   P2S2T2Z2 with the mode, P2S2Z2 without.
      *> COPY 3 - library text whose second line is a D line. GR13 "the
      *>   text-word being introduced appears on a debugging line in
      *>   library text" -> N3 and D3 with the mode, N3 alone without.
      *> COPY 4 - BY text over a library word on a D line. GR10c lets
      *>   the D line's "P4" match ==P4==; GR13 "When a text-word
      *>   specified in the BY phrase is introduced, it appears on a
      *>   debugging line if the first library text-word being replaced
      *>   is specified on a debugging line" -> S4 T4 on a debugging
      *>   line: A4S4T4Z4 with the mode, A4Z4 without.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CPY19.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. COMPUTER-1.
       OBJECT-COMPUTER. COMPUTER-1.
       PROCEDURE DIVISION.
       P1.
           COPY G2CRD1 REPLACING =="P1"
      D    "Q1"== BY =="S1" "T1"==.
           COPY G2CRD2 REPLACING =="Q2"== BY
           =="S2"
      D    "T2"==.
           COPY G2CRD3.
           COPY G2CRD4 REPLACING =="P4"== BY =="S4" "T4"==.
           STOP RUN.
