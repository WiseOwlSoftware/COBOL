## A source or library text is never decoded with U+FFFD: one decoder, a Latin-1 reading with a warning, a refusal for a malformed marked file

**PB1092 and PB1397, wave 20261755 group A (the Annex A.1 documentation chain of the GAP campaign PB2911).**

**What the re-probe showed.** Both notes were documentation debts whose rows the PB1522 integrator had already written, so the work
was to verify them on the tree and finish what was left. `python scripts/spec/audit_annex_a1.py --check` reported no finding, and
`DOC-A.1-8`, `-43`, `-186` (PB1092) and `-26`, `-32`, `-44`, `-74`, `-95` (PB1397) were CONFORMS with witnesses. One defect remained and it
was real: `DOC-A.1-25` stated a disposition for source text that `CompilationInputs.ReadAllText` (`File.ReadAllText`) did not implement. A
file with no byte-order mark holding a raw 0xE9 in a literal gave `FUNCTION ORD` 65534 (U+FFFD plus one) with no diagnostic, and so did the
same byte after a UTF-8 mark. The determination had been written ahead of the code with a "not yet what the compiler does" caveat.

**The fix (ISO §8.1.3.2 GR1 and GR2; Annex A.1 items 25 and 26).** `SourceTextDecoder.Decode` is the one place the bytes of a source or
library text become characters:

- a byte-order mark names the encoding (UTF-8, UTF-16 LE/BE, UTF-32 LE/BE) and is not source text;
- a marked file whose content is not well-formed in that encoding is the error **COBOL5434**, naming the file and the byte offset of the first
  ill-formed sequence, with no fallback because the mark states the encoding;
- an unmarked file is UTF-8 or, when it is not well-formed UTF-8, ISO/IEC 8859-1 (every byte its own code point) with the warning **COBOL5435**.
  The precedence is ISO (silent, the implementor's latitude), then GnuCOBOL, which passes source bytes through unchanged: Latin-1 is the only
  byte-preserving decoding into a UTF-16 repertoire, and `FUNCTION ORD` of the byte 0xE9 is then 234.

`CompilationInputs.ReadAllText` is gone (every caller changed, no forwarder). `ReadSourceText(path, diagnostics)` reads, decodes and reports, once
per file per diagnostic bag, so a library text copied ten times is one finding. `Frontend.Parse` ends a compilation at a malformed main source;
`CopyProcessor.ResolveOneCopy` treats a malformed library text as an unresolved COPY after its single COBOL5434 and releases it from the include
set (the first draft left it in, and a second COPY of the same file drew a false "circular COPY" CBL3621; the unit test caught it). .NET reports a
lone UTF-16 high surrogate one code unit late, so the offset of a malformed UTF-16 file is found by walking the units rather than taken from the
exception. COBOL5436 of the allocation is returned unused.

**Sibling sweep.** Source text is read in exactly two places (`Frontend.Parse`, `CopyProcessor.ResolveOneCopy`), both changed;
`CompiledProgramCacheDriftTests.EveryAmbientReadInTheCompiler_IsRecorded` already forbids an ambient read anywhere else. The second reader of library text is the conformance-suite
generator (`conformance-suite/harness/cobol_source.py#decode`): it was strict UTF-8 and crashed on the new library texts. It only needs a
text's structure (COPY statements, DISPLAY operands), so it is now lenient by design and its self-test pins that. Eight inventory rows
named the retired `ReadAllText` as a code site (`DOC-A.1-25`, `-26`, `-31`, `-34`, `-38`, `GR-8.1.3.2-1`, `-2`, `-5`); all were re-sited. The
prose sentences naming it (DOC-A.1-26, DOC-A.1-34 in `docs/CONFORMANCE.md`; the notes of DOC-A.1-38 and GR-8.1.3.2-5) now name the decoder.
`PB3453`'s Sites paragraph follows the rename.

**Goldens.** `l1_encoding_unmarked_non_utf8_is_latin1` (a library text with raw bytes 41 E9 5A in a literal; expected `AéZ|234`, 233 + 1, the
previous decoder's 65534 cannot pass it) and `l1_encoding_marked_malformed_is_refused` (a UTF-8 mark and the same byte, refused with COBOL5434 at
all four editions, an entry in `conformance-suite/wiseowl/diagnostics.json`). `SourceTextDecoderTests` (26 cases) cover every mark, the offsets,
lone surrogates, the UTF-32 range, the empty file and the once-per-file reporting for main source and library text.

**Rows.** Batch `pb1092-pb1397-annex-a1-documentation` (11 records): PARTIAL to CONFORMS for `DOC-A.1-25`, `GR-8.1.3.2-1`, `GR-8.1.3.2-2`,
`GR-8.3.3.6.4-1` (its documentation half is `DOC-A.1-74`), `SR-12.3.7.3-20` (documentation half `DOC-A.1-43`) and `GR-12.3.7.4-7` (its k) 4. arm,
`DOC-A.1-186`); GAP 29 to 23. PB1092 closes `SR-12.3.7.3-20`, `GR-12.3.7.4-7`, `DOC-A.1-8`, `-43`, `-186`; PB1397 closes `GR-8.1.3.2-1`, `-2`,
`GR-8.3.3.6.4-1` and `DOC-A.1-25`. The code comments at `SwitchBindCurrency` and `ValidateCurrencyChar` now name the item-43 and item-44 rows beside
the code that makes the determination, so the key and its documentation change together.

**Self-review.** Architecture: one decoder, one reporter, the encodings held as static strict instances, no second copy of the disposition (the
harness reader is lenient on purpose and says so). Code: the diagnostics cite §8.1.3.2 GR1 and A.1 item 25 (checked with `cite.py`). Performance:
the file is read once as bytes; the strict UTF-8 attempt is the same single pass `File.ReadAllText` made. Duplication: the offset walk exists only
for UTF-16, where .NET's index is not exact.

**Gate.** Run `TestResults/build-local/000001-20261029T230436Z` on head b692a8fda: `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN` with `Linux half (WSL clone of HEAD) GREEN`; both new goldens ran (`l1_encoding_unmarked_non_utf8_is_latin1` at 2023, `l1_encoding_marked_malformed_is_refused` at 85, 2002, 2014 and 2023) and the 26 `SourceTextDecoderTests` cases passed. `scripts/semgrep/verify.py` PASS with no rule count raised.
