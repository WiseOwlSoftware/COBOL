- **D-CASE-MAPPING — Case mappings (UPPER-CASE / LOWER-CASE, §2023:15.97.4 r4 / §2023:15.57.4)**: absent a locale, the case correspondence is
  **implementor-defined** (§2023:15.97.4 r4). WiseOwl COBOL uses the .NET invariant Unicode case tables. Because the mapping
  is delegated to the implementor, the enumerated 2023 annex changes (E.2 item 14 deletions of DOTLESS I
  `(0131,0069)` and GREEK FINAL SIGMA `(03C2,03C3)`; E.3.3 item 6 additions) are not separately tuned — the invariant
  mapping is the determination (a corner case; the general Latin/basic repertoire matches exactly). Since kb/Work
  PB64 T5 this is the ELSE-arm it is meant to be: a `LOCALE locale-name-1` phrase (r2) or a CHARACTER CLASSIFICATION
  locale in effect for the operand's class (r3) maps through THAT locale's LC_CTYPE instead — see §4 item 5.
  **The two letters the invariant table maps to themselves, at every edition:** U+0130 (LATIN CAPITAL LETTER I WITH DOT
  ABOVE) and U+0131 (LATIN SMALL LETTER DOTLESS I) have no invariant case pair, so LOWER-CASE and UPPER-CASE each return
  both unchanged (measured 2026-11-15 on main 24372b7b17 at `--std 2002` and `--std 2023`; U+00C4/U+00E4 and U+03A9/U+03C9
  map both ways in the same run). That is the "no corresponding letter" rule (§2002:15.45.3 RV4: "If there is no
  corresponding lowercase letter for a given uppercase letter, that letter is unchanged in the returned value";
  §2002:15.78.3 RV4 the same for uppercase), applied to the documented correspondence.
  **At `--std 2002`, with no locale in effect** (kb/Work PB5625 reading D, adjudicated 2026-11-15, with its supplement):
  2002 gives the source of the correspondence only for a locale (§2002:15.45.3 RV2: "the correspondence of uppercase to
  lowercase letters is determined from locale category LC_CTYPE") and states none otherwise, so the correspondence is
  implementor latitude, and WiseOwl COBOL uses the same .NET invariant simple case mappings. Annex D is normative but is
  not the functions' rule: it "shows the mapping of uppercase letters to lowercase letters in the COBOL character
  repertoire" (§2002:D), its use in the body is the equivalence of letters in COBOL words (§2002:8.1.2.1 GR3: "folding
  from uppercase to lowercase in accordance with the case mapping described in Annex D"; GR4c for extended letters), and
  no rule of 15.45 or 15.78 refers to it. The choice follows the annex's own advice to implementors (§2002:D.2.1:
  "Implementors should use the case mappings in UnicodeData.txt if additional characters are added to the set of
  extended letters as implementor extensions"), so the 65 uppercase and 104 lowercase Annex C letters that have a Unicode
  pair and no Annex D pair (counted by the 2002 read, kb/Work PB5625) are changed by design. Over all 600 pairs Annex D lists, LOWER-CASE maps 596 as listed and
  UPPER-CASE 586 of the 587 lowercase letters the annex pairs with one uppercase letter (measured 2026-11-15, the same
  build). None of the five exceptions is a defect of the functions: (U+0131, U+0069) and (U+03C2, U+03C3), the two
  "additional case mappings" of D.2.2, and the reversed (U+0256, U+0189) each have a LOWERCASE first member, which
  LOWER-CASE does not replace (§2002:15.45.3 RV1: the value has "any uppercase letters replaced by their corresponding
  lowercase letters"), and the reversed pair's second member U+0189 is uppercase, which UPPER-CASE leaves as it is; (U+0130, U+0069) is the invariant table's self-mapped letter above. **A defect in the 2002 text:**
  Annex D.2.1 prints the pair (U+0189, U+0256) and, on the same page, the reversed duplicate (U+0256, U+0189) (ISO/IEC
  1989:2002 PDF page 724, printed page 702, read 2026-11-15), which pairs a lowercase letter with an uppercase one; the
  functions apply (U+0189, U+0256) in the uppercase-to-lowercase direction (LOWER-CASE(U+0189) = U+0256 and
  UPPER-CASE(U+0256) = U+0189 in the same run) and the reversed entry is recorded here, not coded around. **Survey
  (CLAUDE.md rule 1):** GnuCOBOL 3.2.0 (measured 2026-11-15 in WSL Ubuntu, `cobc -x -std=default`, national items
  holding U+0130, U+0131, U+00C4, U+00E4, U+03A9, U+03C9, U+0041, U+0061, result bytes shown with HEX-OF) stores a
  national item as UTF-16 big-endian bytes ("handling of USAGE NATIONAL is unfinished", its own warning) and cases only
  the basic Latin letters: LOWER-CASE(U+0041) = U+0061 and UPPER-CASE(U+0061) = U+0041, while U+0130, U+0131, U+00C4,
  U+00E4, U+03A9 and U+03C9 all come back unchanged in both functions. Under `-std=cobol2002` the same program built
  with FUNCTION ORD in place of HEX-OF (HEX-OF is not a 2002 function there) printed the same unchanged first-byte
  ordinals as under `-std=default`. GnuCOBOL therefore states no correspondence beyond the basic letters for national
  data, and nothing below the ISO text decides the extended letters; the Unicode choice stands on Annex D's NOTE.
  IBM Enterprise COBOL and Micro Focus: unmeasured (neither is installed on this host).
