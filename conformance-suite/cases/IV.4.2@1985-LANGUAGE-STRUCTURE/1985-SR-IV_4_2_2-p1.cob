      *>--- conformance-case
      *> id: 1985-SR-IV_4_2_2-p1
      *> rules: [1985-SR-IV.4.2.2-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.2.2 p1 (LANGUAGE STRUCTURE). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-02.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * s-02 probe: 1985-SR-IV.4.2.2-p1 character-strings: COBOL
      * words, a literal, a PICTURE character-string and a comment-
      * entry, each delimited by separators (§1985:IV.4.2.2 p1).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CHARSTR.
       AUTHOR. A COMMENT-ENTRY; "IS NOT, A LITERAL" 9(5)V99.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  NUM-1    PIC S9(3)V99, VALUE -12.5.
       01  EDT-1    PIC ZZ9.99-.
       01  TXT-1    PIC X(5); VALUE "AB,CD".
       PROCEDURE DIVISION.
       P0.
           MOVE NUM-1 TO EDT-1.
           DISPLAY EDT-1 " " TXT-1.
           STOP RUN.
