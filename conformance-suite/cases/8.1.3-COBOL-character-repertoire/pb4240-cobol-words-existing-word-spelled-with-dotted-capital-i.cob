*>--- conformance-case
*> id: pb4240-cobol-words-existing-word-spelled-with-dotted-capital-i
*> rules: [GR-8.1.3.2-3, GR-8.1.3.2-4, SR-7.3.10.3-3]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB4240, review finding R2-1 - 7.3.10.3 SR3 (cite.py --check 7.3.10.3 "The content of literal-1, literal-3, and
*> literal-4 shall be a reserved word, a context-sensitive word, or an intrinsic function name" -> OK 3)): the existing
*> word is a keyword, matched on the basic letters alone (8.1.3.2 3) a); the Annex C fold of 4) governs user-defined words
*> only, owner decision 2026-10-31). DİSPLAY, written with U+0130, is no reserved word, so UNDEFINE names no word to
*> undefine. It fails when the literal is read by the fold, which makes it DISPLAY and accepts the directive.
       >>COBOL-WORDS UNDEFINE "DİSPLAY"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4240W.
       PROCEDURE DIVISION.
           STOP RUN.
