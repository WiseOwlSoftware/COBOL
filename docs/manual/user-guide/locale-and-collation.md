---
title: Locale and collation
description: The default collating sequence, how to order text by locale, and how the user and system default locales are chosen.
sidebar:
  order: 5
---

WiseOwl COBOL claims the Annex A.4.9 optional module, locale support and the related functions, in full (conformance
statement section 5). This page covers the parts a user has to decide on: the order text sorts in, and which locale is
current.

## The default collating sequence

Unless a program names another, text compares and sorts in code-unit order. The native character set is UTF-16, one code
unit per character position, and a character's ordinal position is its code-unit value plus one: `"A"` is ordinal 66
and X"FF" is ordinal 256 (Annex A.1 item 8, ISO/IEC 1989:2023 section 12.3.7.4 general rule 6). `FUNCTION ORD` and
`FUNCTION CHAR` use these ordinals.

Two consequences matter in practice: every uppercase ASCII letter sorts before every lowercase one, and `ñ` (U+00F1)
sorts after `z`.

## Ordering by locale

To compare and sort by the rules of a locale, define an alphabet whose collating sequence is the locale's and name it as
the program collating sequence:

```cobol
       OBJECT-COMPUTER. GENERIC-BOX
           PROGRAM COLLATING SEQUENCE IS BY-LOCALE.
       SPECIAL-NAMES.
           ALPHABET BY-LOCALE IS LOCALE.
```

When `LOCALE` is written with no locale name, the sequence is the one defined by the locale that is current when the
sequence is used (section 12.3.7.4 general rule 7 e). The same alphabet can be named as the COLLATING SEQUENCE of a
SORT, a MERGE or a file. The locale functions (`LOCALE-COMPARE`, `LOCALE-DATE`, `LOCALE-TIME`, `LOCALE-TIME-FROM-SECONDS`), the
`LOCALE` phrase of `UPPER-CASE` and `LOWER-CASE`, locale editing PICTUREs and `SET LOCALE` are all supported.

For example, a program that compares `"ñu"` with `"nz"` under such an alphabet answers according to the current
locale: in a Spanish locale `ñ` is a letter after `n`, so `"nz"` sorts first; in a German locale `ñ` is `n` with an
accent, the letters `u` and `z` decide, and `"ñu"` sorts first.

## Which locale is current

A run unit starts with its current locale set to the user default locale (section 14.6.6 general rule 1).
`SET LOCALE LC_ALL TO SYSTEM-DEFAULT` and `SET LOCALE LC_ALL TO USER-DEFAULT` switch to the system default and back
(section 14.9.39.4). Section 8.2.1 requires the implementor to say how the two defaults are defined (Annex A.1 item 118),
and WiseOwl COBOL defines them this way:

| Default | Taken from, in order |
|---|---|
| user default | the environment variable `COBOL_USER_LOCALE`; else the culture of the process; else the root locale |
| system default | the environment variable `COBOL_SYSTEM_LOCALE`; else the installed UI culture of the host; else the root locale |

A variable's value is a locale name such as `es-ES`, `de-DE` or `es_ES.UTF-8`; surrounding spaces are ignored, and `C`
names the root locale. Compiling the comparison program above and running it with different values shows the effect:

```text
$ dotnet loc.dll                              (no variables set)
n-tilde sorts first
$ COBOL_USER_LOCALE=es-ES dotnet loc.dll
nz sorts first
$ COBOL_USER_LOCALE=de-DE dotnet loc.dll
n-tilde sorts first
```

(The first run reflects the culture of the machine it ran on.) A .NET host that changes `CultureInfo.CurrentCulture`
while a run unit is active does not change that run unit's locale: the run unit read the culture once, when it began,
and Annex A.1 item 120 records that a switch by a non-COBOL runtime module is not utilized, in any locale category. The
next run unit starts from the culture then in effect.

Where the machine has no culture data (the .NET invariant globalization mode), both defaults are the root locale.
