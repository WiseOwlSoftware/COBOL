---
title: The source listing
description: When the compiler writes a source listing, what it holds, how it is paginated, its JSON form and schema, and the options that control it.
sidebar:
  order: 10
---

The compiler writes a **source listing**: every physical line of the source file and of each COPY library text, in the
order the compiler read them, paginated for reading or printing, and on request the same listing as a JSON document for
programs to read.

- At `--std 85` the listing is written by default. COBOL-85 requires each compiler to produce "an output listing of the
  source program in reference format" (ANSI X3.23-1985 section IV.7.1). `--listing off` turns it off.
- At `--std 2002`, `2014` and `2023` the standard leaves the listing to the implementor (ISO/IEC 1989:2023 section
  7.3.18.3 general rule 1), and the compiler writes one only on request: `--listing`, or any other listing option below.

For `cobol prog.cob --std 85 -o out/prog.dll` the listing is `out/prog.lst`, beside the assembly. A listing is written on
every outcome once the source was read, including a compilation that fails: the listing of a program that does not
compile is often the one most worth reading. A source file that does not exist has no listing.

## Options

| Option | Effect |
|---|---|
| `--listing [on\|off]` | write a listing (a bare `--listing` is `on`); default `on` at `--std 85`, `off` at the later editions |
| `--listing-output <BASE>` | write the listing to `BASE.lst` (and `BASE.lst.json`); `-` writes it to standard output instead of a file, in the one format requested. Default: the output assembly's path without its extension |
| `--listing-format text\|json` | a format to write: `text` is the paginated listing `BASE.lst`, `json` the JSON listing `BASE.lst.json` (below). Repeat it for both (`--listing-format text --listing-format json`); default `text`. With `--listing-output -` exactly one format may be named |
| `--listing-page-length <N>` | lines per page, the four heading lines included (default 60); `0` paginates only where a page must start (below). At least 5 otherwise |
| `--listing-page-width <N>` | columns per page, 100 to 255 (default 132) |

Each listing option other than `--listing off` asks for a listing at any edition. A value outside its range is a
command-line error, reported before anything is compiled (exit status 1), never a COBOL diagnostic.

## What a page holds

Each page begins with four heading lines: the compiler and its version, the edition (for example
`ANSI X3.23-1985 (--std 85)`), the name of the program the page's first line belongs to, the compilation date and time,
and the page number; then `SOURCE` and the source path; a blank line; and a ruler that numbers the 80 source columns.
No heading line is wider than the page. When the first one does not fit, the compiler's version is shortened first, then
the edition; the date and time and the page number are always printed whole, and so is the program name unless it is
longer than 60 characters on a narrow page. A source path too long for the second line keeps its end (the file's name)
after `...`. A `COPY` banner too long for the 80 source columns continues on rows marked `+`, as a long source line does.

The program name follows the program-name rule of COBOL-85: "The program-name identifies the source program, the object
program, and all listings pertaining to a particular program" (section VI.3.3.4, and X.3.1.4 for a nested program). A page
names the innermost program that contains its first line, so a page that starts inside a nested program names the nested
program.

Each source line is one row:

| Columns | Content |
|---|---|
| 1-6 | the listing line number, unique in the listing |
| 8 | the COPY depth: blank for the source file, `1` to `9` for library text, `+` deeper |
| 9 | a marker: `/` page-eject comment line, `*` comment line, `D` debugging line, `-` continuation line, `>` compiler directive, `E` the DATE-COMPILED paragraph's replacement, `R` a line REPLACE or COPY REPLACING changed, `X` a line a false `>>IF` branch omitted |
| 11-16 | the line's number in its own file |
| 18-97 | the line as the file holds it (tabs expanded), all 80 positions of a fixed-form line; a longer line continues on rows marked `+` in column 9 |

Lines are listed as written, so their indentation is the source's (section IV.7.4: "The entries on the output listing need
be indented only if the input is indented"). Each COPY statement's library text follows the line that ends the statement,
under a row that names it (`COPY PAYREC OF LIB1 -> lib/PAYREC.cpy (depth 1)`); a library text copied twice is listed twice,
once at each COPY statement. A file that ends with a line terminator has an empty last line, which is listed (the
compiler's line rule, Annex A.1 item 156).

A line where a REPLACE statement or a COPY statement's REPLACING phrase replaced text is printed as written, marked `R`,
and under it, in rows marked `R`, the text the compiler compiled from it. A library text copied twice with different
REPLACING operands shows each copy's own replacement:

```text
     5         5        COPY REPBOOK REPLACING ==:N:== BY ==FIRST-ITEM==.
                 COPY REPBOOK -> lib/REPBOOK.cpy (depth 1)
     6 1R      1        01 :N: PIC X.
        R        01 FIRST-ITEM PIC X.
```

The compiled text is the text after the reference format is read (section 6.5 of ISO/IEC 1989:2023), so a fixed-form line's
sequence and indicator areas are not part of it.

## What is listed (2002 and later)

The compiler directives and the COPY statement of COBOL-2002 and later control the listing (ISO/IEC 1989:2023 sections
7.3.18, 7.3.19 and 7.2.3.4). The lines they turn off are still in the JSON listing, with `listed` false. COBOL-2002 and
COBOL-2014 state the same LISTING, PAGE and COPY SUPPRESS rules in the same words (ISO/IEC 1989:2002 sections 7.2.15,
7.2.16 and 7.1.2.3; ISO/IEC 1989:2014 sections 7.3.16, 7.3.17 and 7.2.2.3), and the listing applies them the same way at
`--std 2002` and `--std 2014`. Those two editions have no `>>PUSH`, `>>POP` or `>>DISPLAY` directive and no
exception-checking PERFORM, so the items below that name them apply at `--std 2023` only.

- `>>LISTING OFF` stops the listing of the lines after it until `>>LISTING ON` (or `>>LISTING` with no operand, which
  means ON). Every LISTING directive line is listed, the OFF ones included: "Each LISTING directive shall be listed, even
  if the listing is being suppressed by a LISTING directive" (section 7.3.18.3 general rule 3). The listing starts ON.
- `>>PUSH LISTING` (or `>>PUSH ALL`) saves the ON or OFF state and `>>POP LISTING` (or `>>POP ALL`) restores it for the
  lines after the POP. The implicit PUSH ALL and POP ALL of an exception-checking PERFORM (section 14.9.28.4 general rule
  14) do the same, so a `>>LISTING OFF` written in a WHEN phrase ends at the END-PERFORM.
- A `>>LISTING` directive in library text does not outlive it: after the library text, the state of the COPY statement
  is in effect again (section 7.2.3.4 general rule 5).
- `COPY name SUPPRESS.` lists the COPY statement but not the library text it copies (section 7.2.3.4 general rule 4); the
  library text is compiled in full.
- `>>DISPLAY` with `UPON LISTING`, or with no UPON phrase, prints its data in the listing where the directive stands, as
  `DISPLAY: <data>` in the source columns, in the order the directives are processed, even where `>>LISTING OFF` hides the
  directive's own line (section 7.3.12.4 general rules 2, 5 and 6). Without a listing the same data goes to the compiler's
  standard output; `UPON SYSERR` goes to standard error and `UPON CONSOLE` or `SYSOUT` to standard output either way.

## Where a page starts

- Before a comment line with `/` in the indicator area: "The slant in the indicator area causes page ejection prior to
  printing the comment line in the listing" (section IV.7.2.4). A `*` comment line prints on the next line.
- Before a listed `>>PAGE` directive line: "a PAGE directive shall cause page ejection followed by listing of the PAGE
  directive" (section 7.3.19.4 general rule 2). A `>>PAGE` line under `>>LISTING OFF` is not listed and starts no page,
  and neither does a `/` line there.
- Before each separately compiled program of the source after the first, so each program starts a page.
- When the page is full (`--listing-page-length`).

Each page after the first begins with a form feed character (U+000C), so a printer or `less` sees the page boundary.
The listing is UTF-8 with LF line ends.

## DATE-COMPILED and WHEN-COMPILED

The DATE-COMPILED paragraph "is replaced during compilation with a paragraph of the form `DATE-COMPILED. current date.`"
(section VI.3.4.4 general rule 1). The listing prints that paragraph, marked `E`, in place of the written one, for example
`DATE-COMPILED. 2026-11-18 09:15:02 -08:00.`; the further lines of its comment-entry are replaced with it and are not
printed. The page headings print the same date and time, and `FUNCTION WHEN-COMPILED` in the program returns the same
instant: one compilation has one time ("The returned value must denote the same time as the compilation date and time
if provided in the listing of the source program", the WHEN-COMPILED function, returned value 3).

WHEN-COMPILED denotes the listing's instant at every edition (ISO/IEC 1989:2002 section 15.80.2, ISO/IEC 1989:2014
section 15.92.2 and ISO/IEC 1989:2023 section 15.99.3, returned value 3 in each).

At `--std 2002` and later the DATE-COMPILED paragraph is a removed construct (COBOL0902); under `--permissive` it is
accepted with a warning and replaced in the listing as at `--std 85`.

## Diagnostics

Every diagnostic of the compilation (each error and warning the compiler prints) is placed in the listing under the line
it names. When that line is printed whole as written, a caret marks the column; then a row gives the severity, the code,
the column and the message, continued under itself when it is longer than the page is wide:

```text
     5         5        01 BIG PIC 9(32).
                        ^
                 ==> error COBOL0801 (col 8): data item 'BIG' (PICTURE 9(32)) has 32 digit
                     positions; ISO/IEC 1989 limits a numeric item to 31 digit positions (ISO
                     §2023:13.18.40.3 SR14)
```

A diagnostic in library text is placed in the copy it belongs to: when a library text is copied twice and only the second
copy is in error, the diagnostic is under the second copy's line. The column counts the line as listed, tabs expanded.

The listing ends with a summary on a page of its own, headed `DIAGNOSTICS`: the number of errors, warnings and information
messages, then every diagnostic in listing order with its listing line, file, line and column:

```text
Errors: 1   Warnings: 0   Information: 0

     5  error COBOL0801 demo.cob(5,8): data item 'BIG' (PICTURE 9(32)) has 32 digit positions;
        ISO/IEC 1989 limits a numeric item to 31 digit positions (ISO §2023:13.18.40.3 SR14)
```

A diagnostic that names no line of a file the listing lists (one about the compilation as a whole) appears in the summary
only, with `-` for its line; so does a diagnostic on a line the listing does not print. Messages cite the rule of the
edition being compiled, as on standard error.

## The JSON listing

`--listing-format json` writes the listing as one JSON document, `BASE.lst.json`: the stable form for a program to read,
where the text listing is for people. It holds everything the text listing is made from, and more: every file read with
its SHA-256 digest, every COPY statement with its outcome and nesting, every line with its kind (source, comment,
directive, ...), whether it was listed and whether a page starts before it, the text as written and, for the
DATE-COMPILED paragraph and a line REPLACE changed, what the compiler compiled in its place, every COPY statement's
SUPPRESS phrase, every `>>DISPLAY` transfer to the listing (`displays`), every program with its listing lines, and every
diagnostic with its code, severity, message, listing line, file, line and column. Each diagnostic also carries the
citations its message makes as data, `citations`: for the example above
`[{ "edition": 2023, "locator": "13.18.40.3", "rule": "SR14" }]`. A line lists the ids of the diagnostics placed on it. The text and
the JSON listing of one compilation are renderings of one document, so they always agree.

The document opens with the schema it follows:

```json
{
  "$schema": "urn:wiseowl:cobol:listing:v1",
  "schemaVersion": "1.0",
  "header": { "compilerVersion": "30.0.0", "edition": 85, "editionName": "ANSI X3.23-1985", ... },
  "files": [ { "id": 1, "path": "prog.cob", "role": "Source", "sha256": "..." } ],
  "lines": [ { "number": 1, "file": 1, "fileLine": 1, "depth": 0, "form": "Fixed", "kind": "Source", "text": "...", ... } ],
  ...
}
```

Property names are camelCase, enumerated values are their names (`"Source"`, `"PageEjectComment"`), and a property with
no value is left out rather than written as `null`. The schema is JSON Schema (draft 2020-12), `listing-v1.schema.json`:
it is embedded in the compiler's `WiseOwl.Cobol.Listing` assembly (`ListingJsonSchema.Committed()` returns it) and kept in
the source repository at `src/WiseOwl.Cobol.Listing/Json/listing-v1.schema.json`; every property in it carries a
description of its meaning. A
.NET program can read a JSON listing into the same types the compiler builds by referencing `WiseOwl.Cobol.Listing` and
calling `JsonListingWriter.Read`.

**Versions.** `schemaVersion` is `major.minor`. The minor grows when a property or an enumerated value is added; the major
changes only when one is removed, renamed or changes meaning, and the `$schema` identifier and the schema's file name
carry the major (`v1`). Within a major every addition is optional, so a reader of version 1 must ignore a property it does
not know and accept an enumerated value it does not know, and a reader written for 1.0 reads every 1.x document.
`JsonListingWriter.Read` does both: in a document of a later minor than the assembly's own, a value it does not know reads
as `ListingJson.UnknownEnumValue` (-1, a value no member has, so `Enum.IsDefined` is false for it). It refuses a document
that is not valid against its own version of the schema: a required property missing, a `null` where the schema allows
none, an enumerated value written as a number, or, in a document of its own minor or an earlier one, a value it does not
know. A new major is announced in the release notes of the
release that ships it.

With `--listing-output -` the one format requested goes to standard output, before the diagnostics (which go to standard
error). A `>>DISPLAY ... UPON LISTING` is in the listing itself; one `UPON CONSOLE` or `SYSOUT` follows the listing on
standard output.

## Later additions

The listing grows section by section: the options
section, the program map, the data map, the cross-reference, the statistics and verb counts, the map from each COBOL
statement to its object code, and a hyperlinked HTML form.
