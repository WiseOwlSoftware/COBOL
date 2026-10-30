*>--- conformance-case
*> id: pb3245_free_comment_entry_is_commentary
*> rules: [GR-6.5-5]
*> accept: [85]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3245 review R1-1, docs/CONFORMANCE.md D-CE1 - a free-form
*>   comment-entry is commentary, as a fixed-form one is: the paragraph
*>   header stays program text and the rest of its line is discarded,
*>   so no character of the entry reaches the lexer. Each entry below
*>   is text the token rules would refuse as program text: an
*>   apostrophe in a name (an unclosed literal, COBOL5563), an unclosed
*>   quotation mark, a comma and a semicolon with no space after them
*>   (COBOL2631), a hexadecimal literal with non-hex digits (COBOL2630),
*>   a period inside the entry, and a paragraph word with no period.
*>   No edition has both free form and a comment-entry, so the entry's
*>   end is GnuCOBOL 3.2's -free reading (CLAUDE.md rule 1): the rest of
*>   the header line. Accepted at 85, where the paragraphs exist.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB3245CE.
AUTHOR. Pat O'Brien.
  INSTALLATION. Joe's "big program.
DATE-WRITTEN. Smith,J"r" X;Y.
DATE-COMPILED. X"ZZ" B"12".
SECURITY. J. Smith.
REMARKS no period after the paragraph word.
PROCEDURE DIVISION.
    DISPLAY "OK".
    STOP RUN.
