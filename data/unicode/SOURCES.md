# data/unicode — pinned Unicode / CLDR source data for the collation subsystem

Every file here is redistributed under the **Unicode License v3** (`LICENSE-UNICODE.txt`, © Unicode, Inc.), which
permits copying and redistribution of the data files with the notice. Nothing here is, or is derived from, ISO/IEC
14651 text or its Common Template Table.

These files are the ONLY inputs of `scripts/collation/generate-collation-table.py`, which produces the derived
collation table `src/Cobol.Net.Runtime/Collation/Data/root-collation.bin` (+ its `.manifest.json`, whose `inputs`
block records the SHA-256 of each file below; the unit test `CollationTableTests.Manifest_InputsAndOutput_MatchTheCommittedFiles`
fails when the three drift apart). Regenerating is deliberate: fetch a newer pinned set, run the generator, run the
collation tests (including the CLDR conformance samples), and commit data + table + manifest together.

| File | Version | Retrieved (2026-08-18) from | Role |
|---|---|---|---|
| `allkeys_CLDR.txt` | CLDR **release-48-2** (UCA 17.0.0, UCD 17.0.0) | `https://raw.githubusercontent.com/unicode-org/cldr/release-48-2/common/uca/allkeys_CLDR.txt` | the CLDR ROOT collation weights (DUCET format) — the table's weights |
| `allkeys.txt` | UCA **17.0.0** (`allkeys-17.0.0.txt`) | `https://www.unicode.org/Public/UCA/latest/allkeys.txt` (latest = 17.0.0 on the retrieval date; the versioned directory `Public/UCA/17.0.0/` publishes the same file) | the DUCET of the same version — read only for its `@version` cross-check |
| `UnicodeData.txt` | UCD **17.0.0** | `https://www.unicode.org/Public/17.0.0/ucd/UnicodeData.txt` | combining classes (non-starters), canonical decompositions (the runtime's own NFD), assigned ranges of the siniform blocks |
| `PropList.txt` | UCD 17.0.0 | `https://www.unicode.org/Public/17.0.0/ucd/PropList.txt` | `Unified_Ideograph` (UTS #10 Table 16 Han rows) |
| `Blocks.txt` | UCD 17.0.0 | `https://www.unicode.org/Public/17.0.0/ucd/Blocks.txt` | the block boundaries Table 16 names |
| `cldr-collation-es.xml` / `-fr.xml` / `-en.xml` | CLDR release-48-2 `common/collation/{es,fr,en}.xml` | `https://raw.githubusercontent.com/unicode-org/cldr/release-48-2/common/collation/…` | the rule sources of the shipped tailorings (`src/Cobol.Net.Runtime/Collation/Tailoring/*.tailor`): es = `&N<ñ<<<Ñ`; en, fr = the root order |

Verification data that is NOT committed in full (2.3 MB each), from the same CLDR tag —
`common/uca/CollationTest_CLDR_NON_IGNORABLE_SHORT.txt` and `CollationTest_CLDR_SHIFTED_SHORT.txt`: deterministic
1-in-25 samples live in `tests/Cobol.Net.Tests.Unit/TestData/collation/`; the full files run when the environment
variable `COBOLNET_UCA_CONFORMANCE_DIR` names the directory holding them (`CollationConformanceTests`). The full
run of 2026-08-18: 206,298 + 227,809 lines, 0 violations.

The specification the derivation follows is UTS #10 (Unicode Collation Algorithm) revision 53 for Unicode 17.0.0
(`https://www.unicode.org/reports/tr10/tr10-53.html`) — Table 16 for implicit weights, Table 12 for the shifted
variable weighting, S2.1.1–S2.1.3 for discontiguous contractions, S3/S4 for key formation and comparison.
