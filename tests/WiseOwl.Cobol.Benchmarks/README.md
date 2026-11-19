# `tests/WiseOwl.Cobol.Benchmarks/` — the BenchmarkDotNet harness

The measured performance profile of WiseOwl COBOL's performance-sensitive runtime subsystems: the **collation
engine** (`src/WiseOwl.Cobol.Runtime/Collation/`, kb/Work **PB101** — `Collation/CollationBenchmarks.cs`), the **key
cache** (**PB106** — `Collation/CacheBenchmarks.cs`), the **CLDR loader and builder** (**PB105** —
`Collation/CldrBenchmarks.cs`), and the **Unicode normalization and grapheme segmentation** subsystems
(**PB104** — `Unicode/UnicodeBenchmarks.cs`); and the **performance baseline**'s in-process half — compile throughput
and the generated programs' hot paths (**PB2117** — `Compile/`, `GeneratedCode/`, `Programs/`; see
[the baseline section](#the-performance-baseline--compile-throughput-and-generated-programs-kbwork-pb2117)).

This is **not a test project**. Nothing in CI runs it, it asserts nothing, and a red number here is a finding, not
a failure. It exists because the collation engine is on the hot path of ordinary COBOL — every relation condition
between alphanumeric operands under a LOCALE program collating sequence, every key comparison in `SORT`/`MERGE`,
every INDEXED-file key comparison, `MAX`/`MIN`, and `FUNCTION STANDARD-COMPARE` reach it
(`Collation/README.md` §6) — so a `SORT` of a million records is tens of millions of calls to
`Collator.Compare`. Its cost *and its allocation* per comparison are product characteristics.

It also puts a number on a design decision. `Collation/README.md` §1 declines to delegate to
`System.Globalization.CompareInfo` because the host's ICU floats with the operating system while a decade-old
INDEXED file must stay in key order. That is only defensible if the table-driven engine is in the same
performance class as the ICU it declines to use — so `CompareInfo` is a **baseline in every category here**,
alongside `string.CompareOrdinal` as the floor that no culturally-correct collation can reach.

---

## Running it

BenchmarkDotNet requires **Release**. A Debug build compiles and runs, but it measures a Debug JIT and the
harness says so; the numbers below are Release only. From the repository root:

```
dotnet build WiseOwl.Cobol.sln -c Release
dotnet run -c Release --project tests/WiseOwl.Cobol.Benchmarks -- --filter *Collation*
```

Other useful invocations:

```
dotnet run -c Release --project tests/WiseOwl.Cobol.Benchmarks                     # every benchmark class
dotnet run -c Release --project tests/WiseOwl.Cobol.Benchmarks -- --list flat      # what exists
dotnet run -c Release --project tests/WiseOwl.Cobol.Benchmarks -- --job short      # ADDS a short job beside the configured one
dotnet run -c Release --project tests/WiseOwl.Cobol.Benchmarks -- --filter *BuildKey*
```

The whole class takes **about two minutes** (`CacheBenchmarks`, `CldrBenchmarks`, `UnicodeBenchmarks`: about two
minutes each; `--filter '*'` runs all four). Everything after `--` goes to BenchmarkDotNet, so its full command line is
available (`--help` lists it).

> **Quoting.** `*Collation*` must be quoted in a POSIX shell (`--filter '*Collation*'`) or the shell expands it
> against the working directory and BenchmarkDotNet reports `You must have made a typo in 'Collation'`.

The built executable can also be run directly, which skips `dotnet run`'s implicit rebuild:

```
tests/WiseOwl.Cobol.Benchmarks/bin/Release/net10.0/WiseOwl.Cobol.Benchmarks.exe --filter '*Collation*'
```

Results land in `BenchmarkDotNet.Artifacts/results/` beside the working directory — GitHub-flavoured Markdown,
HTML and CSV. That folder is a build output and is `.gitignore`d; the numbers worth keeping are the ones pasted
into this file.

**Configuration** is `Collation/BenchmarkConfig.cs`, applied by `[Config(typeof(BenchmarkConfig))]`: 3 warm-up +
12 measured iterations at ≥ 250 ms each, one launch, `MemoryDiagnoser` on, Median and P95 added to the default
columns, and logical grouping by `[BenchmarkCategory]` so the `Ratio` column always compares a benchmark against
the baseline **of its own category** — a 1.5 KB walk is never ratioed against a five-letter word.

---

## What each benchmark measures

Every corpus is built once in `[GlobalSetup]`, and the loops report **per comparison** (`OperationsPerInvoke`
equals the corpus size, checked against the corpus in `Setup` so an added pair cannot silently rescale the
report). Every method returns an accumulated `int` so nothing is dead-code-eliminated.

### `Compare-Short` — 64 ASCII pairs, the headline number

The corpus is eight groups of eight, each a distinct *shape* of the comparison, because averaging over the shapes
is what makes one number representative instead of a best case: difference at the first character (exits after
one collation element), in the middle, at the last character (a full level-1 walk), **case-only** (equal at
levels 1–2, decided at level 3 — three walks), **equal** (the worst case: every level walked and still tied),
**prefix** (one operand runs out), digits and record-key shapes, and **punctuation/spaces** — the variable
elements on which the root and Standard collators do different work.

| Benchmark | What it exercises |
|---|---|
| `Compare_ShortStrings` | `CollationEngine.Compare` — the CLDR root order (tertiary, non-ignorable). The number that multiplies by the record count in a `SORT`. |
| `Compare_ShortStrings_Standard` | `CollationEngine.Standard` — four levels, **shifted** variables: what `STANDARD-COMPARE`'s default ordering table `"ISO 14651_2020_TABLE1"` resolves to. Strictly more work. |
| `Compare_ShortStrings_Span` | `Collator.Compare(ReadOnlySpan<char>, ReadOnlySpan<char>)` — the overload the COBOL layer will call with slices of a record buffer, since trailing-space truncation re-slices rather than allocating a substring. |
| `Baseline_Ordinal_ShortStrings` | **Baseline.** `string.CompareOrdinal` — no collation at all; the floor. |
| `Baseline_IcuInvariant_ShortStrings` | The host's ICU, `CompareInfo.Compare(…, CompareOptions.None)`. |

### `Compare-Long` — 1.5 KB operands, the per-character cost

Two 1536-character ASCII strings sharing a 1535-character stem (no combining marks, so the comparison stays off
the NFD path and this measures the element walk itself).

| Benchmark | What it exercises |
|---|---|
| `Compare_LongStrings` | Differ at the **last character** → one full level-1 walk of both texts. Divide by 1536 for ns/character. |
| `Compare_LongStrings_CaseOnly` | Differ only in the **case** of the last character → levels 1, 2 and 3 all walked in full. Expected at ≈ 3× the above. |
| `Baseline_Ordinal_LongStrings` | **Baseline.** A vectorized `memcmp`. |
| `Baseline_IcuInvariant_LongStrings` | The host's ICU at the same length. |

### `Compare-Mixed` — off the ASCII fast path

Twelve pairs that reach the rest of the engine: accented Latin (a level-2 decision), the ß / æ / œ **expansions**
that turn one character into several collation elements, precomposed vs. decomposed text (which forces the
table-driven `Normalizer`, and is canonically **equal**, so every level is walked and still ties), Han (no table
entry at all — UTS #10 Table 16 implicit weights), Hangul (decomposed to conjoining jamo), and Spanish ñ.

| Benchmark | What it exercises |
|---|---|
| `Compare_MixedLocaleStrings` | The corpus under the root order. |
| `Compare_MixedLocaleStrings_SpanishTailored` | The same corpus through `CollationEngine.ForLocale("es-ES")`. A tailoring is a **new table**, not an indirection over the root one (`Collation/README.md` §5), so this should track the root closely. `"año"`/`"ano"` is the pair that moves: level 2 under the root order, level 1 under the tailoring. |
| `Baseline_Ordinal_MixedStrings` | **Baseline.** UTF-16 code-unit order. |
| `Baseline_IcuInvariant_MixedStrings` | The host's ICU — the fairest like-for-like in this file, since ICU also has real work to do here. |

### `BuildKey-Short` / `BuildKey-Long` — materializing a sort key

What an INDEXED file's key column stores, and what a `SORT` should use when one record is compared many times.
Unlike `Compare`, this **must** allocate: every level is materialized and nothing can exit early. The question
`MemoryDiagnoser` answers here is *how much, per key*.

| Benchmark | What it exercises |
|---|---|
| `BuildKey_ShortStrings` | `CollationKey.Build` over the 64 short operands. |
| `Baseline_IcuSortKey_ShortStrings` | **Baseline.** `CompareInfo.GetSortKey` — the direct ICU analogue. |
| `BuildKey_LongStrings` | A key for the 1.5 KB operand. |
| `Baseline_IcuSortKey_LongStrings` | **Baseline.** ICU's sort key at the same length. |

### What a regression looks like

- **`Allocated` becomes non-zero on `Compare_ShortStrings`, `Compare_ShortStrings_Span` or
  `Compare_LongStrings`.** `Collation/README.md` §4 says the common path is allocation-free. A non-zero cell on
  pure-ASCII input means something started buffering elements, materializing a key, or boxing — and a `SORT`
  would feel that as GC pressure long before it felt it as CPU. **This is the single most valuable column here.**
- **`Compare_LongStrings` growing faster than linearly in length.** The comparison is one forward pass per level;
  super-linear means a re-walk crept in.
- **`Compare_LongStrings_CaseOnly` drifting far from 3× `Compare_LongStrings`.** Much more means a level is doing
  redundant work; much less means a level is being *skipped*, which is a correctness bug the ordering tests in
  `WiseOwl.Cobol.Tests.Unit` should catch first.
- **`Compare_MixedLocaleStrings` jumping while the ASCII paths hold still.** That is the NFD / expansion /
  contraction / implicit-weight machinery.
- **`Compare_MixedLocaleStrings_SpanishTailored` separating from `Compare_MixedLocaleStrings`.** Tailored lookups
  would be taking a slower path than root ones, taxing every locale that has a tailoring at all.
- **The ratio against `CompareInfo` worsening by a large factor.** Across hosts the *ratio*, not the absolute
  nanoseconds, is the number to watch: losing an order of magnitude reopens §1's trade-off.

---

## Results

Two runs on **2026-08-18**, same host, `dotnet build WiseOwl.Cobol.sln -c Release` followed by the filtered run above,
on an otherwise-idle developer workstation. **Run 1** is the engine as first landed (DEVLOG 1326); it found the
long-key outlier that became the identical-prefix skip (DEVLOG 1327). **Run 2** is the engine after that skip, with
the COBOL-layer carrier category added. Both pasted verbatim, including the host description BenchmarkDotNet prints.

### Run 2 — after the identical-prefix skip (the current engine)

```
BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9168/25H2/2025Update/HudsonValley2)
13th Gen Intel Core i9-13900K 3.00GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.303
  [Host]    : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  collation : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3

| Method                                     | Categories     | Mean          | Error         | StdDev      | Median        | P95           | Ratio          | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |--------------- |--------------:|--------------:|------------:|--------------:|--------------:|---------------:|--------:|-------:|-------:|----------:|------------:|
| BuildKey_LongStrings                       | BuildKey-Long  | 30,278.226 ns | 1,205.1016 ns | 940.8640 ns | 29,930.518 ns | 31,471.505 ns |   2.16x slower |   0.08x | 1.9531 | 0.1221 |   37240 B | 11.41x more |
| Baseline_IcuSortKey_LongStrings            | BuildKey-Long  | 14,047.204 ns |   364.7035 ns | 284.7365 ns | 14,187.968 ns | 14,310.452 ns |       baseline |         | 0.1526 |      - |    3264 B |             |
| BuildKey_ShortStrings                      | BuildKey-Short |    182.663 ns |    11.4622 ns |   8.9489 ns |    186.685 ns |    191.842 ns |   1.11x slower |   0.06x | 0.0281 |      - |     530 B |  4.14x more |
| Baseline_IcuSortKey_ShortStrings           | BuildKey-Short |    164.312 ns |     3.5052 ns |   2.7366 ns |    164.281 ns |    168.095 ns |       baseline |         | 0.0067 |      - |     128 B |             |
| Compare_LongStrings                        | Compare-Long   |    696.036 ns |    11.7886 ns |   9.2038 ns |    695.269 ns |    709.333 ns |  13.53x slower |   0.34x |      - |      - |         - |          NA |
| Compare_LongStrings_CaseOnly               | Compare-Long   |    730.612 ns |    18.5817 ns |  14.5074 ns |    731.573 ns |    749.882 ns |  14.20x slower |   0.41x |      - |      - |         - |          NA |
| Baseline_Ordinal_LongStrings               | Compare-Long   |     51.478 ns |     1.5105 ns |   1.1793 ns |     51.531 ns |     53.131 ns |       baseline |         |      - |      - |         - |          NA |
| Baseline_IcuInvariant_LongStrings          | Compare-Long   |    442.398 ns |    10.9080 ns |   8.5162 ns |    441.536 ns |    453.357 ns |   8.60x slower |   0.25x |      - |      - |         - |          NA |
| Compare_MixedLocaleStrings                 | Compare-Mixed  |    139.088 ns |     3.6994 ns |   2.8883 ns |    138.689 ns |    142.444 ns | 131.40x slower |   3.49x | 0.0048 |      - |      93 B |          NA |
| Compare_MixedLocaleStrings_SpanishTailored | Compare-Mixed  |    132.709 ns |     2.9481 ns |   2.3017 ns |    131.764 ns |    136.570 ns | 125.37x slower |   3.03x | 0.0048 |      - |      93 B |          NA |
| Baseline_Ordinal_MixedStrings              | Compare-Mixed  |      1.059 ns |     0.0249 ns |   0.0194 ns |      1.052 ns |      1.086 ns |       baseline |         |      - |      - |         - |          NA |
| Baseline_IcuInvariant_MixedStrings         | Compare-Mixed  |     33.673 ns |     0.9114 ns |   0.6029 ns |     33.440 ns |     34.548 ns |  31.81x slower |   0.78x |      - |      - |         - |          NA |
| Compare_ShortStrings                       | Compare-Short  |     44.940 ns |     0.7422 ns |   0.5795 ns |     45.166 ns |     45.396 ns |  38.13x slower |   0.73x |      - |      - |         - |          NA |
| Compare_ShortStrings_Standard              | Compare-Short  |     95.451 ns |     2.0796 ns |   1.6236 ns |     94.936 ns |     97.684 ns |  80.99x slower |   1.78x |      - |      - |         - |          NA |
| Compare_ShortStrings_LocaleCollation       | Compare-Short  |     65.586 ns |     1.3984 ns |   1.0918 ns |     65.277 ns |     67.275 ns |  55.65x slower |   1.21x |      - |      - |         - |          NA |
| Compare_ShortStrings_Span                  | Compare-Short  |     44.637 ns |     1.1053 ns |   0.8629 ns |     44.543 ns |     45.612 ns |  37.87x slower |   0.90x |      - |      - |         - |          NA |
| Baseline_Ordinal_ShortStrings              | Compare-Short  |      1.179 ns |     0.0233 ns |   0.0182 ns |      1.174 ns |      1.205 ns |       baseline |         |      - |      - |         - |          NA |
| Baseline_IcuInvariant_ShortStrings         | Compare-Short  |     13.131 ns |     0.3110 ns |   0.2428 ns |     13.153 ns |     13.453 ns |  11.14x slower |   0.26x |      - |      - |         - |          NA |
```

**What run 2 shows against run 1:** `Compare_LongStrings` **17,958 → 696 ns** (26×; now **1.6× the host ICU**, not
40×) and `Compare_LongStrings_CaseOnly` **54,038 → 731 ns** — the identical prefix is walked once with a vectorized
`CommonPrefixLength` instead of at every level; the two long variants are now within 5% of each other because the
level walks start at the last character. Short pairs improved too (**64.8 → 44.9 ns**, they share prefixes) — now
**3.4× ICU**. `Compare_ShortStrings_LocaleCollation` (the COBOL layer's carrier: §2023:8.8.4.2.11 span trim, well-formedness
scan, per-use resolution of the run unit's locale, then the engine) is **65.6 ns — ~20 ns over the engine and 0 B
allocated**, so the carrier needs no devirtualized fast arm. Unchanged, by construction: STANDARD-COMPARE's shifted
lane (the skip is taken for state-free non-ignorable collators only), key building, and the mixed-script path's 93 B
(the NFD path — the next engine follow-up).

### Run 1 — the engine as first landed

```
// * Summary *

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9168/25H2/2025Update/HudsonValley2)
13th Gen Intel Core i9-13900K 3.00GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.303
  [Host]    : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  collation : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3

Job=collation  MinIterationTime=250ms  IterationCount=12  
LaunchCount=1  RunStrategy=Throughput  WarmupCount=3  

| Method                                     | Categories     | Mean          | Error         | StdDev        | Median        | P95           | Ratio            | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |--------------- |--------------:|--------------:|--------------:|--------------:|--------------:|-----------------:|--------:|-------:|-------:|----------:|------------:|
| BuildKey_LongStrings                       | BuildKey-Long  | 30,126.919 ns | 1,293.9620 ns | 1,010.2404 ns | 29,592.789 ns | 31,631.549 ns |     2.14x slower |   0.08x | 1.9531 | 0.1221 |   37240 B | 11.41x more |
| Baseline_IcuSortKey_LongStrings            | BuildKey-Long  | 14,112.770 ns |   323.9494 ns |   234.2367 ns | 14,189.182 ns | 14,340.230 ns |         baseline |         | 0.1526 |      - |    3264 B |             |
|                                            |                |               |               |               |               |               |                  |         |        |        |           |             |
| BuildKey_ShortStrings                      | BuildKey-Short |    175.915 ns |     6.2119 ns |     4.8499 ns |    175.001 ns |    184.289 ns |     1.09x slower |   0.04x | 0.0281 |      - |     530 B |  4.14x more |
| Baseline_IcuSortKey_ShortStrings           | BuildKey-Short |    161.978 ns |     4.4650 ns |     3.4860 ns |    163.692 ns |    164.956 ns |         baseline |         | 0.0067 |      - |     128 B |             |
|                                            |                |               |               |               |               |               |                  |         |        |        |           |             |
| Compare_LongStrings                        | Compare-Long   | 17,958.219 ns |   325.1138 ns |   253.8274 ns | 17,903.244 ns | 18,327.669 ns |   358.04x slower |  10.00x |      - |      - |         - |          NA |
| Compare_LongStrings_CaseOnly               | Compare-Long   | 54,038.279 ns | 1,244.8410 ns |   971.8900 ns | 53,840.997 ns | 55,356.270 ns | 1,077.39x slower |  32.21x |      - |      - |         - |          NA |
| Baseline_Ordinal_LongStrings               | Compare-Long   |     50.187 ns |     1.6432 ns |     1.2829 ns |     49.946 ns |     52.025 ns |         baseline |         |      - |      - |         - |          NA |
| Baseline_IcuInvariant_LongStrings          | Compare-Long   |    446.190 ns |     9.1218 ns |     7.1217 ns |    449.688 ns |    452.384 ns |     8.90x slower |   0.26x |      - |      - |         - |          NA |
|                                            |                |               |               |               |               |               |                  |         |        |        |           |             |
| Compare_MixedLocaleStrings                 | Compare-Mixed  |    179.976 ns |     6.2988 ns |     4.9177 ns |    181.157 ns |    185.501 ns |   167.74x slower |   4.98x | 0.0045 |      - |      93 B |          NA |
| Compare_MixedLocaleStrings_SpanishTailored | Compare-Mixed  |    179.369 ns |     6.0076 ns |     4.6904 ns |    180.852 ns |    184.207 ns |   167.18x slower |   4.80x | 0.0045 |      - |      93 B |          NA |
| Baseline_Ordinal_MixedStrings              | Compare-Mixed  |      1.073 ns |     0.0258 ns |     0.0153 ns |      1.077 ns |      1.085 ns |         baseline |         |      - |      - |         - |          NA |
| Baseline_IcuInvariant_MixedStrings         | Compare-Mixed  |     34.393 ns |     2.0579 ns |     1.4880 ns |     34.070 ns |     36.817 ns |    32.06x slower |   1.40x |      - |      - |         - |          NA |
|                                            |                |               |               |               |               |               |                  |         |        |        |           |             |
| Compare_ShortStrings                       | Compare-Short  |     64.788 ns |     1.7571 ns |     1.3719 ns |     65.391 ns |     66.051 ns |    54.90x slower |   1.69x |      - |      - |         - |          NA |
| Compare_ShortStrings_Standard              | Compare-Short  |     99.450 ns |     3.1013 ns |     2.4213 ns |     99.249 ns |    102.860 ns |    84.27x slower |   2.77x |      - |      - |         - |          NA |
| Compare_ShortStrings_Span                  | Compare-Short  |     68.594 ns |     2.2796 ns |     1.7797 ns |     68.136 ns |     71.283 ns |    58.12x slower |   1.98x |      - |      - |         - |          NA |
| Baseline_Ordinal_ShortStrings              | Compare-Short  |      1.181 ns |     0.0360 ns |     0.0281 ns |      1.182 ns |      1.214 ns |         baseline |         |      - |      - |         - |          NA |
| Baseline_IcuInvariant_ShortStrings         | Compare-Short  |     12.978 ns |     0.3423 ns |     0.2475 ns |     12.883 ns |     13.437 ns |    11.00x slower |   0.32x |      - |      - |         - |          NA |
```

The run reported no warnings and no config issues. Its only hint was outlier removal on four benchmarks
(1–3 outliers each, all on the baselines):

```
// * Hints *
Outliers
  CollationBenchmarks.Baseline_IcuSortKey_LongStrings: collation    -> 1 outlier  was  removed, 3 outliers were detected (13.70 us, 13.70 us, 14.65 us)
  CollationBenchmarks.Baseline_Ordinal_MixedStrings: collation      -> 3 outliers were removed (1.32 ns..1.64 ns)
  CollationBenchmarks.Baseline_IcuInvariant_MixedStrings: collation -> 1 outlier  was  removed (39.62 ns)
  CollationBenchmarks.Baseline_IcuInvariant_ShortStrings: collation -> 1 outlier  was  removed (13.82 ns)
```

### Reading the numbers

**The allocation claim holds.** Every comparison benchmark on ASCII input — short, Standard, span, both long
variants — reports `-` in `Allocated`: **zero bytes per comparison**, at every string length and under both
alternate-handling modes. `Collation/README.md` §4's "the common unequal-at-primary case is one pass and no
allocation" is now measured rather than asserted, and it is the property a million-record `SORT` depends on.

**The level model is confirmed to three digits.** `Compare_LongStrings_CaseOnly` / `Compare_LongStrings` =
54,038 / 17,958 = **3.01×**, against a prediction of exactly 3 (a case-only difference forces levels 1, 2 and 3 to
be walked in full where a last-character primary difference costs one walk). Nothing is being skipped and nothing
is walking twice.

**Against the host ICU.** On the corpus that matters most — short alphanumeric keys — the engine costs
**64.8 ns** per comparison against ICU's **13.0 ns**: **≈ 5.0× ICU**, at 55× ordinal. On the mixed-script corpus
the gap is similar (180.0 ns vs 34.4 ns, **≈ 5.2×**). Sort-key construction is where the engine is closest:
**176 ns vs 162 ns, 1.09×** for short operands. That is the same performance class, and it is the answer to §1's
trade-off: on this host, one order that does not float with the operating system costs about five times a
comparison that does.

**The exception is long strings**, and it is the one number worth following up. At 1536 characters the engine
takes **17.96 µs (11.7 ns/character)** where ICU takes **446 ns (0.29 ns/character)** — a gap of **40×**, against
5× on short input. That is not a constant factor stretched out; it is a different per-character regime. ICU almost certainly
has a vectorized Latin-1 fast path and a bulk common-prefix skip that the streaming
`CollationElementIterator` does not; the same shape shows in key construction (**2.14×** ICU at 1.5 KB against
**1.09×** at word length). COBOL alphanumeric keys are short, so this does not touch the common case — but a
`SORT` on a long `PIC X(2000)` key under a LOCALE collating sequence would feel it.

**Shifted costs about half as much again.** `Compare_ShortStrings_Standard` (four levels, variables shifted) is
**99.5 ns** against the root collator's **64.8 ns** — **1.54×**. That is the price of `STANDARD-COMPARE`'s
default ordering table over the CLDR/ICU default, on a corpus one-eighth of which is punctuation pairs that
genuinely need the fourth level.

**Tailoring is free.** `Compare_MixedLocaleStrings_SpanishTailored` (179.4 ns) and
`Compare_MixedLocaleStrings` (180.0 ns) are indistinguishable — 0.3% apart against a ±6 ns error. A tailored
table really is a table, not an indirection over the root one, so a locale with a tailoring pays nothing for
having one.

**The span and string overloads are indistinguishable.** 68.6 ns vs 64.8 ns, with the *string* overload — which
forwards to the span one and therefore cannot physically be faster — measuring lower. The ~4 ns is measurement
noise and code layout, not a real difference; treat the two as one number. The COBOL layer's re-slicing approach
to trailing-space truncation costs nothing.

**One allocation to look at.** `Compare_MixedLocaleStrings` allocates **93 B per comparison** averaged over the
twelve pairs — the only comparison path in the table that allocates at all. The candidate is the table-driven
`Normalizer`: two of the twenty-four strings in that corpus hold a combining mark and are decomposed to NFD
before the walk (`Collation/README.md` §4.1), and NFD returns a new string. If a COBOL program's data routinely
carries combining marks, that allocation is on its comparison path. Whether the whole 93 B is the normalizer, and
whether it can be a stack buffer for short inputs, is not answered by this harness — narrowing it is the obvious
next measurement.

---

## The other classes — cache, CLDR, Unicode (run 3, 2026-08-19)

Same host and configuration; `--filter '*CacheBenchmarks*' '*CldrBenchmarks*' '*UnicodeBenchmarks*'` (about six
minutes). Each category has the host's implementation of the same job as its baseline where one exists.

### `CacheBenchmarks` — the collation key cache (`Collation/Cache/README.md`)

| Category | Benchmark | Mean | Alloc | Reading |
|---|---|---|---|---|
| Cache-Hit | `Hit_ShortStrings` vs `Build_ShortStrings_NoCache` (baseline) | **23.6 ns** vs 214 ns (541 B) | 0 B | a hit is a dictionary lookup + interlocked stamp: **9× faster than building**, no allocation |
| Cache-Hit | `Hit_LongStrings` (400-char texts) | 82.7 ns | 0 B | the lookup hashes the text; still 2.6× the build |
| Cache-Miss | `Miss_ShortStrings` (a full 1,024-entry cache fed only new texts — every lookup misses, an eviction batch every 256 inserts) vs the disabled pass-through | 508 ns vs 355 ns | 770 B vs 664 B | steady-state churn costs **≈1.4× the bare build** — the eviction batch (a lock-free enumeration + sort by stamp) is amortized to ~150 ns per miss (a first version read `ConcurrentDictionary.Count` on every insert and `ToArray()`-snapshotted per batch — every bucket lock, both — and cost 27×; replaced by an interlocked count and the enumeration) |
| Cache-Compare-Short | `Compare_ShortStrings_ViaCache` vs the streaming `Collator.Compare` (baseline) | 48.0 ns vs **26.9 ns** | 0 B | **the streaming comparison wins for short operands** — two lookups cost more than deciding at the first differing primary; hence relation conditions never go through the cache |
| Cache-Compare-Long | 400-char texts sharing a 390-char stem, via cache vs streaming | 322 ns vs **246 ns** | 0 B | with an identical prefix to skip, streaming still wins |
| Cache-Compare-CaseDifferent | 120-char texts differing only in case throughout (equal through level 2, decided at level 3 — the record-comparison shape of a SORT), via cache vs streaming | **196 ns** vs 3,245 ns | 0 B | **16.6× faster through keys**: no prefix to skip, three full walks otherwise. This is the case SORT/MERGE and INDEXED keys are in, and why they key once and compare keys |

### `CldrBenchmarks` — the CLDR loader and tailoring builder (`Collation/CLDR/README.md`)

| Category | Benchmark | Mean | Alloc | Reading |
|---|---|---|---|---|
| CLDR-Parse | `Parse_Es` / `Parse_Da` / `Parse_Zh` | 5.7 µs / 5.0 µs / **18.7 ms** | 21 KB / 21 KB / 36 MB | LDML XML + the rule syntax; zh.xml is 1.1 MB (pinyin, stroke, zhuyin, unihan) |
| CLDR-Build | `Build_Es` / `Build_Da` / `Build_Vi` / `Build_Zh` | **6.1 ms / 6.5 ms / 7.7 ms / 50.5 ms** | 10.8 / 10.9 / 13.0 / 75.6 MB | rules → a tailored table + settings; the fixed ~6 ms is the table REBUILD (copying the 41,724-element pool and 38,787 mappings and renumbering the tertiary line, which almost every `<<<` forces); Vietnamese adds the closure over ~900 composites, Chinese the ~40,000 relations and a script reorder. **Paid once per process per locale** — see the next row |
| CLDR-Resolve | `Resolve_Cached_Es` / `Resolve_Cached_Zh` | **19.8 ns / 19.6 ns** | 40 B | what a program pays after the first use: the engine's per-tag cache lookup — identical for the lightest and the heaviest locale |

### `UnicodeBenchmarks` — normalization and segmentation (`Unicode/README.md`, `Unicode/Segmentation/README.md`)

| Category | Benchmark | Mean | Alloc | Reading |
|---|---|---|---|---|
| NFD-ASCII | `Nfd_Ascii` vs the host's `string.Normalize(FormD)` | 7.1 ns vs 1.1 ns | 0 B | our "needs NFD?" scan of a 32-char text vs the host's ASCII short-circuit; both return the input by reference |
| NFD-Accented | `Nfd_Accented` (accented / composed / Hangul corpus) vs the host | 50.3 ns vs 39.4 ns | **394 B** vs 32 B | the table-driven decomposition is in the host's class in time and behind it in allocation — the `List<int>` + string build in `Normalizer.ToNfd`; the next engine follow-up (also the 93 B on the mixed-script comparison path) |
| NFC | `Nfc_Accented` (the subsystem) vs the host directly | 32.9 ns vs 32.0 ns | 2 B | the subsystem's NFC IS the host's composer behind the invariant-mode fallback: 1 ns of dispatch |
| Segment-ASCII | `Segment_Ascii` (`GraphemeBreaker.Count`, 32-char texts) vs the host's `StringInfo.LengthInTextElements` | **129 ns vs 450 ns** | 0 B vs 192 B | **3.5× faster than the host, no allocation** — one property lookup per code point |
| Segment-Mixed | marks, emoji sequences, flags, Hangul jamo, Indic conjuncts | **11.3 ns vs 39.2 ns** | 0 B vs 66 B | 3.5× |
| Segment-Long | enumerating a ~2 KB mixed text vs `StringInfo.GetTextElementEnumerator` | **7.5 µs vs 24.9 µs** | 0 B vs 37 KB | 3.3× |

The raw BenchmarkDotNet tables of run 3 are in the session's `BenchmarkDotNet.Artifacts/results/` (not committed);
the numbers above are copied from them.

---

## The performance baseline — compile throughput and generated programs (kb/Work PB2117)

Two classes are the in-process half of the baseline that `scripts/arch/perf_baseline.py` records and compares
(kb/Work A6's instrument; `docs/rearchitecture/DESIGN-architecture-review.md` §3 R0 and §4 item 5). Run them through
the script, which also takes the cold, whole-process and GnuCOBOL measurements and writes the record to
`docs/rearchitecture/evidence/perf-baseline/<product commit>.md`; the numbers live there, not here.

- **`Compile/CompileBenchmarks`** — `CompilerDriver.Compile` over the three largest NIST programs (IX113A, NC105A,
  NC218A) and the one making the most COPY statements (SM201A), compiled as the NIST harness compiles them.
  `FrontEndAndBind` (the `CheckOnly` compile: preprocess, parse, bind, the conformance pass) is the baseline and
  `FullCompile` adds emit, Roslyn and the written assembly, so the `Ratio` column is the backend's share. Witness: both
  compiles succeed, the assembly is non-empty, and SM201A really read its library texts.
- **`GeneratedCode/GeneratedProgramBenchmarks`** — the programs under `Programs/` (`MOVEHOT` moves through every storage
  form, `PERFHOT` is PERFORM dispatch, `SEQHOT` sequential and `IDXHOT` indexed file I/O, `SORTHOT` a SORT through input
  and output procedures and `MERGEHOT` a MERGE of four files, kb/Work PB2777), compiled once at the shipping
  edition and run in this process through their entry point: the generated code and the runtime, without process
  start-up. Witness: the program's whole output must equal its line in `Programs/witnesses.tsv`, which is computed from
  the formula in the program's header comment, never copied from a run. `Programs/EMPTYRUN.cob` is the start-up floor
  the script's whole-process rows are read against.

The `Programs/` sources are plain COBOL 85 on purpose: the script runs the same files through GnuCOBOL 3.2 under WSL,
so the comparison always measures the same COBOL. Each has exactly one `01 LOOP-COUNT PIC 9(9) COMP VALUE n.` line,
which the script scales (1x, 2x, 4x) for the scaling curve. The script also records every whole process's PEAK
resident set, which allocated bytes cannot stand in for, and times a program with an SD entry twice: under the default
sort memory limit, where every scale is sorted in memory, and as `<program>-SPILL` under a limit several times smaller
than its records, where the sort writes sorted runs to temporary files and merges them (`docs/CONFORMANCE.md` §3
D-SWK). Both implementations get the same limit, and the record shows the spilling case's peak staying nearly flat as
the input grows (kb/Work PB2777).

### The listing's cost (kb/Work PB5923, DESIGN-listing.md section 9)

`CompileBenchmarks` takes `Listing` = `None` (no listing: the path every caller that asks for none takes) and `Text` (the
paginated text listing written beside the assembly). Its witness: `Text` wrote a listing of every source line, `None` wrote
none. Measured on 2026-11-18, Release, one Windows host, the parent commit (`eb507bc720`, no `Listing` parameter) and the
step-1 branch (`listing-step-1` after the `Wrap` fix) run back to back. Mean time and allocated bytes per compile:

| Program | Method | parent | None | Text | parent alloc | None alloc | Text alloc |
|---|---|---|---|---|---|---|---|
| IX113A | FrontEndAndBind | 109.88 ms | 111.47 ms | 94.75 ms | 72.82 MB | 72.93 MB | 79.44 MB |
| IX113A | FullCompile | 514.99 ms | 532.80 ms | 573.45 ms | 179.05 MB | 180.86 MB | 185.05 MB |
| NC105A | FrontEndAndBind | 73.76 ms | 90.46 ms | 100.41 ms | 37.01 MB | 37.02 MB | 40.54 MB |
| NC105A | FullCompile | 462.43 ms | 439.66 ms | 467.15 ms | 108.95 MB | 109.29 MB | 113.17 MB |
| NC218A | FrontEndAndBind | 84.03 ms | 88.12 ms | 97.47 ms | 32.60 MB | 33.19 MB | 36.06 MB |
| NC218A | FullCompile | 447.90 ms | 414.73 ms | 457.81 ms | 97.87 MB | 102.53 MB | 105.55 MB |
| SM201A | FrontEndAndBind | 57.98 ms | 57.84 ms | 57.95 ms | 16.41 MB | 16.11 MB | 17.69 MB |
| SM201A | FullCompile | 203.88 ms | 204.96 ms | 194.96 ms | 36.53 MB | 36.54 MB | 39.90 MB |

Reading them: the host was loaded (StdDev 20-30 % of the mean on most rows), so no time row can decide the budgets
(None within the parent's noise; Text at most 10 % over None); every time difference above is inside one standard
deviation. Allocated bytes are steadier: `Text` costs 2-9 % over `None` on a full compile and 9-10 % on the front end
alone, and `None` equals the parent within 0.6 MB except NC218A's full compile (+4.7 MB, where the front end alone differs
by 0.6 MB, so the rest is the backend's own variation). A quiet-host re-measure is owed (kb/Work PB5927). The first run,
before `TextListingRenderer.Wrap` stopped allocating a string per character, showed `Text` 28-35 % over `None` on the
front end; that is the fix the measurement found.

**Step 2 (kb/Work PB5925, the JSON listing): `Text` re-measured with JSON.** `Listing` gains `TextAndJson` (the text and the
JSON listing written beside the assembly); its witness reads the JSON back and checks it holds every line of the listing.
Measured on 2026-11-18, Release, one Windows host, the `listing-step-2` branch, the three modes in one run
(`dotnet run -c Release --project tests/WiseOwl.Cobol.Benchmarks -- --filter *CompileBenchmarks*`, 24 cases, 289 s):

| Program | Method | None | Text | TextAndJson | None alloc | Text alloc | TextAndJson alloc | JSON file |
|---|---|---|---|---|---|---|---|---|
| IX113A | FrontEndAndBind | 105.22 ms | 138.54 ms | 115.60 ms | 72.86 MB | 79.50 MB | 87.70 MB | |
| IX113A | FullCompile | 537.42 ms | 531.81 ms | 544.54 ms | 176.62 MB | 185.40 MB | 191.30 MB | 2,134,814 B |
| NC105A | FrontEndAndBind | 78.00 ms | 93.36 ms | 75.44 ms | 37.40 MB | 40.70 MB | 45.55 MB | |
| NC105A | FullCompile | 527.50 ms | 480.64 ms | 518.82 ms | 104.86 MB | 112.39 MB | 112.45 MB | 1,141,529 B |
| NC218A | FrontEndAndBind | 82.78 ms | 64.68 ms | 78.54 ms | 32.59 MB | 36.57 MB | 40.72 MB | |
| NC218A | FullCompile | 411.81 ms | 463.53 ms | 448.69 ms | 102.14 MB | 101.32 MB | 105.96 MB | 1,126,280 B |
| SM201A | FrontEndAndBind | 48.86 ms | 51.13 ms | 63.53 ms | 15.97 MB | 17.62 MB | 19.44 MB | |
| SM201A | FullCompile | 184.95 ms | 241.36 ms | 314.27 ms | 38.60 MB | 39.89 MB | 41.13 MB | 452,259 B |

Reading them: the host was loaded again (StdDev 25-40 % of the mean on most rows, SM201A's `TextAndJson` full compile 25 %),
so, as at step 1, no time row decides a budget; every time difference but SM201A's full compile is inside one standard
deviation, and SM201A's is the noisiest case of the run. Allocated bytes: on a full compile `TextAndJson` costs 3.7-8.3 %
over `None` (inside the 10 % budget of the standard listing), and 0.1-3.2 % over `Text` alone; on the front end alone,
where the listing is a larger share, 20-25 % over `None` and 10-12 % over `Text`. The JSON file is 3.5 times the text
listing (it carries every line's attributes as named properties, indented), and is built as one string before it is
written. The quiet-host re-measure of every row is still owed (kb/Work PB5927).

**Step 3 (kb/Work PB5925, diagnostics): the no-listing budget against the parent, and every row with the diagnostics section.**
Step 3 changes the path of a compilation that asks for no listing (a bind diagnostic is a record, a located `SourceLocation`
carries two more integers), so it measures `None` against its parent. Measured on 2026-11-18, Release, one Windows host, the
parent (`5d26bc2f25`, main with step 2) and the `listing-step-3` branch, each a full run of
`dotnet run -c Release --project tests/WiseOwl.Cobol.Benchmarks -- --filter *CompileBenchmarks*` (24 cases, about 300 s).
Allocated bytes per compile, parent -> step 3:

| Program | Method | None | Text | TextAndJson |
|---|---|---|---|---|
| IX113A | FrontEndAndBind | 72.90 -> 67.64 MB | 79.49 -> 74.42 MB | 87.67 -> 83.06 MB |
| IX113A | FullCompile | 172.67 -> 172.01 MB | 177.71 -> 171.56 MB | 190.69 -> 179.46 MB |
| NC105A | FrontEndAndBind | 37.66 -> 34.02 MB | 40.61 -> 37.57 MB | 44.91 -> 42.64 MB |
| NC105A | FullCompile | 105.66 -> 107.03 MB | 109.96 -> 110.43 MB | 117.82 -> 110.96 MB |
| NC218A | FrontEndAndBind | 32.61 -> 30.09 MB | 36.22 -> 33.60 MB | 40.37 -> 38.39 MB |
| NC218A | FullCompile | 99.48 -> 95.82 MB | 102.98 -> 98.98 MB | 109.76 -> 103.75 MB |
| SM201A | FrontEndAndBind | 17.72 -> 15.07 MB | 17.61 -> 16.47 MB | 19.19 -> 18.30 MB |
| SM201A | FullCompile | 38.40 -> 37.76 MB | 40.59 -> 38.72 MB | 41.91 -> 40.73 MB |

Reading them: the first branch run was OVER the no-listing budget: the front end and bind of IX113A allocated 0.85 MB more than
the parent in two runs each (72.90 and 72.86 MB against 73.77 and 73.70 MB), NC218A 0.4 MB more. An exact probe
(`GC.GetAllocatedBytesForCurrentThread` around 30 check-only compiles of IX113A, three runs each: parent 73.11-73.74 MB, branch
73.90-74.61 MB) and the runtime's allocation-tick sampling found the cause: `MarginRCutScreen.Warnings` built a closure
capturing the cut's `SourceLocation` once per CHARACTER of every token, before asking whether there was a cut (about 112,000
closures, 5.4 MB per compile), and step 3's two integers made each 8 bytes larger. The screen now asks with a loop
(`MarginRCutScreen.Holds`), and the same probe reads 68.14-68.20 MB: 5.3 MB (7 %) under the parent. The text listing's cost
over `None` on the front end is the parent's (IX113A 6.78 MB against 6.59-6.67 MB) once the two sections are composed into
one buffer (the first version copied the whole listing to join the summary to it, 1.4 MB on IX113A). On these clean programs
the diagnostics section is one summary page; full-compile rows vary by several MB from run to run (the backend's own
variation, as at steps 1 and 2). Time rows are not shown: StdDev was again 20-40 % of the mean (kb/Work PB5927).

**Step 4a (kb/Work PB5925, the directive hooks): the no-listing budget against the parent, and every row with the hooks.**
Step 4a touches the path of a compilation that asks for no listing (the conditional-compilation driver carries the LISTING
state for PUSH / POP; the DISPLAY directive's LISTING destination is a stream of its own; the replacing actions take a null
match observer), so it measures `None` against its parent. Measured on 2026-11-18, Release, one Windows host, the parent
(`14fc6f26ac`, main with step 3) and the `listing-step-4a` branch, back to back, each a full run of
`dotnet run -c Release --project tests/WiseOwl.Cobol.Benchmarks -- --filter *CompileBenchmarks*` (24 cases). Allocated bytes
per compile, parent -> step 4a:

| Program | Method | None | Text | TextAndJson |
|---|---|---|---|---|
| IX113A | FrontEndAndBind | 67.63 -> 67.60 MB | 74.34 -> 74.70 MB | 83.09 -> 83.47 MB |
| IX113A | FullCompile | 176.89 -> 176.43 MB | 179.66 -> 188.67 MB | 191.83 -> 190.76 MB |
| NC105A | FrontEndAndBind | 34.09 -> 34.03 MB | 38.22 -> 37.70 MB | 42.21 -> 42.38 MB |
| NC105A | FullCompile | 106.00 -> 103.35 MB | 105.68 -> 111.13 MB | 114.20 -> 111.31 MB |
| NC218A | FrontEndAndBind | 30.08 -> 30.06 MB | 33.59 -> 34.34 MB | 38.35 -> 38.51 MB |
| NC218A | FullCompile | 99.54 -> 96.33 MB | 99.11 -> 99.06 MB | 105.37 -> 103.84 MB |
| SM201A | FrontEndAndBind | 14.90 -> 15.11 MB | 16.46 -> 16.65 MB | 18.24 -> 18.51 MB |
| SM201A | FullCompile | 38.02 -> 37.49 MB | 39.24 -> 39.27 MB | 40.88 -> 41.25 MB |

Reading them: the no-listing budget holds. The front end and bind, where step 4a's changes run, allocate within 0.21 MB of the
parent on every program (-0.06 to +0.21 MB, run-to-run variation of the size steps 1 to 3 saw), and the full-compile `None`
rows are at or under the parent. With a listing, the front end's rows move by -0.52 to +0.75 MB: these NIST programs hold no
LISTING, PAGE, DISPLAY or REPLACE (the hooks record nothing for them), and the full-compile rows vary by several MB both ways
(the backend's own variation, as before). Time rows are not shown: StdDev was 20-40 % of the mean again (kb/Work PB5927).

**Step 5 (kb/Work PB5925, options, program map, statistics and verbs): every row with the standard sections.** Step 5 adds
no work to a compilation that asks for no listing (the statements are read off the parse only when `Frontend.RecordsListing`
is set, and the options rows are built only inside the driver's listing branch; `git diff` of the branch against its parent
touches no other path), and it makes the default request (`standard`) produce three more sections: the options, the program
map and the statistics with the verbs. Measured on 2026-11-18, Release, one Windows host, the `listing-step-5` branch, one full
run of `dotnet run -c Release --project tests/WiseOwl.Cobol.Benchmarks -- --filter *CompileBenchmarks*` (24 cases). Allocated
bytes per compile, step 5, with step 4a's run (the same product code as the parent, `64e0c51de1`, whose step 4b changed no
file under `src/`) beside it:

| Program | Method | None | Text | TextAndJson | Text over None |
|---|---|---|---|---|---|
| IX113A | FrontEndAndBind | 67.67 (67.60) MB | 75.35 (74.70) MB | 84.29 (83.47) MB | +11.3 % |
| IX113A | FullCompile | 176.47 (176.43) MB | 185.95 (188.67) MB | 190.00 (190.76) MB | +5.4 % |
| NC105A | FrontEndAndBind | 34.26 (34.03) MB | 38.35 (37.70) MB | 43.74 (42.38) MB | +11.9 % |
| NC105A | FullCompile | 106.08 (103.35) MB | 106.29 (111.13) MB | 115.58 (111.31) MB | +0.2 % |
| NC218A | FrontEndAndBind | 30.08 (30.06) MB | 34.81 (34.34) MB | 39.26 (38.51) MB | +15.7 % |
| NC218A | FullCompile | 97.75 (96.33) MB | 100.37 (99.06) MB | 109.10 (103.84) MB | +2.7 % |
| SM201A | FrontEndAndBind | 15.12 (15.11) MB | 17.03 (16.65) MB | 18.81 (18.51) MB | +12.6 % |
| SM201A | FullCompile | 37.94 (37.49) MB | 39.92 (39.27) MB | 39.72 (41.25) MB | +5.2 % |

Reading them: the standard text listing's budget (at most 10 % over the no-listing compile, DESIGN-listing.md section 9)
holds on every program, +0.2 % to +5.4 % of a full compile. Step 5's own cost is the front end and bind's with a listing,
where the sections are built: +0.38 to +0.65 MB on `Text` and +0.30 to +1.36 MB on `TextAndJson` over step 4a's rows (the
statements' walk of the parse tree, the options rows and the statistics, and in JSON every line kind and severity counted
for the listing and for each file). The `None` rows of the front end and bind are within 0.23 MB of step 4a's, the
run-to-run variation of the earlier steps, as a change that adds no no-listing work should be. The full-compile rows move
by several MB both ways (NC105A's `Text` is 4.8 MB under step 4a's, NC218A's `TextAndJson` 5.3 MB over), the backend's own
variation. The front end and bind with `Text` is 11 to 16 % over `None`, as it was before step 5 (10.2 to 14.2 % at step 4a):
that ratio is of the front end alone, where the listing's work is, and the budget is the compile's. Time rows are not shown:
StdDev was up to 51 % of the mean (kb/Work PB5927).

**Step 6 (kb/Work PB5925, the data map from the symbol table): the `All` row.** Step 6 adds `ListingMode.All` (every section,
the data map included, in text and JSON). Measured on 2026-11-18, Release, one Windows host, one full run of
`dotnet run -c Release --project tests/WiseOwl.Cobol.Benchmarks -- --filter *CompileBenchmarks*` on the `listing-step-6`
branch (32 cases) and one on its parent `0d4042678a` (24 cases, no `All`), the parent's in parentheses. Allocated bytes per
compile:

| Program | Method | None | Text | TextAndJson | All | Text over None | All over None |
|---|---|---|---|---|---|---|---|
| IX113A | FrontEndAndBind | 68.63 (68.69) MB | 76.42 (76.34) MB | 85.32 (85.19) MB | 96.60 MB | +11.4 % | +40.8 % |
| IX113A | FullCompile | 169.36 (173.08) MB | 180.54 (182.69) MB | 192.23 (192.23) MB | 203.37 MB | +6.6 % | +20.1 % |
| NC105A | FrontEndAndBind | 35.28 (34.69) MB | 39.14 (39.60) MB | 44.44 (44.39) MB | 46.32 MB | +10.9 % | +31.3 % |
| NC105A | FullCompile | 102.42 (103.39) MB | 111.50 (107.56) MB | 111.45 (115.50) MB | 114.09 MB | +8.9 % | +11.4 % |
| NC218A | FrontEndAndBind | 30.83 (31.15) MB | 34.88 (35.43) MB | 40.23 (39.58) MB | 40.98 MB | +13.1 % | +32.9 % |
| NC218A | FullCompile | 100.19 (100.33) MB | 100.95 (100.18) MB | 105.47 (109.30) MB | 111.36 MB | +0.8 % | +11.1 % |
| SM201A | FrontEndAndBind | 15.10 (15.57) MB | 17.05 (17.22) MB | 19.18 (19.27) MB | 21.06 MB | +12.9 % | +39.5 % |
| SM201A | FullCompile | 35.76 (37.83) MB | 39.78 (39.89) MB | 41.75 (41.80) MB | 42.99 MB | +11.2 % | +20.2 % |

Reading them: every section in both formats (`All`) is +11.1 % to +20.2 % over no listing on a full compile, inside its 25 %
budget (DESIGN-listing.md section 9); the data map's own cost is the front end and bind's `All` over `TextAndJson`, +0.75 to
+11.28 MB (the witness counts 164 names in NC218A's data map, 275 in NC105A's, 293 in SM201A's and 1,544 in IX113A's). Step 6 adds no no-listing work: the `None` rows of the front end and bind are within
0.6 MB of the parent's, both ways. The standard text listing finishes only the program entries (`EditionContext.RecordsNames`);
the first run of this step finished every name for it and put SM201A's `Text` at 40.32 MB (+12.6 %), which is why. On this run
SM201A's `Text` full compile reads +11.2 % over `None`, over its 10 % budget, but the cause is the `None` row: step 6's
`Text` is the parent's within 0.11 MB (39.78 against 39.89), and its `None` full compile is 2.07 MB under the parent's for the
same no-listing code (35.76 against 37.83), the backend's run-to-run variation; against the parent's `None` it is +5.2 %, as
step 5 measured. A full compile's allocation varies by about 2 MB between runs of one tree, which on SM201A's 36 MB is as large
as the budget's margin (kb/Work PB5925's lead `2026-11-18-full-compile-allocation-noise`). Time rows are not shown (kb/Work
PB5927).

**Step 7a (kb/Work PB5925, a classifying label on every name slot of the grammar): the `None` row against the parent.** Step 7a
adds no listing section; it adds 452 element labels to the grammar, and ANTLR makes each a field of its rule's context class, so
every parse of every compilation, with or without a listing, allocates larger context objects. Measured on 2026-11-18, Release,
one Windows host, `dotnet run -c Release --project tests/WiseOwl.Cobol.Benchmarks -- --filter '*CompileBenchmarks.FrontEndAndBind*'`
twice on the `listing-step-7` branch (commit 4340f3a1e4) and twice on its parent `6cb363e123`, alternating. Allocated bytes per
front end and bind, the two runs of each tree:

| Program | Listing | Parent | Step 7a | Step 7a over the parent (both runs) |
|---|---|---|---|---|
| IX113A | None | 68.63, 68.54 MB | 68.92, 68.88 MB | +0.25 to +0.38 MB (+0.4 to +0.5 %) |
| NC105A | None | 34.67, 34.63 MB | 35.24, 34.83 MB | +0.16 to +0.61 MB |
| NC218A | None | 30.61, 30.60 MB | 30.79, 30.77 MB | +0.16 to +0.19 MB |
| SM201A | None | 15.25, 15.26 MB | 16.92, 15.30 MB | +0.04 MB on the second run; the first is an outlier (+1.67 MB, beside its own `All` row, 21.28 MB on one run and 23.09 MB on the other) |
| IX113A | Text | 76.48, 76.49 MB | 76.73, 76.73 MB | +0.24 to +0.25 MB |
| IX113A | All | 97.17, 97.18 MB | 97.26, 97.31 MB | +0.08 to +0.14 MB |

Reading them: the parent repeats itself within 0.1 MB on IX113A, NC218A and SM201A, and step 7a is above it on both runs by a
fixed amount that grows with the program (about 0.3 MB on IX113A, 0.2 MB on NC218A): the label fields. It is a cost of the
design's own mechanism (DESIGN-listing.md section 6.7.1 classifies each slot by its label), outside the budget's letter for the
`None` row ("the parent's plus the line-kind arrays"), so section 9 now names it beside the line-kind arrays, with this
measurement; no listing work runs without a listing. Time rows are not shown (kb/Work PB5927).

**Step 8 (kb/Work PB5925, sequence points, the PDB and the object code): the `None` row against the parent, and the `All` row.**
Step 8 changes every compilation: the Roslyn backend writes a line directive before each statement's first code and `#line hidden`
elsewhere, and a portable PDB beside the assembly; and `all` now holds the object-code map and the object code. Measured on
2026-11-18, Release, one Windows host, `dotnet run -c Release --project tests/WiseOwl.Cobol.Benchmarks -- --filter
'*CompileBenchmarks.FullCompile*'` on the `listing-step-8` branch after its last change and on its parent `ec1dc45947`, the
parent's in parentheses. Allocated bytes per full compile:

| Program | None | Text | TextAndJson | All | None over the parent | All over None |
|---|---|---|---|---|---|---|
| IX113A | 189.57 (171.16) MB | 207.87 (179.81) MB | 205.89 (187.27) MB | 256.88 (197.49) MB | +18.4 MB (+10.8 %) | +35.5 % |
| NC105A | 118.36 (107.13) MB | 119.26 (107.91) MB | 124.50 (113.25) MB | 160.93 (113.74) MB | +11.2 MB (+10.5 %) | +36.0 % |
| NC218A | 114.31 (97.58) MB | 118.43 (100.49) MB | 122.54 (106.05) MB | 158.49 (106.71) MB | +16.7 MB (+17.1 %) | +38.6 % |
| SM201A | 41.14 (37.77) MB | 42.69 (37.37) MB | 46.17 (41.52) MB | 56.11 (43.88) MB | +3.4 MB (+8.9 %) | +36.4 % |

Reading them: two probe runs of the branch split the `None` cost (temporary switches, reverted): with neither the directives nor
the PDB the full compile is the parent's (IX113A 170.30, NC218A 97.05 MB); the directives alone are most of it (IX113A 185.06,
NC218A 112.60 MB: Roslyn's handling of a directive pair per statement), and the PDB the rest. Reading each line's first sequence
point without a parse (`CodeWriter.SimpleStatement`) did not move the `None` row, so the cost is Roslyn's, not the classifier's.
Both are the design's decided mechanism (section 6.9: every compilation carries COBOL sequence points and a PDB, which a debugger
needs whether or not a listing is asked), so section 9's `None` budget now names them with this measurement. The `All` row is over
its 25 % budget: kb/Work PB5941 (the IL instructions of the object-code section); the first build of the section decoded each
stretch from its method's start and allocated 2,657 MB for NC105A, fixed in the step. The standard text listing (`Text`) is +0.8
to +9.7 % over `None`, inside its 10 % budget. Time rows are not shown (kb/Work PB5927).

**Step 6b, slice 1 (kb/Work PB5932, the cd-names and the file-names resolved by `SymbolTable.Identify`): the `None` row against
the parent.** Slice 1 changes how every compilation resolves a cd-name or a file-name, with or without a listing, so it
measures the no-listing path. Measured on 2026-11-18, Release, one Windows host,
`dotnet run -c Release --project tests/WiseOwl.Cobol.Benchmarks -- --filter '*CompileBenchmarks.FrontEndAndBind*'` twice on the
`listing-step-6b` branch (commit 82820eddff, main merged) and twice on its parent `bfb8500569`, alternating. Allocated bytes per
front end and bind, the two runs of each tree:

| Program | Listing | Parent | Slice 1 |
|---|---|---|---|
| IX113A | None | 68.88, 68.71 MB | 68.87, 68.91 MB |
| NC105A | None | 35.49, 34.90 MB | 35.24, 34.84 MB |
| NC218A | None | 30.78, 31.13 MB | 30.78, 31.36 MB |
| SM201A | None | 15.37, 15.30 MB | 15.31, 15.29 MB |
| IX113A | Text | 76.76, 76.66 MB | 76.71, 76.68 MB |
| IX113A | All | 97.26, 97.26 MB | 97.21, 97.35 MB |

Reading them: every slice-1 row lies within the parent's own spread or within 0.23 MB of it (NC218A's second run, where the
parent's two runs differ by 0.35 MB), so slice 1 moves no measurable allocation; the `None` budget holds. Time rows are not
shown (kb/Work PB5927). The later slices of step 6b each measure their own `None` row the same way.

**Step 6b, slice 2 (kb/Work PB5932, the tables keyed by no COBOL user word re-keyed, the procedure-names resolved by
`SymbolTable.Identify`): the `None` row against the parent.** Slice 2 changes how every compilation keys the intrinsic, implementor
and clause tables and resolves every procedure-name, so it measures the no-listing path. Measured on 2026-11-19, Release, one
Windows host, `dotnet run -c Release --project tests/WiseOwl.Cobol.Benchmarks -- --filter '*CompileBenchmarks.FrontEndAndBind*'`
twice on the `listing-step-6b-2` branch (commit 0dc23408fa) and twice on its parent `eac451c185`, alternating
(parent, branch, parent, branch). Allocated bytes per front end and bind, the two runs of each tree:

| Program | Listing | Parent | Slice 2 |
|---|---|---|---|
| IX113A | None | 69.18, 69.22 MB | 69.20, 69.13 MB |
| NC105A | None | 35.12, 35.08 MB | 35.10, 35.10 MB |
| NC218A | None | 31.05, 31.01 MB | 31.09, 31.06 MB |
| SM201A | None | 15.39, 15.37 MB | 15.44, 15.26 MB |
| IX113A | Text | 76.99, 77.02 MB | 76.97, 77.01 MB |
| IX113A | All | 97.59, 97.64 MB | 97.62, 97.69 MB |

Reading them: every slice-2 `None` row lies within the parent's own spread or within 0.07 MB of it (SM201A's first run, where
the branch's own two runs differ by 0.18 MB), so slice 2 moves no measurable allocation; the `None` budget holds. Time rows are
not shown (kb/Work PB5927).

**Step 6b, slice 3 (kb/Work PB5932, the constant-names resolved by `SymbolTable.Identify`): the `None` row against the parent.**
Slice 3 changes how every compilation reads a constant-name (every bare data reference asks whether it is one), so it measures the
no-listing path. Measured on 2026-11-19, Release, one Windows host, `dotnet run -c Release --project tests/WiseOwl.Cobol.Benchmarks --
--filter '*CompileBenchmarks.FrontEndAndBind*'` twice on the `listing-step-6b-3` branch (commit 43b05feca4) and twice on its parent
`9654bb00ec`, alternating (parent, branch, branch, parent: the parent's second run was refused by a locked build file and run again
after the branch's second). Allocated bytes per front end and bind, the two runs of each tree:

| Program | Listing | Parent | Slice 3 |
|---|---|---|---|
| IX113A | None | 69.28, 69.20 MB | 68.91, 68.98 MB |
| NC105A | None | 35.22, 35.15 MB | 35.64, 35.59 MB |
| NC218A | None | 31.08, 31.19 MB | 31.50, 30.91 MB |
| SM201A | None | 15.43, 15.38 MB | 15.38, 15.39 MB |
| IX113A | Text | 77.05, 77.04 MB | 76.68, 76.68 MB |
| IX113A | All | 97.66, 97.58 MB | 97.33, 97.29 MB |

NC105A's `None` rows sat 0.37 to 0.49 MB above the parent's, so its row alone was run twice more, alternating (`--filter
'*FrontEndAndBind*NC105A*None*'`, branch first): branch 34.90 and 34.95 MB, parent 35.78 and 35.22 MB.

Reading them: across its four runs each, NC105A's `None` row is 34.90 to 35.64 MB on the branch and 35.15 to 35.78 MB on the parent,
two ranges that overlap, so the first pair's difference was the harness's run-to-run spread (the same program's parent runs differed
by 0.59 MB at slice 1), not the slice. IX113A's `None`, `Text` and `All` rows are 0.2 to 0.4 MB below the parent's on every run,
NC218A's straddle the parent's and SM201A's equal it, so slice 3 moves no measurable allocation; the `None` budget holds. Time rows
are not shown (kb/Work PB5927).

**Step 6b, slice 4 (kb/Work PB5932, the CONFIGURATION SECTION names resolved by `SymbolTable.Identify`): the `None` row against the
parent.** Slice 4 changes how every compilation reads a configuration-section name (every bare word a literal position or a class
condition admits asks whether it is a symbolic-character, an alphabet-name or a class-name), so it measures the no-listing path.
Measured on 2026-11-19, Release, one Windows host, `dotnet run -c Release --project tests/WiseOwl.Cobol.Benchmarks -- --filter
'*CompileBenchmarks.FrontEndAndBind*'` twice on the `listing-step-6b-4` branch (commit 6397b7eac5) and twice on its parent
`c7088a17b9`, alternating (parent, branch, branch, parent). Allocated bytes per front end and bind, the two runs of each tree:

| Program | Listing | Parent | Slice 4 |
|---|---|---|---|
| IX113A | None | 68.94, 68.84 MB | 68.92, 68.88 MB |
| NC105A | None | 35.58, 34.92 MB | 35.09, 34.97 MB |
| NC218A | None | 30.88, 30.87 MB | 30.87, 30.87 MB |
| SM201A | None | 15.38, 15.66 MB | 15.39, 15.33 MB |
| IX113A | Text | 76.72, 76.66 MB | 76.70, 76.69 MB |
| IX113A | All | 97.33, 97.30 MB | 97.34, 97.31 MB |

Every branch row lies inside the parent's two runs or within 0.04 MB of them, so slice 4 moves no measurable allocation; the `None`
budget holds. Time rows are not shown (kb/Work PB5927).

**Step 6b, slice 5 (kb/Work PB5932, the REPORT SECTION names resolved by `SymbolTable.Identify`): the `None` row against the
parent.** Slice 5 changes what every data-name reference asks first (a sum counter resolves before the ordinary lookup), so it measures
the no-listing path. Measured on 2026-11-19, Release, one Windows host, `dotnet run -c Release --project tests/WiseOwl.Cobol.Benchmarks --
--filter '*CompileBenchmarks.FrontEndAndBind*'` on the `listing-step-6b-5` branch and on its parent `b039f2b67e`. The first build
(commit 59401eabfd, parent, branch, branch, parent) allocated more on every program, consistently: IX113A `None` 69.98 and 69.95 MB
against 68.92 and 68.95, NC218A 31.46 and 31.51 against 30.88 and 30.90. The moved sum-counter arm read the reference's word and its
qualifiers before asking whether any counter carries the name, for every data-name reference, probes included; commit 2fad1cf810 asks
the table first and reads them only for a name it holds. Re-measured (branch, parent, branch), allocated bytes per front end and bind:

| Program | Listing | Parent (three runs) | Slice 5 (2fad1cf810) |
|---|---|---|---|
| IX113A | None | 68.92, 68.95, 68.85 MB | 69.08, 68.98 MB |
| NC105A | None | 34.96, 34.96, 34.98 MB | 35.02, 35.00 MB |
| NC218A | None | 30.88, 30.90, 30.87 MB | 30.96, 31.61 MB |
| SM201A | None | 15.33, 15.38, 15.38 MB | 15.36, 15.41 MB |
| IX113A | Text | 76.73, 76.56, 76.76 MB | 76.82, 76.85 MB |
| IX113A | All | 97.20, 97.34, 97.44 MB | 97.44, 97.51 MB |

Every branch `None` row lies within 0.13 MB of the parent's runs but one: NC218A's second run, 31.61 MB, whose `Text` and `All` rows of
the same run lie inside the parent's (the parent's own NC218A `Text` row ran 35.18 to 35.94 MB). Slice 5 moves no measurable allocation
once the arm is fixed; the `None` budget holds. Time rows are not shown (kb/Work PB5927).

**Step 6b, slice 6 (kb/Work PB5932, the program-prototype-names resolved by `SymbolTable.Identify`): the `None` row against the
parent.** Slice 6 changes where every unit's program prototypes are made and read (the symbol table, once per element as before), so
it measures the no-listing path. Measured on 2026-11-19, Release, one Windows host, `dotnet run -c Release --project
tests/WiseOwl.Cobol.Benchmarks -- --filter '*CompileBenchmarks.FrontEndAndBind*'` twice on the `listing-step-6b-6` branch (commit
9ae07c433e) and twice on its parent `3dd8fd019b`, alternating (parent, branch, branch, parent). Allocated bytes per front end and bind,
the two runs of each tree:

| Program | Listing | Parent | Slice 6 |
|---|---|---|---|
| IX113A | None | 69.05, 69.09 MB | 68.98, 68.85 MB |
| NC105A | None | 34.99, 35.16 MB | 35.00, 35.12 MB |
| NC218A | None | 31.56, 30.99 MB | 31.14, 30.98 MB |
| SM201A | None | 15.35, 15.35 MB | 15.41, 15.34 MB |
| IX113A | Text | 76.83, 76.85 MB | 76.82, 76.86 MB |
| IX113A | All | 97.22, 97.50 MB | 97.44, 97.44 MB |

Every branch `None` row lies inside the parent's two runs or within 0.06 MB of them (SM201A's first run), and IX113A's lies below
both, so slice 6 moves no measurable allocation; the `None` budget holds. Time rows are not shown (kb/Work PB5927).

**Step 6b, slice 7 (kb/Work PB5932, the function-prototype-names resolved by `SymbolTable.Identify`): the `None` row against the
parent.** Slice 7 changes where every element's user-defined functions are made and read (the symbol table, made twice per element as
`BinderDriver.UserFunctionsOf` was, now always as the element's own table where a forest with no specifier shared the group's view), so
it measures the no-listing path. Measured on 2026-11-19, Release, one Windows host, `dotnet run -c Release --project
tests/WiseOwl.Cobol.Benchmarks -- --filter '*CompileBenchmarks.FrontEndAndBind*'` twice in a detached worktree of the slice's commit
(4525d45aae) and twice in one of its parent `84097a3e22`, alternating (parent, slice, slice, parent). Allocated bytes per front end and
bind, the two runs of each tree:

| Program | Listing | Parent | Slice 7 |
|---|---|---|---|
| IX113A | None | 69.05, 68.98 MB | 69.06, 69.00 MB |
| NC105A | None | 35.07, 34.99 MB | 35.66, 35.03 MB |
| NC218A | None | 30.95, 30.98 MB | 31.12, 31.56 MB |
| SM201A | None | 15.36, 15.21 MB | 15.35, 15.41 MB |
| IX113A | Text | 76.81, 76.81 MB | 76.83, 76.94 MB |
| IX113A | All | 97.51, 97.45 MB | 97.52, 97.55 MB |

IX113A's and SM201A's branch rows lie inside the parent's two runs or within 0.05 MB of them, and one run of NC105A and of NC218A does
too; the other two branch runs stand above this parent pair, NC105A's by 0.59 MB and NC218A's by 0.58 MB. Both are within the
run-to-run spread these programs show on one tree (the slice's own two NC105A runs are 0.63 MB apart, and slice 6's parent measured NC218A at 31.56 and 30.99 MB, 0.57 MB apart), and none of
the four programs declares a function, so the only new work on their path is two empty tables per element. Slice 7 moves no allocation
this measurement can tell from its noise; the `None` budget holds. Time rows are not shown (kb/Work PB5927).
**Step 6b, the OO lane (kb/Work PB5932, the object-class-names and interface-names resolved by the symbol table's compilation-group
tier, the method rosters keyed by the externalized method name, the PROPERTY specifiers): the `None` row against the parent.** The lane
changes how every class-name, interface-name and method roster is declared and looked up (a lookup keyed by `CobolName` or
`ExternalizedMethodName` where it was keyed by a string under the name comparer), so it measures the no-listing path. Measured on
2026-11-19, Release, one Windows host, `dotnet run -c Release --project tests/WiseOwl.Cobol.Benchmarks -- --filter
'*CompileBenchmarks.FrontEndAndBind*'` twice on the `listing-step-6b-oo` branch (commit f6cd542052) and twice on its parent `84097a3e22`,
alternating (parent, branch, branch, parent). Allocated bytes per front end and bind, the two runs of each tree:

| Program | Listing | Parent | OO lane |
|---|---|---|---|
| IX113A | None | 69.05, 68.99 MB | 69.07, 69.02 MB |
| NC105A | None | 35.11, 35.06 MB | 35.20, 35.04 MB |
| NC218A | None | 30.96, 31.56 MB | 30.94, 31.08 MB |
| SM201A | None | 15.35, 15.40 MB | 15.41, 15.21 MB |
| IX113A | Text | 76.82, 76.82 MB | 76.89, 76.82 MB |
| IX113A | All | 97.47, 97.47 MB | 97.51, 97.56 MB |

Every branch `None` row lies inside the parent's two runs or within 0.09 MB of them (NC105A's first run), so the lane moves no
measurable allocation; the `None` budget holds. None of the four programs declares a class, so the OO tables are empty in every row:
the rows measure what the lane adds to a compilation with no OO (one empty tier per class table, the per-element PROPERTY table).
Time rows are not shown (kb/Work PB5927).
---

## Not measured here

**The COBOL layer beyond the carrier.** `Compare_ShortStrings_LocaleCollation` (run 2) measures the
`CobolCollation` carrier's LOCALE arm; the table arms (`AlphanumericCollation` / `NationalCollation`) are not
benchmarked yet. SORT and MERGE are, end to end, by `SORTHOT` and `MERGEHOT` (numeric keys, so no collation), and
indexed-file key handling by `IDXHOT`.

**Numeric conversion in isolation.** `MOVEHOT` measures `MOVE` across the storage forms as a program does it; there is
no micro-benchmark of one conversion. Add one as a sibling folder (`tests/WiseOwl.Cobol.Benchmarks/<Subsystem>/`) reusing
`Collation/BenchmarkConfig.cs`, which is deliberately subsystem-neutral (`Unicode/`, `Compile/` and `GeneratedCode/`
already do).
