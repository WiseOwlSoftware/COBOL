---
order: 6
---
**The locale facility (Annex A.4.9 optional module) — CLAIMED WHOLE** since kb/Work PB64 T6 (2026-08-28; owner
decision Q1, 2026-08-18): all thirteen optional elements are implemented — the last two, item 8 (PICTURE format 2)
and item 12 (the `LOCALE` keyword of `NUMVAL-C`/`TEST-NUMVAL-C`), landed with T6 — so this facility is NO LONGER a
non-support item; the ⚖ determination blocks below are the module's §2023:4.2.7 user documentation and stay here as the
claimed-module section. The **COBOL1518** by-name refusal is DELETED with the claim (its code is never
reallocated).

   > ⚖ **A.4.9 items 8 and 12 — PICTURE format 2 (§2023:13.18.40.2) and the `LOCALE` keyword of `NUMVAL-C` /
   > `TEST-NUMVAL-C` (§2023:15.68.3 r5 / §2023:15.94.3 r1) — ARE IMPLEMENTED** (kb/Work PB64 T6, increment T6 of
   > `docs/rearchitecture/DESIGN-locale-facility.md` §4.6). A format-2 item is fixed-point numeric-edited
   > (§2023:13.18.40.4 GR16) of exactly SIZE integer-1 character positions (GR17 — the picture is not the field size);
   > its editing/de-editing is the named-else-current locale's LC_MONETARY AT EACH operation (§2023:13.18.40.5 r9–r15;
   > `CobolLocaleEdit`), EC-LOCALE-SIZE raised per r14 b (character-based — a truncated currency character,
   > separator or sign raises; a truncated zero or suppressed-zero space does not; golden
   > `2002/pb64t6_locale_size_ec`); the NUMVAL-C family's LOCALE arm reads argument-1 under the same LC_MONETARY
   > model (`MonetaryFacts` — ONE snapshot for editing and recognition), §2023:15.94.4's error positions ordinal in the
   > original argument. **Determinations (documented per §2023:4.2.7):**
   > `currency_symbol` = the .NET culture's currency symbol and is what an EDIT emits (recognition also matches the
   > first three characters of `int_curr_symbol` per r5b.3); `int_curr_symbol` = `RegionInfo.ISOCurrencySymbol` +
   > one space, and is ABSENT for a neutral or invariant culture (the international alternative can then never
   > match); `int_frac_digits` has no .NET carrier and equals `frac_digits`; `positive_sign` = "+" (.NET's numeric
   > positive sign standing in — POSIX locales usually leave it empty); `p_sign_posn` = 1 flat (.NET's positive
   > currency pattern carries no sign slot; never mirrored from `n_sign_posn`, whose invariant value is 0 —
   > parentheses); the placement conventions (`*_cs_precedes`, `*_sep_by_space`, `*_sign_posn` — the latter two
   > reach COBOL only through §2023:8.2.1's ISO/IEC 9945:2009 clause-7 incorporation) are DERIVED AT RUNTIME from
   > `CurrencyPositivePattern`/`CurrencyNegativePattern` by a probing round-trip that THROWS on an unreproducible
   > layout (this runtime accepts SEVENTEEN negative patterns, 0–16; the derived range is asserted against the
   > runtime's accepted maximum by `MonetaryFactsTests`); seven POSIX placement renderings are unreachable from
   > .NET's patterns — a documented limit under §2023:8.2.1's own licence ("The format and implementation of locales
   > may differ from those specifications provided that logically-equivalent functionality is supported");
   > grouping separators are validated by IDENTITY, digit-flanking AND the `mon_grouping` group SIZES (§2023:15.68.3
   > r5b.6 names both fields — kb/Work PB835): a digit string with no separator is admitted (r5b.6 "may
   > contain"), and once one appears EVERY `mon_grouping` position carries one — **⚖ DETERMINATION**, the
   > reading "each separator at SOME legal boundary" is rejected; TEST-NUMVAL-C reports the first character no
   > grouped completion admits (a valid-but-short grouping at the character that ends it, or LENGTH+1 — §2023:15.94.4
   > r1 b/c); the §2023:14.6.13.2 r4 de-edit of a format-2 item holds its content to the SAME walk exactly (every
   > position, as §2023:13.18.40.5 r12 writes them); the fraction-digit count of argument-1 is not constrained by
   > `frac_digits` (§2023:15.68.1 names "the grouping separator and the decimal separator permitted"; r5b.5 is a
   > permission — the strict reading rejects legal source); spaces are
   > admitted at every token adjacency (a superset of the three `sep_by_space` values); `frac_digits` is NEVER an
   > editing input — §2023:13.18.40.5 r12 hands the locale only the separators and group sizes, the fraction width is
   > the picture's; §2023:13.18.40.4 GR19's format-validation leg fires only inside a VALIDATE statement and rides the
   > unclaimed VALIDATE facility (A.4.14, §4 item 3); ANYCASE folds ONLY the currency-string comparison, through
   > the LOCALE's own LC_CTYPE (r5b.1/3 — with `LOCALE` written and locale-name-1 omitted, the cross-reference's
   > nonexistent name is read as the same current locale r5a selects, an editorial-gap determination).
   > **DETERMINATION L12** (generalizing L10): every LC_TIME pattern and LC_MONETARY string is normalized —
   > Unicode Cf characters removed, U+00A0/U+202F/U+2009 mapped to the plain space (host-ICU stability: monetary
   > strings occupy character positions of a fixed-width item, so a host-varying byte would move EC-LOCALE-SIZE
   > itself; U+2212 is kept — a real character); recognition matches through the same equivalence — ONE
   > per-character fold (`LocaleFacts.TryFoldLocaleChar`) normalizes the snapshot strings AND argument-1 of NUMVAL-C /
   > TEST-NUMVAL-C LOCALE before the §2023:15.68.3 r5 b scan, so a Cf character in the argument is ignorable (an ar-*
   > culture's own formatted currency string, U+200F after the symbol included, is conforming), U+00A0/U+202F/U+2009
   > is a space wherever r5 b 7 or a token adjacency admits one, and §2023:15.94.4's positions stay the original
   > argument's ordinals (kb/Work PB2766). **L12 covers
   > EVERY locale-sourced string a result carries, not only the patterns** (kb/Work PB2764): `TimeFacts` (the LC_TIME
   > snapshot, the twin of `MonetaryFacts`) normalizes the date and time separators, the AM/PM designators, the era, the
   > day and month names and the seconds fraction's decimal point once and renders `d_fmt` / `t_fmt` itself, so ICU's
   > U+200F inside every ar-* date separator and U+00A0 inside the `a. m.` / `p. m.` of es-US, es-CO, es-DO, es-PA,
   > es-PH, es-PR, es-VE, yrl-CO and yrl-VE can no longer reach a result through a pattern L10 had already cleaned.
   > **DETERMINATION L13:** the date rendered is the GREGORIAN date argument-1 names (§2023:15.52.3 r2: "a date in the same
   > format as the year, month, and day returned in character positions 1 through 8 by the CURRENT-DATE function") and
   > `d_fmt` formats it (§2023:15.52.4 r2) — ISO/IEC 9945's `d_fmt` is a strftime format of a Gregorian broken-down time,
   > its alternative-era rendering being the separate `era_d_fmt` — never the culture's own calendar: .NET defaults fa-*,
   > ps-*, ckb-IR, lrc, mzn and uz-Arab to the Persian calendar, th-* to the Thai Buddhist and ar-SA to Umm al-Qura, so
   > `LOCALE-DATE("20261008")` read 1405/7/16 under fa-IR and 8/10/2569 under th-TH; it is 2026/10/8 and 8/10/2026.
   > Day and month names and the era come from a Gregorian clone of the culture's format data. `LocaleTextNormalizationDriftTests`
   > holds all three: no format character or NBSP/narrow/thin space in any predefined culture's result, every pattern
   > letter a token the renderer handles, and no product source outside the snapshot classes reading a culture's raw
   > `DateTimeFormat` / `NumberFormat`.

   > ⚖ **A.4.9 items 2–5 — `LOCALE-COMPARE` (§2023:15.51), `LOCALE-DATE` (§2023:15.52), `LOCALE-TIME` (§2023:15.53),
   > `LOCALE-TIME-FROM-SECONDS` (§2023:15.54) — ARE IMPLEMENTED** (owner decision Q1; kb/Work PB64, increment T4 of
   > `docs/rearchitecture/DESIGN-locale-facility.md` §4.7/§4.8, 2026-08-19; refused by name with COBOL1518 until
   > then). `LOCALE-COMPARE` IS the locale-based relation comparison (§2023:8.8.4.2.11 through the ONE `LocaleCollation`
   > carrier — r2's trailing-space truncation, r4 the cultural ordering, r5/r6 the one-character result); the three
   > time functions format per the locale's `d_fmt` / `t_fmt` (§2023:15.52.4 r2 / §2023:15.53.4 r2 / §2023:15.54.4 r2) with
   > §2023:15.53.3 r3's own ranges (hours 00–24, seconds 00–99) and the §2023:7.3.17 standard-numeric-time-form screen; the
   > optional `locale-name-1` is a SPECIAL-NAMES LOCALE clause's name (§2023:15.3 argument type 8 — COBOL1664
   > otherwise), else the locale CURRENT for LC_COLLATE / LC_TIME (§2023:14.6.6 r7/r8). **Determinations (documented per
   > §2023:4.2.7; DETERMINATION L10):** `d_fmt` is the .NET culture's SHORT DATE pattern and `t_fmt` its LONG TIME pattern
   > (the one carrying seconds), read once per locale by `LocaleFacts` — the ONE place a `CultureInfo` is read — from
   > the locale tag's nearest predefined .NET culture (the tag, then its ancestors); **the U+202F / U+2009 spacing
   > newer ICU releases put inside those patterns normalizes to the PLAIN SPACE** (kb/Work PB112 — host ICU versions
   > differ, and the same program must print the same bytes on every host; ISO 9945's locale data and GnuCOBOL's
   > strftime rendition both use the plain space); a scaled
   > `LOCALE-TIME-FROM-SECONDS` argument carries its fraction into the seconds (Annex D.31.4.5's nanosecond note);
   > the result length is run-time-determined (§2023:15.52.4 r3 / §2023:15.53.4 r3 / §2023:15.54.4 r3 — a dynamic-length string).
   > **`EC-LOCALE-INVALID` is raised** (§2023:8.2.1 "invalid or incomplete") when an available locale has no culture data
   > for the category an operation needs — no predefined .NET culture backs the tag, or the process runs in .NET
   > invariant globalization mode (detected once) — the invariant culture's content standing when checking is off;
   > `EC-LOCALE-MISSING` for an unavailable named locale (the root's answer standing). The construct rows
   > `locale-functions-2002` / `locale-time-from-seconds-2014` gate the four (the 2014 edge is the catalog's
   > provisional window, kb/Work R28).

   > ⚖ **A.4.9 items 6, 7 and 13 — the OBJECT-COMPUTER `CHARACTER CLASSIFICATION` clause (§2023:12.3.6) and the
   > `LOCALE` phrase of `LOWER-CASE` (§2023:15.57) / `UPPER-CASE` (§2023:15.97) — ARE IMPLEMENTED** (owner decision Q1;
   > kb/Work PB64, increment T5 of `docs/rearchitecture/DESIGN-locale-facility.md` §4.5, 2026-08-19; the clause was
   > refused by name — PB78 — and the phrases — PB25 — until T5). `CHARACTER CLASSIFICATION [IS] locale-phrase-1
   > [locale-phrase-2] | FOR ALPHANUMERIC IS … | FOR NATIONAL IS …`, each phrase `locale-name | LOCALE |
   > SYSTEM-DEFAULT | USER-DEFAULT` (§2023:12.3.6.2; §2023:12.3.6.3 SR3 — COBOL1664 for an undeclared name; at most once —
   > COBOL2104 `choice-alternative-repeated`, counted by the `ClauseCardinalities` OBJECT-COMPUTER row, §2023:5.2.6.4; inherited by contained units, §2023:12.3.6.4 GR1), is RESOLVED AT EACH ACTIVATION of the module
   > (§2023:12.3.6.4 GR5 a–j; GR8 "effective with the initial state of the runtime modules"; §2023:14.6.6 r2) into the module's
   > classification — `LOCALE` is the locale current at THAT activation, and a later `SET LOCALE LC_CTYPE` does not
   > move it. Its two consumers (GR7): `UPPER-CASE` / `LOWER-CASE` without a phrase map through the classification
   > locale's LC_CTYPE (§2023:15.97.4 r3 / §2023:15.57.4 r3), and the class tests `ALPHABETIC` / `ALPHABETIC-LOWER` /
   > `ALPHABETIC-UPPER` classify by it (§2023:8.8.4.4.4 GR3 b1 / c1 / d1); with the `LOCALE locale-name-1` phrase the
   > functions map through the NAMED locale's LC_CTYPE whatever the classification (r2 — the construct row
   > `case-function-locale-phrase-2014`); without either, the implementor's correspondence (r4 — the invariant
   > tables, §3 above). **Determinations (documented per §2023:4.2.7):** (a) **DETERMINATION L9 — the LC_CTYPE case
   > correspondence is SIMPLE (one code unit to one)**: .NET's `TextInfo.ToUpper`/`ToLower`, which ISO/IEC 9945's
   > `toupper`/`tolower` also are (§2023:8.2.1 — categories "as specified in ISO/IEC 9945"), so the returned string always
   > has argument-1's length (§2023:15.97.4 r5 / §2023:15.57.4 r5 — the "may be longer or shorter" sentence is a permission a
   > one-to-one correspondence does not exercise; `ß` stays `ß`, never `SS`) and a letter without a correspondence is
   > unchanged (r6). (b) **The classification's LC_CTYPE is the .NET culture of the locale tag** (nearest predefined
   > culture, as LC_TIME — `LocaleFacts`): `ALPHABETIC` under a classification is "characters identified as alphabetic
   > in LC_CTYPE" = a Unicode LETTER (`Rune.IsLetter` — POSIX `alpha`), `ALPHABETIC-UPPER` / `-LOWER` a letter that is
   > uppercase / lowercase or that the locale's case mapping lowers / uppers to something else (the round-trip —
   > Turkish dotted/dotless I are classified as the locale says); **and, exactly as the rule reads, SPACE is NOT
   > alphabetic under a classification locale** — §2023:8.8.4.4.4 GR3 b2 (no locale) lists "space" explicitly, b1 (a locale
   > in effect) names only the characters LC_CTYPE identifies as alphabetic, and POSIX `alpha` excludes space; so
   > `"ab cd" IS ALPHABETIC` is TRUE without a classification and FALSE under one (golden
   > `2002/pb64t5_character_classification`). (c) **A class condition's alphabet-name-1 shall not be a LOCALE
   > alphabet** (§2023:8.8.4.4.3 SR2 — COBOL1669 `locale-alphabet-not-a-charset`, for an alphabet of either class): a
   > LOCALE alphabet defines a collating sequence only (§2023:12.3.7.4 GR7 Table 6), so the "class test specifying an
   > alphabet-name that is associated with a locale" that GR7 b) mentions is a reference the standard's own SR2
   > forbids — WiseOwl COBOL follows SR2. (d) **A DECLARED but UNAVAILABLE classification locale compiles** (L1 —
   > availability is a run-time property) and every operation that requires it — a class test, a case function without
   > a phrase — raises **`EC-LOCALE-MISSING`** at use through the ONE §2023:8.2.1 gate (`LocaleFacts.Require`; an available
   > locale without culture data for LC_CTYPE raises `EC-LOCALE-INVALID` the same way), the coded character set's
   > behavior standing when checking is off (§2023:14.6.13.1.3 #8 — golden `2002/pb64t5_classification_unavailable`);
   > `EC-LOCALE-INVALID` checking therefore rides every statement of a module with a classification. The construct
   > rows `character-classification-2002` / `case-function-locale-phrase-2014` gate the clause at 2002 and the phrase at 2014 (kb/Work PB4311: the printed 2002 formats of UPPER-CASE / LOWER-CASE carry no phrase).

   > ⚖ **A.4.9 item 9 (`SET LOCALE`, formats 11/12) and item 10's CLAUSE half (the SPECIAL-NAMES `LOCALE` clause) ARE
   > IMPLEMENTED, and so is the ALPHABET clause's NAMED `IS LOCALE locale-name-2` form** (owner decision Q1; kb/Work
   > PB64, increment T1 of `docs/rearchitecture/DESIGN-locale-facility.md`, 2026-08-19; they were refused by name
   > with COBOL1518 — PB25 / PB92 / PB100 — until T1). `LOCALE locale-name-1 IS {external-locale-name-1 |
   > literal-4}` declares a locale-name (§2023:12.3.7.2; SR10/SR11 checked; repeatable; inherited by contained units per
   > §2023:12.3.7.4 GR1); `SET LOCALE {category… | USER-DEFAULT} TO {identifier-10 | locale-name-1 | USER-DEFAULT |
   > SYSTEM-DEFAULT}` (§2023:14.9.39.4 GR22–GR25 — the category operand is a SET, per the format's choice indicators;
   > SR25–SR27 diagnosed) and `SET identifier-11 TO LOCALE {LC_ALL | USER-DEFAULT}` (GR26/GR27; SR28) act on the run
   > unit's ONE locale state (§2023:8.2.1 / §2023:14.6.6 r1/r3/r9; §2023:14.9.39.4 GR25). **Determinations (documented per §2023:4.2.7):**
   > the external identification is a locale TAG — any CLDR locale (BCP-47) or the root spelled `INVARIANT` — with
   > POSIX spellings normalized (`fr_FR.UTF-8` ≡ `fr_FR` ≡ `fr-FR`; an `@modifier` that is a CLDR collation type
   > becomes `-u-co-type`, any other makes the locale unavailable), and availability is a RUN-TIME property — the
   > compiler never resolves it (§2023:8.1.5; L1); a saved locale is a MANAGED HANDLE held by the data-pointer, never an
   > address (L4), and a locale is a value PER CATEGORY (a saved snapshot may name different locales per category);
   > the system default cannot be set from COBOL (§2023:8.2.1); a callee's SET is not unwound (§2023:14.6.6 r9); a SORT/MERGE
   > takes its collating sequence when the statement begins (§2023:14.6.6 r5 — a SET LOCALE in an INPUT PROCEDURE has no
   > effect on it). **The EC-LOCALE family of exception-names is LEGAL at every naming site** (item 1 — the PB100
   > refusal is reverted) and four of the conditions are RAISED: `EC-LOCALE-MISSING` (GR24, a named `IS LOCALE`
   > sequence whose locale is unavailable, and the LOCALE functions' locale-name-1), `EC-LOCALE-INVALID-PTR`
   > (GR21), `EC-LOCALE-INCOMPATIBLE` (§2023:8.8.4.2.11, L6) and `EC-LOCALE-INVALID` (§2023:8.2.1, the LOCALE functions — T4)
   > — each checking-gated per §2023:14.6.13.1.1, fatal per §2023:14.6.13.1.6's table, observed by the goldens
   > the suite tests `pb64t1_*` / `pb64t4_*` (`conformance-suite/cases/`). `EC-LOCALE-SIZE` is RAISED since T6 (§2023:13.18.40.5 r14 b — the ONE raise site is `CobolLocaleEdit.Format`,
   > observed by `2002/pb64t6_locale_size_ec`, both silent arms pinned);
   > `EC-LOCALE-IMP` is reserved. The construct rows
   > `special-names-locale-2002`, `set-locale-2002`, `set-save-locale-2002` gate the three at the 2002 edition.

   > ⚖ **A.4.9 item 10's ALPHABET half IS IMPLEMENTED for the bare form — `ALPHABET name [FOR ALPHANUMERIC|NATIONAL]
   > IS LOCALE`** (owner decision Q1; kb/Work PB101, increment T3 of the locale design). Without a locale-name it is
   > the collating sequence of the locale CURRENT at each use (§2023:12.3.7.4 GR7e); as the PROGRAM COLLATING SEQUENCE, a
   > SORT/MERGE or a file COLLATING SEQUENCE it makes the comparisons LOCALE-BASED per §2023:8.8.4.2.11 (trailing spaces
   > truncated, an all-space operand one space, no padding; then the LC_COLLATE algorithm), and MAX/MIN, ORD/CHAR,
   > HIGH-/LOW-VALUE follow through the one `CobolCollation` carrier. **Determinations (documented per §2023:4.2.7):**
   > the current locale is the run unit's LC_COLLATE locale — the user default from `COBOL_USER_LOCALE` (else the
   > process culture, else the root) until a SET LOCALE (the T1 increment) changes it (L2); the LC_COLLATE algorithm
   > is the locale's CLDR collation at its CLDR defaults — its tailoring over the root table at tertiary strength
   > with non-ignorable punctuation (L11); the collation is WiseOwl COBOL's own derived table generated from Unicode
   > CLDR release-48-2 / UCA 17.0.0 data (never the host's ICU), so the order is identical on every host — the same
   > verbatim conformance statement as item 11; `EC-LOCALE-INCOMPATIBLE` is set for an operand that is not
   > well-formed UTF-16 (an unpaired surrogate) — every well-formed code point is ordered (L6); ORD/CHAR positions
   > are the materialized rank of each native code unit under the locale's collation, equal-collating units sharing
   > a position (L7); HIGH-VALUE is U+FFFF and LOW-VALUE U+0000 under a locale sequence. The NAMED form (`IS LOCALE
   > locale-name-2`) landed with the LOCALE clause (T1, above): the sequence of THAT locale, resolved — and
   > EC-LOCALE-MISSING if unavailable — at use. See `src/WiseOwl.Cobol.Runtime/Collation/README.md` and the design's §4.4.

   > ⚖ **A.4.9 item 11 IS IMPLEMENTED — `STANDARD-COMPARE` and the `ORDER TABLE` clause** (owner decision Q4,
   > 2026-08-18; kb/Work PB101, increment T7 of `docs/rearchitecture/DESIGN-locale-facility.md` §4.9). The
   > conformance statement WiseOwl COBOL makes, verbatim: **"Implements collation behavior consistent with ISO/IEC
   > 14651 through derived tables and CLDR/UCA data."** §2 row 25 (A.3 item 25) carries the same claim and the
   > details; `src/WiseOwl.Cobol.Runtime/Collation/README.md` is the engine's design and legal posture. Three
   > consequences for this list: the function is no longer COBOL1518, the SPECIAL-NAMES `ORDER TABLE
   > ordering-name-1 IS literal-9` clause parses and binds (§2023:12.3.7.2; §2023:12.3.7.3 SR9/SR10/SR11; §2023:12.3.7.4 GR17),
   > and **`EC-ORDER-NOT-SUPPORTED` is a legal exception-name again** at every naming site — it is an A.3 item 25
   > name, not an A.4.9 locale one, and §2023:15.85.4 r2's raise site is live, so refusing it would make a condition
   > this compiler actually raises unobservable.
   >
   > ⚖ **DETERMINATION — the value of `STANDARD-COMPARE` when EC-ORDER-NOT-SUPPORTED checking is OFF.**
   > §2023:14.6.13.1.1 does not raise an unchecked condition and §2023:14.6.13.1.3 #8 leaves to the implementor "whether or
   > not execution will continue, how it will continue, and how any receiving operands are affected". WiseOwl COBOL
   > continues and returns `"="` — a value §2023:15.85.4 r6 defines, one character long per r7, the same for every
   > invocation. An unresolvable `literal-9` is diagnosed at compile time (COBOL1662, a warning) so this
   > outcome is never a surprise.

   > ⚖ **DETERMINATION — `NUMVAL-C`'s `LOCALE` phrase, which A.4.9 does not itself list** (owner decision
   > 2026-08-03; fix-queue PB37). A.4.9 enumerates thirteen optional locale elements and names the `LOCALE`
   > keywords of `LOWER-CASE` (item 6), `TEST-NUMVAL-C` (item 12) and `UPPER-CASE` (item 13) individually, but
   > **not `NUMVAL-C`'s**. WiseOwl COBOL reads that omission as **editorial**, so `NUMVAL-C`'s `LOCALE` phrase is an
   > optional element of this module — the reading under which T6's implementation of BOTH functions' LOCALE
   > keyword completes the module's claim (it licensed the documented non-support while that stood).
   > Grounds: the `NUMVAL-C` and `TEST-NUMVAL-C` general formats are character-identical, and **§2023:15.94 rule 1
   > states that the `TEST-NUMVAL-C` argument rules ARE §2023:15.68's** — so the alternative reading has A.4.9 making
   > item 12 optional while leaving mandatory the very rules item 12 delegates to. Reading a whole optional module
   > as excluding one keyword of one function, on an omission the standard nowhere states, would also be the only
   > reading under which a processor could claim A.4.9 non-support and still owe `NUMVAL-C` locale parsing.
   > **And the annex says so itself — §2023:A.4.1's catch-all is the ground this determination should have rested on
   > from the start:** *"This list identifies features, statements, formats (or parts of formats), clauses, or
   > phrases that are optional. **Any associated syntax rules, general rules, other rules, exception conditions,
   > and I-O status values are also optional, even if not explicitly listed.**"* §2023:15.94 r1 makes §2023:15.68's argument
   > rules the associated rules of item 12, so they are optional by A.4.1's own terms and no editorial-gap
   > inference is needed to reach the same place. The inference is kept because it is independently sound and
   > because it explains WHY the list looks incomplete; A.4.1 is what makes the conclusion not depend on it.
