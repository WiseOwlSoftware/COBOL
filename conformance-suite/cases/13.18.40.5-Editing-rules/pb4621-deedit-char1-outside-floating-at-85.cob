*>--- conformance-case
*> id: pb4621-deedit-char1-outside-floating-at-85
*> rules: [GR-13.18.40.5-6]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 13.18.40.2 Format 1 - the PICTURE EDITING phrase is a COBOL-2023 introduction
*> (Annex E.3.3 item 19), so below 2023 the version-conformance pass rejects it (COBOL0900). The
*> companion POSITIVE golden is pb4621_deedit_char1_outside_floating_string, in this directory.
identification division.
program-id. PB4621NEG.
data division.
working-storage section.
01 lm-t   pic T--9 editing T is "-".
01 n      pic s9(5) sign leading separate.
procedure division.
    move 55 to lm-t
    move lm-t to n
    display n
    stop run.
