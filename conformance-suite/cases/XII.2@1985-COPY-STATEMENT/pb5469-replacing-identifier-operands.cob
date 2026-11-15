      *>--- conformance-case
      *> id: pb5469-replacing-identifier-operands
      *> rules: [1985-FMT-XII.2.2, 1985-GR-XII.2.4-3, 1985-GR-XII.2.4-4]
      *> accept: [85]
      *> files: [PB5469LB.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XII.2.2: REPLACING {pseudo-text-1 | identifier-1 | literal-1 |
      *> word-1} BY {pseudo-text-2 | identifier-2 | literal-2 | word-2}. An
      *> identifier is a data-name "followed by a syntactically correct
      *> combination of qualifiers, subscripts, or reference modifiers"
      *> (§1985:IV.4.3.8.5.1), or a function-identifier, FUNCTION
      *> function-name-1 with its arguments and a reference-modifier
      *> (§1985:IV.4.3.8.3.2). §1985:XII.2.4 GR4: "For purposes of matching,
      *> identifier-1, word-1, and literal-1 are treated as pseudo-text
      *> containing only identifier-1", and GR3 replaces each properly
      *> matched occurrence (cite.py --check: OK). So TBL (2) (1:1) matches
      *> only that whole sequence (TBL (2) alone is left), and the function
      *> identifiers match as written (kb/Work PB5469). Expected: TBL (3)
      *> (2:1) of AABBCD is D; TBL (2) is BB; LOWER-CASE of CD is cd; the
      *> literal-2 7 replaces FUNCTION LENGTH (X).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5469C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  TBL-G.
           05  TBL PIC XX OCCURS 3.
       01  X PIC X(3) VALUE "XYZ".
       PROCEDURE DIVISION.
       P1.
           MOVE "AABBCD" TO TBL-G.
           COPY PB5469LB REPLACING TBL (2) (1:1) BY TBL (3) (2:1)
                FUNCTION UPPER-CASE ("ab") BY FUNCTION LOWER-CASE ("CD")
                FUNCTION LENGTH (X) BY 7.
           STOP RUN.
