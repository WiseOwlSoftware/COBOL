## PB547: a character with no one-byte image is read as `?` with EC-DATA-CONVERSION at every in-memory byte channel, never truncated to its low byte

An alphanumeric item holds any UTF-16 code unit in memory (DOC-A.1-31, -33), one byte per position at a byte channel
(DOC-A.1-209, ISO/IEC 1989:2023 §8.1.2 1)). The file arm of PB547 had already been settled by owner decision R47 (PB690: a WRITE
of such a record is refused with I-O status '91'); the arm left was the REDEFINES storage image.

**Re-probe.** On this build `MOVE "€ABC" TO A1` (PIC X(4)) with `05 N1 PIC N(2)` redefining it read U+AC41 U+4243 through N1; the
round trip `MOVE N1 TO SAVEN`, `MOVE SAVEN TO N1` left A1 = "¬ABC" (the euro's high byte dropped), and
`FUNCTION EXCEPTION-STATUS` was blank under checking. The same truncation was in the bit view, the binary, packed and
floating-point decoders (`CobolNum.Image`) and the whole-image national decode.

**Root.** `StorageByte.ToByte` was `(byte)c`, the one funnel every in-memory byte channel reduced a character through. The owner's
decided disposition for a character with no byte image (substitute `?` and set EC-DATA-CONVERSION, DOC-A.1-33) lived in a second,
private place, `CobolIntrinsics.ByteSub`, reached only by CONVERT: the same decision with two arms and one implemented.

**Fix.** The law lives once in `StorageByte`: `ToByte` is pure and total (a character with a byte image has it, any other is `?`),
and `ImageByte` is the run-time storage-image reader (the same byte, plus EC-DATA-CONVERSION through
`ExceptionState.DataConversionError`). `CobolBits` (bit and national windows, `NatFromStorage`), the three `CobolNum.Image`
decoders and CONVERT's `RawBytes` all call `ImageByte`; `ByteSub` is deleted. The packed class test reads the same law without the
condition so the test and the decode agree. The binder gained one `AmbientRaises` row, EC-DATA-CONVERSION, because the read renders
inline into any verb: without it the substitution happened and the condition was raised into a closed checking gate (measured on
the first build: `?ABC` stored, `EXCEPTION-STATUS` still blank). `StorageByteLawDriftTests` now also refuses a `(byte)` cast of an
indexed character in the runtime and compiler, so the next image reader is routed through the law.

**Goldens.** `pb547_redefines_national_view_of_wide_character` (controls: "ABCD" and HIGH-VALUES raise nothing; the euro reads
U+3F41 U+4243, raises EC-DATA-CONVERSION, and the write back stores "?ABC") and
`pb547_redefines_binary_and_bit_views_of_wide_character` (binary 1061241411 = 0x3F414243 and the bit string 0011 1111 ...),
accepted at 2002, 2014 and 2023 and run at each; `StorageImageByteTests` pins the law over all 65,536 code units and every
reader's condition, including that checking off records nothing.

**Rows.** GR-8.1.2-1 (DIVERGES), GR-13.18.60.4-7 (PARTIAL) and DOC-A.1-46 (DIVERGES) are CONFORMS (GAP 50 to 47); DOC-A.1-31, -33,
RV-15.19.4-2 and RV-15.66.4-3 only re-site the deleted `ByteSub` witness. docs/CONFORMANCE.md row -46 no longer claims the file
channel substitutes `?` (it refuses with '91').

**Self-review.** Architecture: one law, two members with named roles, no wrapper left. Code: sibling arms swept with a cast
grep (zero left), `LineSequentialCharacterSet` keeps the pure `ToByte` after its own `HasCharacterWithoutByteImage` test, and the
compile-time `InitialStateBackground` caller keeps the pure form (it must not raise at compile time; for an OPTIONS INITIALIZE
literal fill above U+00FF it now images `?` instead of the truncated byte). Performance: the byte-image case is one comparison.

**Gate.** `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN` (run 000001-20261025T005721Z: Conformance 8,681/18,400,
Unit 36,741/38,143, Characterization 36/36; Linux half (WSL clone of HEAD) GREEN; the six pb547 conformance runs, three editions
by two cases, Passed in leg-1-Conformance.trx) and `=== LINUX GATE: GREEN (legs selftests unit conformance selftests-built
inert-watch; HEAD 758e3c99b)`.
