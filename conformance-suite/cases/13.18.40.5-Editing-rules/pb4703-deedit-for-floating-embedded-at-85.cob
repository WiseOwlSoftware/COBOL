*>--- conformance-case
*> id: pb4703-deedit-for-floating-embedded-at-85
*> rules: [GR-13.18.40.5-6]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 13.18.40.2 Format 1 - the PICTURE EDITING phrase is a COBOL-2023 introduction
*> (Annex E.3.3 item 19), so below 2023 the version-conformance pass rejects it (COBOL0900). The
*> companion POSITIVE golden is pb4703_deedit_for_floating_embedded_landing, in this directory.
identification division.
program-id. PB4703NEG.
data division.
working-storage section.
01 fa   pic LLTLL9 editing L for negative is "-" positive is "+" editing T is ":".
01 n    pic s9(5) sign leading separate.
procedure division.
    move -555 to fa
    move fa to n
    display n
    stop run.
