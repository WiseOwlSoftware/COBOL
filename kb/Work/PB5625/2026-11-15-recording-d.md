**2026-11-15, second round of the recording (records worker, branch `adjudication-pb5625`): reading D recorded after the supplement; PB5676 filed; PB5625 landed.**

The lead session adjudicated the five Annex D mappings the first round's check failed on (`2026-11-15-adjudication-d-supplement.md`, copied unchanged beside this file): none is a defect of the functions, so no per-pair notes are filed.

## The supplement's three citations

- `python scripts/spec/cite.py --check "§2002:15.45.3 RV1" "any uppercase letters replaced by their corresponding lowercase letters"` printed `OK  §2002:15.45.3 RV1  (Returned values)  the text is in RV1`.
- `... --check "§2002:8.1.2.1 GR4c" "An uppercase extended letter is treated as though it were folded to its corresponding lowercase extended letter in accordance with the case mapping described in Annex D"` printed `OK  §2002:8.1.2.1 GR4c  (General rules)  the text is in GR4c`.
- `... --check "§2002:15.45.3 RV4" "If there is no corresponding lowercase letter for a given uppercase letter, that letter is unchanged in the returned value"` printed `OK  §2002:15.45.3 RV4  (Returned values)  the text is in RV4`.

## The claim verified before recording

The supplement says the functions DO apply (U+0189, U+0256) in the uppercase-to-lowercase direction. The first round's 600-pair run shows it (`pairs_0189.py` over `annexd_all-2002.txt`):

```
#81 annex (U+0130,U+0069): LOWER-CASE(U+0130)=U+0130  UPPER-CASE(U+0069)=U+0049
#125 annex (U+0189,U+0256): LOWER-CASE(U+0189)=U+0256  UPPER-CASE(U+0256)=U+0189
#197 annex (U+0256,U+0189): LOWER-CASE(U+0256)=U+0256  UPPER-CASE(U+0189)=U+0189
#599 annex (U+0131,U+0069): LOWER-CASE(U+0131)=U+0131  UPPER-CASE(U+0069)=U+0049
```

A new probe, `case_dotted_i.cob` (national items U+0130, U+0131, U+00C4, U+00E4, U+03A9, U+03C9), gives the same answer at `--std 2002` and `--std 2023`. LOWER-CASE and UPPER-CASE each return U+0130 and U+0131 unchanged. U+00C4/U+00E4 and U+03A9/U+03C9 map both ways.

## Recorded

- `docs/conformance/3/D-CASE-MAPPING.md` is extended with:
  - the two self-mapped letters, for every edition;
  - 2002 with no locale in effect, with the decision's citations and the 596/600 and 586/587 counts;
  - the reversed duplicate (U+0256, U+0189), recorded as a defect in the 2002 text (PDF page 724, printed page 702);
  - the GnuCOBOL survey.
- GnuCOBOL 3.2.0 was measured in WSL with `cobc -x -std=default` (`case_dotted_i_gnucobol.cob`, `case_dotted_i_gnucobol_basic.cob`, result bytes by HEX-OF):
  - It stores national data as UTF-16BE bytes and warns that "handling of USAGE NATIONAL is unfinished".
  - It cases only U+0041 and U+0061; all six other letters come back unchanged.
  - Under `-std=cobol2002` HEX-OF is unknown. The ORD version of the program printed the same unchanged first-byte ordinals as under `-std=default`.
- IBM and Micro Focus are unmeasured.
- Lead 1 of the first round is filed as PB5676: COBOL0823's wording for INVOKE of an unexpanded parameterized class.
- PB5625 is `landed`, with `closes_rows: []`. Every row closes on its own 2002 witness.
