# data/unicode — pinned Unicode / CLDR source data for the text-processing subsystems

Every file here is redistributed under the **Unicode License v3** (`LICENSE-UNICODE.txt`, © Unicode, Inc.), which
permits copying and redistribution of the data files with the notice. Nothing here is, or is derived from, ISO/IEC
14651 text or its Common Template Table.

These files are the ONLY inputs of three generators, each of which writes an embedded artifact plus a manifest whose
`inputs` block records the SHA-256 of every file it read (a unit test fails when data, artifact and manifest drift
apart — `CollationTableTests.Manifest_InputsAndOutput_MatchTheCommittedFiles`,
`GraphemeBreakerTests.Table_MatchesTheManifest_AndThePinnedInputs`, `CldrLocaleLoaderTests.Pack_Loads_AndEveryFileParses`):

| Generator | Reads | Writes |
|---|---|---|
| `scripts/collation/generate-collation-table.py` | `allkeys_CLDR.txt`, `allkeys.txt`, `UnicodeData.txt`, `PropList.txt`, `Blocks.txt`, `FractionalUCA.txt`, `PropertyValueAliases.txt` | `src/Cobol.Net.Runtime/Collation/Data/root-collation.bin` + `.manifest.json` (the derived collation table, format 2) |
| `scripts/collation/pack-cldr-collation.py` | `cldr/collation/*.xml`, `cldr/bcp47/collation.xml`, `cldr/supplemental/supplementalData.xml`, `cldr/RELEASE` | `src/Cobol.Net.Runtime/Collation/CLDR/Data/cldr-collation.zip` + `.manifest.json` (the CLDR collation pack) |
| `scripts/unicode/generate-grapheme-table.py` | `GraphemeBreakProperty.txt`, `emoji-data.txt`, `DerivedCoreProperties.txt` | `src/Cobol.Net.Runtime/Unicode/Segmentation/Data/grapheme-break.bin` + `.manifest.json` (the grapheme cluster property table) |

Regenerating is deliberate: fetch a newer pinned set (all of the SAME Unicode/CLDR release), run the three
generators, run the tests (including the full CLDR conformance files and the ICU cross-checks), and commit data +
artifacts + manifests together.

## The files

| File | Version | Retrieved (2026-08-18/19) from | Role |
|---|---|---|---|
| `allkeys_CLDR.txt` | CLDR **release-48-2** (UCA 17.0.0, UCD 17.0.0) | `https://raw.githubusercontent.com/unicode-org/cldr/release-48-2/common/uca/allkeys_CLDR.txt` | the CLDR ROOT collation weights (DUCET format) — the table's weights |
| `allkeys.txt` | UCA **17.0.0** (`allkeys-17.0.0.txt`) | `https://www.unicode.org/Public/UCA/latest/allkeys.txt` (latest = 17.0.0 on the retrieval date; the versioned directory `Public/UCA/17.0.0/` publishes the same file) | the DUCET of the same version — read only for its `@version` cross-check |
| `FractionalUCA.txt` | CLDR release-48-2 (UCA 17.0.0) | `https://raw.githubusercontent.com/unicode-org/cldr/release-48-2/common/uca/FractionalUCA.txt` | read ONLY for its `FDD1 xxxx … first primary` markers — the CLDR REORDERING GROUPS (space, punct, symbol, currency, digit, one per script) every explicit mapping is assigned to; no fractional weight is copied |
| `PropertyValueAliases.txt` | UCD 17.0.0 | `https://www.unicode.org/Public/17.0.0/ucd/PropertyValueAliases.txt` | the Script property's long → short (ISO 15924) names, so a group is named by the code a CLDR `[reorder]` uses ("Latn") |
| `UnicodeData.txt` | UCD **17.0.0** | `https://www.unicode.org/Public/17.0.0/ucd/UnicodeData.txt` | combining classes (non-starters), canonical decompositions (the runtime's own NFD), assigned ranges of the siniform blocks |
| `PropList.txt` | UCD 17.0.0 | `https://www.unicode.org/Public/17.0.0/ucd/PropList.txt` | `Unified_Ideograph` (UTS #10 Table 16 Han rows) |
| `Blocks.txt` | UCD 17.0.0 | `https://www.unicode.org/Public/17.0.0/ucd/Blocks.txt` | the block boundaries Table 16 names |
| `GraphemeBreakProperty.txt` | UCD 17.0.0 | `https://www.unicode.org/Public/17.0.0/ucd/auxiliary/GraphemeBreakProperty.txt` | the Grapheme_Cluster_Break property (UAX #29) |
| `emoji-data.txt` | UCD 17.0.0 (emoji 17.0) | `https://www.unicode.org/Public/17.0.0/ucd/emoji/emoji-data.txt` | `Extended_Pictographic` (the emoji ZWJ-sequence rule GB11) |
| `DerivedCoreProperties.txt` | UCD 17.0.0 | `https://www.unicode.org/Public/17.0.0/ucd/DerivedCoreProperties.txt` | `Indic_Conjunct_Break` (InCB — the conjunct rule GB9c) |
| `cldr/collation/*.xml` (135 files) | CLDR release-48-2 `common/collation/` — the whole directory | `https://raw.githubusercontent.com/unicode-org/cldr/release-48-2/common/collation/<name>.xml` | every locale's collation tailoring rules (LDML Part 5): the source of every locale's order (`Collation/CLDR/README.md`); `es.xml` / `fr.xml` / `en.xml` are also the rule sources of the shipped `.tailor` files |
| `cldr/bcp47/collation.xml` | CLDR release-48-2 | `https://raw.githubusercontent.com/unicode-org/cldr/release-48-2/common/bcp47/collation.xml` | the BCP 47 `-u-` collation keys (co/ka/kb/kc/kf/kh/kk/kn/kr/ks/kv) and the `co` type aliases (`phonebk` = phonebook …) |
| `cldr/supplemental/supplementalData.xml` | CLDR release-48-2 | `https://raw.githubusercontent.com/unicode-org/cldr/release-48-2/common/supplemental/supplementalData.xml` | read for its `<parentLocales>` (the general table's plain entries and the `component="collations"` table): the non-truncating parents of the collation fallback chain (nb → no, yue → zh_Hant …) |
| `cldr/RELEASE` | — | (written by hand) | the one-line CLDR release tag the pack manifest records |

Verification data that is NOT committed in full (2.3 MB each), from the same CLDR tag —
`common/uca/CollationTest_CLDR_NON_IGNORABLE_SHORT.txt` and `CollationTest_CLDR_SHIFTED_SHORT.txt`: deterministic
1-in-25 samples live in `tests/Cobol.Net.Tests.Unit/TestData/collation/`; the full files run when the environment
variable `COBOLNET_UCA_CONFORMANCE_DIR` names the directory holding them (`CollationConformanceTests`). The full
run of 2026-08-18: 206,298 + 227,809 lines, 0 violations. The full `GraphemeBreakTest.txt` 17.0.0
(`https://www.unicode.org/Public/17.0.0/ucd/auxiliary/GraphemeBreakTest.txt`, 126 KB) IS committed, under
`tests/Cobol.Net.Tests.Unit/TestData/segmentation/`, and runs always (766 cases, 0 failures).

The specifications the derivations follow: UTS #10 (Unicode Collation Algorithm) revision 53 for Unicode 17.0.0
(`https://www.unicode.org/reports/tr10/tr10-53.html`) — Table 16 for implicit weights, Table 12 for the shifted
variable weighting, S2.1.1–S2.1.3 for discontiguous contractions, S3/S4 for key formation and comparison; UTS #35
Part 5 (Collation) for the CLDR rule syntax, settings, logical reset positions and reordering groups; UAX #29
(Unicode Text Segmentation) for extended grapheme clusters; UAX #44 for the derived properties. All restated in the
code's own words — none of their text is copied.
