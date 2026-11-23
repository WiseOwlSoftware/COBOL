## Entry 4001 — 2026-11-23 12:04 PST — PB2763 finished: one locale spelling, one resolution, one host surface

The open half of PB2763 was six merged R2 findings about locale identification. The re-probe on the base showed the
split: the host's `LocaleManager.GetLocale("es_es.UTF-8")` answered a locale named `es-es.UTF-8` (it had its own
trim-and-replace), while a COBOL `LOCALE … IS "es_ES.UTF-8"` selects `es-ES`; `CollationEngine.ResolveLocale("es_es")`
and `("es-ES")` both came back tagged `es-es`, because the cache kept whichever spelling it saw first; and a named
`IS LOCALE` sequence re-checked availability (a normalization, two tag parses, three lookups) and built a
`CollationOptions` at every comparison.

**The fix.** `Runtime/Globalization/LocaleTag` is now the one canonical spelling of a tag (root/und, `_` for `-`,
subtag case, base against `-u-` extension) and the one .NET culture probe. The CLDR tag, its file names and parent
chain, the `.tailor` lookup, the CLDR parser's `[import]` and JSON identity, `LocaleFacts` and L1's
`LocaleIdentification.Normalize` all read it, and their own copies are deleted. The engine caches each locale once
per canonical tag, and the resolution carries its availability and collator, each decided once; `IsKnownLocale` and
ORDER TABLE resolution read that. A named locale sequence keeps its resolution until `ClearLocaleCache` moves the
generation. `LocaleManager` now normalizes by L1 and returns the engine's `ResolvedLocaleCollation`. `LocaleInfo`,
`LocaleConfig` and the second cache are gone.

**Found on the way.** `es_ES@nosuchmod` was available: L1 makes an unknown `@modifier` unavailable, but the tag found
`es.tailor` through the tailoring lookup's language fallback. A tag whose base subtags are not RFC 5646 subtags now
names no locale. Golden `12.3.7-SPECIAL-NAMES/pb2763_locale_modifier_availability` pins it: EC-LOCALE-MISSING at
SET LOCALE and at LOCALE-COMPARE, plus `@traditional` ordering "ch" after "cz" where modern Spanish does not.

**Left for the fix lane.** The `.tailor` layer still looks a tag up as "tag, then language" while CLDR walks its own
inheritance. Unifying them changes which site file applies, so it is a behavior change and is reported as a lead.

**Evidence.** The locale unit tests pass (448 of 448 in the Collation/Locale/RuntimeConfig/ProcessGlobal/ABI filter,
with the new `LocaleTagTests` and two new `LocaleManagerTests`). `compare_oracle.py`: 1 of 23506 cases differ, the one
added golden. `semgrep/verify.py`: PASS, no count moved. Runtime ABI repinned.

