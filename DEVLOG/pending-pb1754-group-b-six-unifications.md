## PB1754 group B: six unifications (record layout advance, collation carrier names, NOT-phrase renderer, invariant emit culture, SR19 verdict, DataBinder seed)

Wave 20262133 group B took eleven R2 notes of the PB1754 architecture review. Six landed here. The other five (PB3236,
PB3252, PB3235, PB2620, PB2628) are each a mechanism of their own with a wider file set, and go to a successor.

**PB3256: one per-child physical advance.** `RecordLayout` wrote the bytes a child adds to its parent's running position
three times: the width sum and two offset walks. The width copy had no redefiner arm. `ExactPhysicalAdvance` is now the
one decision, and `PhysicalAdvanceDriftTests` pins the behaviour and refuses a second tier test on a child. The oracle
showed the copies really disagreed in a compiled program. In `pb5729_fd_later_record_redefines` (all four editions),
record `RC` holds `CR REDEFINES CA` and `CB REDEFINES CA` inside an FD area class, so the inner class dissolves. The file
was registered with a 12-byte maximum record for a 4-byte record area (§2023:13.18.44.4 GR1). It is now registered at 4.

**PB3215: the program collating carriers spelled once.** Eleven string literals spelled `__COLLATE` / `__COLLATE_NAT`
beside the one declaration. `ObjectComputerEmit.ProgramCollation` and `NationalProgramCollation` are now the only spelling,
and `CollationCarrierNameDriftTests` refuses another. The binder half had already moved to emit under PB5932.

**PB3238: one conditional-phrase / NOT-phrase renderer.** SIZE ERROR, OVERFLOW, message-control and CALL ON EXCEPTION,
RETURN's NOT AT END and the I-O NOT phrases each wrote the pair by hand. `ConditionalPhrase.EmitPair` and `EmitNot` now
render it from a `NotPhraseCondition`: the success test, whether it is the complement, the §2023:14.6.13.1.4 GR3 gate and
a comment. The I-O adapters still take the hook whole (PB3237), and `ConditionalPhraseDriftTests` refuses a NOT body
emitted anywhere else. The emitted text changed in 65 oracle cases, with the same behaviour. A CALL with ON and NOT ON
EXCEPTION renders its NOT arm as the `else` / `else if` of the ON arm, which moves `#line` mappings. A message-control
statement with both phrases and enabled checking loses a `handled` local that nothing ever set.

**PB3239: the backend emits under the invariant culture.** Measured with the scope removed: under a culture whose negative
sign is U+2212, a report with an unconditional rolled SUM total failed to compile, with `CS1056: Unexpected character '−'`
at four sites. `CompilerDriver.Compile` now runs `backend.Emit` inside `InvariantCultureScope`, and `StrongTypeModel`'s
record text uses the same scope. The four culture-sensitive `ToString()` calls name the invariant culture.
`EmittedTextCultureTests` pins the generated source byte-identical under both cultures.

**PB2625: one SR19 data-pointer verdict.** `StrongTypeModel.DataPointerStore` decides §2023:14.9.39.3 SR19 over a
(receiver, sender) pair. SET Format 7 words each fault as before, and the data-address-identifier argument lane
(§2023:14.8.2.3.2) refuses any fault. The SetBinder copy had already gone under PB3966. `DataPointerStoreDriftTests` pins
the verdict matrix against both wordings of the rule.

**PB2626: one DataBinder seed.** `BindSession.NewDataBinder` seeds every source element's binder with the session's facts.
`SourceElementFacts` carries the element's own facts. The OO copy had dropped the shared storage-claim scan, so each OO
element repeated the walk. `DataBinderSeedDriftTests` refuses any other `new DataBinder(`.

**Oracle.** `compare_oracle.py` against base 07bcf2586899: 69 of 23351 cases differ at 8cf07421b2b6 (the PB3238 text
changes and the PB3256 record length, every one classified in the notes), and 8cf07421b2b6 against f9e5fc2b54a1 is
IDENTICAL over 23351 cases. No inventory rows changed: each note closes none, with its `closes_rows_reason`.

**Self-review.** Architecture: each rule now has one owner and a drift test. No C# text was added to a bound node. The
culture scope sits at the backend seam. Code: the citations were checked with `cite.py` and `audit_code_citations.py`.
Performance: the advance adds no allocation, and the OO binders now share one claim scan. Duplication: StrongTypeModel's
private culture copy is folded into the scope.
