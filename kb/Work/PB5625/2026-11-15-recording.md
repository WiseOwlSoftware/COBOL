**2026-11-15, the recording of the adjudication (records worker, branch `adjudication-pb5625`): what was put on record, and the one check that failed.**

The adjudication beside this file (`2026-11-15-adjudication.md`, copied unchanged) decides readings A to D. This round recorded A, B and C and held D; the second round (`2026-11-15-recording-d.md`) records D after the supplement. Every citation below was run through `python scripts/spec/cite.py --check` in this session and answered OK, including the 31 the adjudication carries (its `§2002:8.1.2` folding sentence checked as `§2002:8.1.2.1 GR3`). All probes ran on the build of main 24372b7b17 at `E:/COBOL-wt/coderead85-tree-d` (`cobol.exe <file> --std 2002 -o <dll> --run`); sources and outputs are in `E:/COBOL-coord/scratch/my-20261115/pb5625-rec/`.

## Recorded

- **A** as `docs/conformance/3/D-PARAMETERIZED-UNEXPANDED.md`.
- **B** as `docs/conformance/3/D-USAGE-NATIONAL-CLASS-2002.md`.
- **C, checking not enabled,** as `docs/conformance/3/D-SET-INDEX-UNCHECKED.md`. **C, checking enabled,** is defect PB5674, which now claims rows `2002-GR-14.8.35.3-2` and `2002-GR-14.8.35.3-3`.
- **A's citation instance** (COBOL0859 cites §2023 rules at `--std 2002`) is filed with PB4950: `kb/Work/PB4950/2026-11-15-cobol0859-use-eo-parameterized.md`.

## A: the class case has the same gap in the 2002 text

§2002:9.3.12 says of a parameterized class what §2002:9.3.13 says of an interface: "When it is expanded by substituting specific class-names or interface-names as actual parameters, a class is created that functions as a non-parameterized class", and "When a parameterized class is specified in the REPOSITORY paragraph, a new class (an instance of a parameterized class) is created based on the specification of the parameterized class". §2002:12.2.7.3 GR1 grants the name in the same general words as GR7 ("Class-name-1 is the name of a class that may be used throughout the scope of the containing environment division"), and GR5 is again the only rule that supplies actual parameters ("If the EXPANDS phrase is specified in a class-specifier, a class class-name-1 is created from the parameterized class class-name-2"). So determination A covers both.

The compiler refuses the class case at `--std 2002` and `--std 2023` alike, but not through one message:

- `01 O USAGE OBJECT REFERENCE P18PC.` (P18PC a parameterized class listed in REPOSITORY without EXPANDS; `use_param_class.cob`): `error COBOL0813: data item 'O': USAGE OBJECT REFERENCE 'P18PC': 'P18PC' is a parameterized class or interface (its CLASS-ID / INTERFACE-ID paragraph has a USING clause), which may be specified only in the REPOSITORY paragraph — reference an expansion of it, declared with the EXPANDS phrase (ISO §2023:13.18.60.2/.4; ISO §2023:12.3.8.4 GR1/GR7)`, the same ground COBOL0859 gives the interface.
- `INVOKE P18PC "NEW" RETURNING O` (`use_param_class_invoke.cob`): `error COBOL0823: INVOKE: 'P18PC' is neither a resolvable data item nor a class this source element may reference (ISO §2023:14.9.23.2 — identifier-1 or class-name-1; §2023:8.4.6.4)`. The same program with `USING T` removed from the CLASS-ID (`use_class_invoke_control.cob`) compiles and prints `OK`, so the refusal is the parameterized ground, but the message does not say so: it calls a class the REPOSITORY lists "neither ... a class". That is a misleading diagnostic, filed as PB5676, not part of the determination.

## D: the Annex D check FAILED, so D was held in this round

The adjudication asks, before D is recorded, that every pair Annex D lists map as the annex lists it. The kept probe output (`E:/COBOL-coord/scratch/op-20261114/read-2002/out-answer/probes-a-15/case_annexd-results.txt`) covers eight characters, not the 600 pairs, so the check was run in full: `gen_annexd.py` generates a program from the 600 pairs parsed out of `specs/ISO_COBOL_2002.md` (`probes-a-15/annex.json`, key `d`) that applies LOWER-CASE to each uppercase member and UPPER-CASE to each lowercase member, and compares the run's `FUNCTION ORD` values with the annex (`annexd_all.cob`, output `annexd_all-2002.txt`, verdict `annexd_check-2002.txt`).

- **LOWER-CASE: 596 of 600 uppercase letters map to the annex's lowercase letter; 4 do not.** U+0130 (annex U+0069) returns U+0130; U+0256 (annex U+0189) returns U+0256; U+0131 (annex U+0069) returns U+0131; U+03C2 (annex U+03C3) returns U+03C2.
- **UPPER-CASE: 586 of 587 lowercase letters that the annex pairs with exactly one uppercase letter map back; 1 does not.** U+0189 (annex U+0256) returns U+0189. Thirteen pairs share their lowercase letter with another pair (six lowercase letters: U+0069 with U+0049, U+0130 and U+0131; U+01C6, U+01C9, U+01CC and U+01F3 each with two; U+03C3 with U+03A3 and U+03C2), so the annex gives those letters no single uppercase answer; they were not counted.

Rendered PDF page 724 of the 2002 standard prints both `(<U0130>,<U0069>)` and `(<U0256>,<U0189>)` (the latter after `(<U0189>,<U0256>)`, on the same page); the transcription has both, so the failures are not a transcription error. U+0131 and U+03C2 are §2002:D.2.2 "Additional case mappings", the two pairs 2023 deleted (D-CASE-MAPPING names them).

The adjudication says of this case: "If one does not, that pair is a defect and gets its own note; the decision above covers only letters the annex does not pair." No id was allocated for that note, and whether each failing pair is a defect (the 0256-to-0189 pair maps a lowercase letter to an uppercase one, and two are D.2.2 folding pairs) needs a judgment the recording does not make. D's determination and its survey are therefore not written; rows `2002-RV-15.45.3-4` and `2002-RV-15.78.3-4` stay with this note, which stays `half` until the lead files the pair note(s) and D is recorded.
