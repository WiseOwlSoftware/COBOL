## The 1985 "no digit position" PICTURE refusal cites the paragraph that states it

Note PB5988 (1985 prose-requirement campaign). The composition validator's refusal of a character-string with no digit
position and no character position (COBOL1934, `PIC $`) cited §1985:VI.5.9.4 GR3a at `--std 85`. That rule defines a numeric
item's symbols and its 1 to 18 digit positions (cite.py: the quoted text is not in it). The rule the refusal enforces is the
prose of §1985:VI.5.9.6 ("At least one of the symbols 'A', 'X', 'Z', '9', or '*' or at least two occurrences of one of the
symbols '+', '-', or 'cs' must be present in a PICTURE character-string"; cite.py OK).

Re-probe on this build (`--std 85`, `PIC $`): the refusal reproduced and now reads "(ISO §1985:VI.5.9.6)". The fix is the
1985 argument of `Rule("SR12 a", ...)` in `PictureComposition.cs`. The sibling sweep found the other `GR3a` citations
(`EditionContext.cs`, 18 digit positions) correct: GR3a really says "must range from 1 to 18 inclusive".

Golden: `13.18.40-PICTURE/pb5988-picture-no-digit-position-cites-1985` with `wiseowl/diagnostics/...json` pinning
"(ISO §1985:VI.5.9.6)" at 85. Rows closed: none (the catalog holds no row for the VI.5.9.6 prose). Left unfixed and noted in
the report: the message's symbol list still names 'N' and '1' at 85.

Review round 1 (R1-1): the refusal message at --std 85 still listed the 2023 symbol set (N, 1, character-1). It now names the §1985:VI.5.9.6 set (A, X, Z, 9, *; +, -, cs) through Edition.Cite, the 2002+ text unchanged; the golden expect pins the 1985 list. The 2023 arm SR12 a became SR12 (cite.py rejects a sub-item a). PB5988 Sites no longer names PictureAnalyzer.cs (R1-2).
