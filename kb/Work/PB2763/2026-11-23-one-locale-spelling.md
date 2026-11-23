# PB2763, 2026-11-23: one locale spelling, one resolution, one host surface (wave 20262154 group E — the open half)

**Re-probe (base 2d3d3c11e1, Debug build 0/0).** Through the runtime assembly from PowerShell:
`LocaleManager.GetLocale("es_es.UTF-8").Name` was `es-es.UTF-8` (the host's own trim-and-replace kept the codeset;
a COBOL `LOCALE … IS "es_ES.UTF-8"` normalizes to `es-es`), `CollationEngine.ResolveLocale("es_es").Tag` and
`ResolveLocale("es-ES").Tag` were both `es-es` (the cache, OrdinalIgnoreCase on the trimmed spelling, kept whichever
spelling came first), and the code read confirmed the rest: `LocaleCollation.ResolveNow` ran
`LocaleIdentification.IsAvailable` (Normalize + two `CldrLocaleTag.Parse` + three lookups) and `ForLocale` (a
`CollationOptions` per call through `Cached`) at every comparison; the culture probe was written three times
(`CollationEngine`, `LocaleConfig`, `LocaleFacts`); the tag rules (root/und, `_`, case, `-u-` split) in seven places.
COBOL probe `locprobe.cob` (named `IS LOCALE "es_es.UTF-8"` as PCS, SET LOCALE to `de_DE@phonebook` and `und`) printed
`es: n-tilde-a > nz` / `done` before and after.

**Fixed (the six merged findings but one).**
- `Runtime/Globalization/LocaleTag` is the one canonical spelling (`Parse`, `Base`, `Extension`, `Canonical`,
  `Language`, `IsWellFormed`) and the one culture probe (`PredefinedCulture`). `CldrLocaleTag.Parse` / `FileName`,
  `CldrLocaleLoader.Chain`, `CldrParser` (the JSON `locale` split, `[import]`), `TailoringRules.Candidates`,
  `LocaleFacts`, `LocaleIdentification.Normalize` (which keeps only the POSIX half: C/POSIX/INVARIANT, `.codeset`,
  `@modifier`) and the engine read it; `NormalizeBase`, `IndexOfExtension` and the other copies are deleted. Control
  and Globalization no longer reach into `Collation.Cldr` for a tag (LocaleState still asks it whether a modifier is a
  collation type, which is collation knowledge).
- `CollationEngine` caches each locale once per canonical tag (Ordinal); `ResolvedLocaleCollation` carries
  `IsAvailable` (the known-locale rule, decided once) and its `Collator` (built once); `IsKnownLocale` and the ORDER
  TABLE resolution read it. A caller holding the canonical spelling pays one lookup and no parse.
- `LocaleCollation` holds a named locale's resolution per cache generation (`CollationEngine.LocaleGeneration`,
  bumped by `ClearLocaleCache`); `LocaleFacts.For` takes the same canonical fast path.
- `LocaleManager` normalizes by L1 and returns the engine's `ResolvedLocaleCollation`; `LocaleInfo` (forwarders),
  `LocaleConfig` (alias constants, `IsSupported`, the cloned culture probe, `DefaultLocale`), the `s_infos` second
  cache, `LocaleManager.Normalize` / `ClearCache` / `CurrentCollator` are deleted (rule 4: every caller changed).
  `LocaleState.UserDefaultNow` is the one L2 read the host and `CollationRuntime.Describe` use.

**Found and fixed on the way (I9).** `es_ES@nosuchmod` was AVAILABLE: L1 says an `@modifier` that is no collation type
makes the locale unavailable, but the tag `es-ES@nosuchmod` found `es.tailor` through the tailoring lookup's language
fallback (`de_DE@foo` was correctly unavailable only because no `de.tailor` ships). A tag whose base subtags are not
RFC 5646 subtags (1–8 letters or digits) now names no locale (`LocaleTag.IsWellFormed`). The same rule makes the ORDER
TABLE literal `"es_ES.UTF-8"` (a literal-9 is a tag, never L1-normalized; design §4.9 "a CLDR locale tag") not a
supported table. Golden `12.3.7-SPECIAL-NAMES/pb2763_locale_modifier_availability` (EC-LOCALE-MISSING at SET LOCALE and
at LOCALE-COMPARE; `@traditional` orders "ch" > "cz", modern "ch" < "cz").

**Not done here (a behavior change, so a fix-lane lead, PB2119's contract): `runtime-text-1--duplication--f1#1`.**
The `.tailor` layer looks a tag up as "tag, then language" (`TailoringRules.Candidates`) while the CLDR layer walks CLDR
inheritance (`CldrLocaleLoader.Chain`: explicit parents `nb`→`no`, `es-MX`→`es-419`, script truncation). Making the
`.tailor` lookup walk the CLDR chain changes which site file applies (an `es-419.tailor` would start to apply to
`es-MX`, a `no.tailor` to `nb`), so it is a determination for the fix lane, not part of a unify wave. Both chains now
read `LocaleTag`, so the spelling half of that finding is done.

**Neutrality.** `python scripts/arch/compare_oracle.py --no-build`: `=== ARCH-ORACLE: DIFFERENT — 1 of 23506 cases
differ (baseline 2d3d3c11e1c0, …)`, the one being `ADDED suite/2002/pb2763_locale_modifier_availability` (the new
golden); no emitted C# or diagnostic stream changed. Runtime public API: `LocaleInfo`, `LocaleConfig`,
`LocaleManager.ClearCache` / `CurrentCollator` removed, `GetLocale` / `TryGetLocale` / `CurrentLocale` return
`ResolvedLocaleCollation`; `abi_pin.py repin`.
