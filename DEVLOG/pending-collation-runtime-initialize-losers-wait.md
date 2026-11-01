## CollationRuntime.Initialize: the one-shot latch is published after the work, and losers wait (PB380)

**Mechanism.** `CollationRuntime.Initialize` took its latch with `Interlocked.CompareExchange(ref s_initialized, 1, 0)`
BEFORE assigning `CollationKeyCache.DefaultConfig` from `COBOL_COLLATION_CACHE`. A concurrent caller lost the CAS,
returned at once with `IsInitialized == true`, and read the compiled-in 8,192-key default; a per-collator cache created
in that window kept that size for the life of the process (a resource defect: results stayed correct).

**Re-probe (wave build, before the fix).** New `CollationRuntimeInitializeTests`: 16 threads behind a `Barrier`, 300
trials, the latch reset by reflection between trials, `COBOL_COLLATION_CACHE=777`. 808 of 4,800 post-`Initialize` reads
saw 8192.

**Fix.** `Initialize` takes `s_initLock`, does the work (the environment's `CacheConfig` unless the host configured one)
and publishes `s_initialized` with `Volatile.Write` only afterwards; losers block on the lock; the fast path is a
`Volatile.Read`. `ConfigureCache` takes the same lock, so `s_hostConfigured` (a plain bool read across threads) is now
ordered with the work. The warm-up stays outside the lock and runs by the winner alone, because `Warmup` re-enters
`Initialize` and creates a run unit whose constructor does too (a `Lazy<T>` would have deadlocked or thrown on that
recursion, which is why the lock-and-publish shape was chosen over the note's `Lazy` suggestion).

**Guard.** The two tests of `CollationRuntimeInitializeTests` (the race, and a host `ConfigureCache` that the
environment must not overwrite); the race test fails against the old body (808 stale) and passes on the new.

**Sibling sweep.** `grep -rn CompareExchange src`: the only other site is `LocaleCollation.cs:80`, a value publish whose
losers adopt the winner's value (benign). `StandardStreams.EnsureUtf8` has the same one-shot shape (`Interlocked.Exchange`
before the work, so a concurrent caller returns before the streams are configured); outside this group, registered as a
lead.

**Rows.** None claimed or closed (`closes_rows: []`, process-only). PB380 flipped to `landed`.
