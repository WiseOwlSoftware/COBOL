*>--- conformance-case
*> id: pb4514-deedit-embedded-char1-at-85
*> rules: [GR-13.18.40.5-6]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 13.18.40.2 Format 1 - the PICTURE EDITING phrase is a COBOL-2023 introduction
*> (Annex E.3.3 item 19), so below 2023 the version-conformance pass rejects it (COBOL0900). The
*> companion POSITIVE golden is 2023/pb4514_deedit_embedded_char1_sign.
identification division.
program-id. PB4514NEG.
data division.
working-storage section.
01 em-t   pic --T--9 editing T is ":".
01 n      pic s9(5) sign leading separate.
procedure division.
    move -555 to em-t
    move em-t to n
    display n
    stop run.
