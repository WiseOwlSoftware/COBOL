# The collation key cache — `Runtime/Collation/Cache/`

A thread-safe, per-collator cache of materialized `CollationKey`s (kb/Work PB106): build a text's key once, compare
it many times. Three files: `CollationKeyCache.cs` (the cache), `CacheEntry.cs` (key + creation/access stamps + hit
count), `CacheConfig.cs` (size, eviction strategy, enable flag, text-length cap, environment mapping).

## 1. Strategy

- **One cache per `Collator`.** Keys are comparable only within one collator (table + settings), so
  `CollationKeyCache.For(collator)` hands out that collator's cache (created on demand, held weakly);
  `CollationKeyCache.Shared` is the root collator's, and the static `CollationKeyCache.GetOrBuild(string)` is its
  convenience form. Instance API: `GetKey(text)`, `TryGet(text, out key)`, `Compare(a, b)` (through keys),
  `Count`/`Hits`/`Misses`/`Evictions`, `Entries`, `Clear()`.
- **A `ConcurrentDictionary<string, CacheEntry>`** keyed by the text (ordinal). A hit is one lookup plus interlocked
  updates of the entry's access stamp and hit counter; a miss builds the key (`Collator.GetKey`), inserts, and if the
  count exceeds `MaxEntries` triggers an eviction — by ONE thread at a time (compare-exchange guarded); no lookup
  ever blocks. Two threads missing the same text at once both build, one wins the insert, both return the winning
  key — one key object per live text.
- **Eviction** (`CacheConfig.Eviction`): `LeastRecentlyUsed` (default) sorts a snapshot by access stamp and removes
  the oldest-used down to `(1 − EvictionFraction) × MaxEntries` (default: evict a quarter at once, so the O(n log n)
  scan is amortized over thousands of inserts); `SizeBased` does the same by insertion stamp (FIFO — cheaper when
  values stream through once). Stamps are `Stopwatch` ticks (monotonic, high resolution; the runtime reads no wall
  clock outside the run unit's clock seam).
- **Bounds**: `MaxEntries` (default 8,192 per collator), `MaxTextLength` (default 512 UTF-16 units — a longer text is
  keyed but not stored, so one huge key never displaces thousands of useful ones), `Enabled` (a disabled cache is a
  pass-through). Environment: `COBOL_COLLATION_CACHE` = `off` | `<max entries>`; `COBOL_COLLATION_CACHE_EVICTION` =
  `lru` | `fifo`. `CollationRuntime.Initialize()` (every run unit) reads them into `CollationKeyCache.DefaultConfig`
  unless a host called `CollationRuntime.ConfigureCache(...)` first.

## 2. Performance — where a cache pays and where it does not

Measured (`tests/Cobol.Net.Benchmarks`, `CacheBenchmarks`; numbers in the harness README): a hit on a short text is a
dictionary lookup (~20–30 ns) against a key build (~150–250 ns) — a large saving *when the value recurs*. But the
engine's **streaming comparison** of two short operands (~45 ns, zero allocation, decided at the first differing
primary) is *cheaper than two cache hits plus a key compare*, and a miss is a build plus a store. Hence the rule the
integration follows:

| Path | Through the cache? | Why |
|---|---|---|
| a relation condition, `MAX/MIN`, `ORD/CHAR` (`Collator.Compare` / `LocaleCollation.Compare`) | **no** | one comparison per operand pair; streaming wins |
| **SORT / MERGE** key columns (`CobolSort.KeyColumns`) | **yes** — every record's alphanumeric key window under a LOCALE sequence is keyed once (`CobolCollation.KeyOf` → the cache), the sort compares keys | n key builds instead of 2·n·log n element walks; records sharing a value share one key |
| **INDEXED file** key comparison (`IndexedConnector.KeyCompare`) | **yes** — a LOCALE-keyed file compares `KeyOf(a).CompareTo(KeyOf(b))` | the file's stored key values are compared on every lookup and insert; after the first, all hits |
| `CollationEngine.GetKey(text, locale)`, `Collator.GetKeyCached(text)` | yes | the explicit "key me this" API for hosts |

`CobolCollation.SupportsKeys` / `KeyOf` are the seam: the LOCALE arm answers true and keys through the cache of the
collator in effect (the §8.8.4.2.11 trim applied first, EC-LOCALE-INCOMPATIBLE for an ill-formed operand, exactly as
`Compare`); the ALPHABET table arms answer false — their per-character table lookup is cheaper than any key.

## 3. Integration with `CollationEngine`

The engine's key builder (`Collator.GetKey`) is pure and stays so; the cache wraps it. Nothing in the engine
normalizes to NFC or segments into clusters before keying: UCA is defined over NFD (the engine decomposes on demand),
and a key is per whole text because collation contractions cross grapheme-cluster boundaries and keys are level-major
— see `Unicode/Segmentation/README.md` §2, where both facts are pinned by tests. Correctness never depends on the
cache: every cached key is the collator's own key (`GetKey` equality asserted under concurrency), and a disabled or
evicted entry simply rebuilds.

## 4. Verified

`CollationKeyCacheTests`: one key per text and the counters; LRU keeps the recently used and evicts the rest;
size-based evicts the oldest; disabled / over-long texts are pass-through; concurrent callers under eviction always
get a correct key; per-collator caches stay apart; `LocaleCollation.KeyOf` orders exactly like `Compare` (Danish);
a SORT under the Swedish sequence orders exactly like a stable sort by `Compare` and keys through the cache.
