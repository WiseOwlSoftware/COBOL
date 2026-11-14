- **D-SET-SIZE-NOT-AVAIL — SET SIZE OF — the storage-physically-unavailable EC-STORAGE-NOT-AVAIL leg (§2023:14.9.39.4 GR38 third sentence)**:
  **implemented, not assumed away.** All three of GR37/GR38's outcomes are live and each sets the nonfatal
  EC-STORAGE-NOT-AVAIL under `>>TURN EC-STORAGE-NOT-AVAIL CHECKING ON`: a value that does not evaluate to a
  nonnegative number gives length 0 (GR37: a floating-point NaN is not a number, and −∞ is negative, so both take
  it — `2023/pb2647_set_size_not_finite`, kb/Work PB2647); a value above the item's §2023:8.5.1.10.1 **maximum size**,
  +∞ included (an infinity is a number in ISO/IEC 60559), is clamped to that
  maximum (GR38 second sentence); and, when the host cannot hand over the storage that expansion needs, **the size
  is not changed** (GR38 third sentence) — an `OutOfMemoryException` at the growth site inside
  `CobolDynString.SetSize`, which returns the item unchanged instead of letting a framework exception escape into
  generated code. The condition is raised ONCE per statement: a request that already took the clamp leg has set it
  to exist. Goldens `2023/ec_storage_not_avail` (a LIMIT-phrase item) and `2023/pb463_dyn_implementor_maximum` (an
  item with none).
  ⚠ **This row used to pin the opposite** — "a dynamic-length elementary item is a managed .NET `System.String`,
  always allocatable within the runtime string limit, so the … branch is unreachable" — and that was a claim about
  code nobody had checked. It was false in both directions (kb/Work PB463): the item's maximum size was the LIMIT
  phrase with `-1` meaning "no phrase", the clamp SKIPPED that sentinel, and so an item written without a LIMIT had
  no bound at all — a SET SIZE request of 2³²+k silently wrapped to k, and one between `int.MaxValue` and 2³² threw
  a raw `ArgumentOutOfRangeException` out of generated code. The maximum size is now computed by the one producer
  `CobolDynString.MaxSizeOf` for EVERY dynamic-length item (A.1 item 62 below), which makes the clamp the one
  path for an over-large request and the narrowing exact by construction rather than by inspection. The integer-2
  literal form is compile-time bounded by SR34 (`SetBinder.BindSetSize` over `SetLiteralAmount`, COBOL2113 —
  kb/Work PB458; the sentence stood here for a screen nobody had written until then), so its out-of-range
  cases are compile diagnostics, not this runtime EC.
