*>--- conformance-case
*> id: l1_encoding_unmarked_non_utf8_is_latin1
*> rules: [DOC-A.1-25]
*> accept: [2023]
*> files: [L1LAT1.cpy]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §8.1.3.2 GR1 (Annex A.1 item 25) - the encoding of the COBOL
      *> character repertoire is the implementor's to define and document.
      *>   cite.py --check 8.1.3.2 "The implementor shall define a
      *>     mapping of each basic letter, basic digit, basic special"
      *>     -> OK 8.1.3.2 1)
      *>   cite.py --check A.1 "25)" -> OK A.1 25)
      *> THE DETERMINATION PINNED (docs/CONFORMANCE.md DOC-A.1-25): a
      *> file with NO byte-order mark is read as UTF-8, or - when it is
      *> not well-formed UTF-8 - as ISO/IEC 8859-1, where every byte is
      *> the character with that code point; the compiler warns
      *> (COBOL5435) and NO byte is replaced by U+FFFD.
      *> THIS FILE is UTF-8 with no mark. Its library text L1LAT1.cpy is
      *> NOT: it holds the bytes 41 E9 5A inside the literal of
      *>   01 VLAT PIC X(3) VALUE "A?Z".
      *> where ? stands for the single byte E9: it begins a three-byte
      *> UTF-8 sequence and 5A is no continuation byte, so the file is
      *> not well-formed UTF-8.
      *> DERIVATION of the one .out line: ISO/IEC 8859-1 maps the byte
      *> E9 to U+00E9 (e acute), so VLAT is "A" U+00E9 "Z" and
      *> FUNCTION ORD of its second position is 234 (the ordinal is the
      *> code point plus one, DOC-A.1-8: 233 + 1). The reading the
      *> previous decoder made, U+FFFD, would give 65534, and a reading
      *> that dropped the byte would leave "AZ" and a blank third
      *> position. DISPLAY separates the two values with "|".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1LAT101.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       COPY L1LAT1.
       01 N PIC 9(3).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE FUNCTION ORD(VLAT(2:1)) TO N
           DISPLAY VLAT "|" N
           STOP RUN.
