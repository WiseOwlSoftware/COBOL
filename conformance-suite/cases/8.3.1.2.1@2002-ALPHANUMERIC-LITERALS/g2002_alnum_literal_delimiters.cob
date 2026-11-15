      *>--- conformance-case
      *> id: g2002_alnum_literal_delimiters
      *> rules: [2002-SR-8.3.1.2.1.2-3, 2002-GR-8.3.1.2.1.3-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:8.3.1.2.1.2 SR3 - "Two contiguous quotation symbol
      *>   characters matching the quotation symbol used in the
      *>   opening delimiter represent a single occurrence of that
      *>   quotation symbol character in the content of the literal."
      *>   Lines Q1-Q4: a doubled quote in a quote-delimited literal,
      *>   and a doubled apostrophe in an apostrophe-delimited one,
      *>   is ONE character: "A""B" is A"B (length 3), 'C''D' is C'D
      *>   (3), """" is " (1), '''' is ' (1). Lines Q5-Q6: only the
      *>   MATCHING symbol pairs; the other symbol is ordinary
      *>   content, so 'G""H' keeps both quotes (G""H, 4) and "E''F"
      *>   both apostrophes (E''F, 4). Q1-Q4 fail if a pair is kept
      *>   as two characters or ends the literal; Q5-Q6 fail if the
      *>   non-matching symbol is collapsed too.
      *> §2002:8.3.1.2.1.3 GR1 - "The separators that delimit the
      *>   alphanumeric literal are not included in the value of the
      *>   alphanumeric literal." Lines S1-S3: "ABC" and 'ABC' are ABC
      *>   of length 3, not 5; format 2 X"414243" is three characters
      *>   (the X" and " are not content; two hex digits per
      *>   character is the implementor's, DOC-A.1-9, so it is ABC),
      *>   and a VALUE "AB" in PIC X(4) holds AB and two spaces (S4).
      *>   Each fails if a delimiter is counted or stored.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2ALIT01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  L   PIC 9(3).
       01  W4  PIC X(4) VALUE "AB".
       PROCEDURE DIVISION.
           MOVE FUNCTION LENGTH("A""B") TO L.
           DISPLAY "Q1 [" "A""B" "] " L.
           MOVE FUNCTION LENGTH('C''D') TO L.
           DISPLAY "Q2 [" 'C''D' "] " L.
           MOVE FUNCTION LENGTH("""") TO L.
           DISPLAY "Q3 [" """" "] " L.
           MOVE FUNCTION LENGTH('''') TO L.
           DISPLAY "Q4 [" '''' "] " L.
           MOVE FUNCTION LENGTH('G""H') TO L.
           DISPLAY "Q5 [" 'G""H' "] " L.
           MOVE FUNCTION LENGTH("E''F") TO L.
           DISPLAY "Q6 [" "E''F" "] " L.
           MOVE FUNCTION LENGTH("ABC") TO L.
           DISPLAY "S1 [" "ABC" "] " L.
           MOVE FUNCTION LENGTH('ABC') TO L.
           DISPLAY "S2 [" 'ABC' "] " L.
           MOVE FUNCTION LENGTH(X"414243") TO L.
           DISPLAY "S3 [" X"414243" "] " L.
           DISPLAY "S4 [" W4 "]".
           STOP RUN.
