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
| 9 | a marker: `/` page-eject comment line, `*` comment line, `D` debugging line, `-` continuation line, `>` compiler directive, `E` the DATE-COMPILED paragraph's replacement, `X` a line a false `>>IF` branch omitted |
| 11-16 | the line's number in its own file |
| 18-97 | the line as the file holds it (tabs expanded), all 80 positions of a fixed-form line; a longer line continues on rows marked `+` in column 9 |

Lines are listed as written, so their indentation is the source's (section IV.7.4: "The entries on the output listing need
be indented only if the input is indented"). Each COPY statement's library text follows the line that ends the statement,
under a row that names it (`COPY PAYREC OF LIB1 -> lib/PAYREC.cpy (depth 1)`); a library text copied twice is listed twice,
once at each COPY statement. A file that ends with a line terminator has an empty last line, which is listed (the
compiler's line rule, Annex A.1 item 156).

## Where a page starts

- Before a comment line with `/` in the indicator area: "The slant in the indicator area causes page ejection prior to
  printing the comment line in the listing" (section IV.7.2.4). A `*` comment line prints on the next line.
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

At `--std 2002` and later the DATE-COMPILED paragraph is a removed construct (COBOL0902); under `--permissive` it is
accepted with a warning and replaced in the listing as at `--std 85`.

## The JSON listing

`--listing-format json` writes the listing as one JSON document, `BASE.lst.json`: the stable form for a program to read,
where the text listing is for people. It holds everything the text listing is made from, and more: every file read with
its SHA-256 digest, every COPY statement with its outcome and nesting, every line with its kind (source, comment,
directive, ...), whether it was listed and whether a page starts before it, the text as written and, for the
DATE-COMPILED paragraph, what the compiler compiled in its place, and every program with its listing lines. The text and
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
not know, and a reader written for 1.0 reads every 1.x document. A new major is announced in the release notes of the
release that ships it.

With `--listing-output -` the one format requested goes to standard output, before the diagnostics (which go to standard
error). The lines of a `>>DISPLAY` directive that go to standard output follow the listing there.

## Later additions

The listing grows section by section: diagnostics placed on their lines, the options
section, the program map, the data map, the cross-reference, the statistics and verb counts, the map from each COBOL
statement to its object code, and a hyperlinked HTML form. The `>>LISTING` and `>>PAGE` directives of 2002 and later
are accepted today and do not yet change the listing.
