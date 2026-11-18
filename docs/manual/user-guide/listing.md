---
title: The source listing
description: When the compiler writes a source listing, what it holds, how it is paginated, and the options that control it.
sidebar:
  order: 10
---

The compiler writes a **source listing**: every physical line of the source file and of each COPY library text, in the
order the compiler read them, paginated for reading or printing.

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
| `--listing-output <BASE>` | write the listing to `BASE.lst`; `-` writes it to standard output instead of a file. Default: the output assembly's path without its extension |
| `--listing-page-length <N>` | lines per page, the four heading lines included (default 60); `0` paginates only where a page must start (below). At least 5 otherwise |
| `--listing-page-width <N>` | columns per page, 100 to 255 (default 132) |

Each listing option other than `--listing off` asks for a listing at any edition. A value outside its range is a
command-line error (exit status 2), never a COBOL diagnostic.

## What a page holds

Each page begins with four heading lines: the compiler and its version, the edition (for example
`ANSI X3.23-1985 (--std 85)`), the name of the program the page's first line belongs to, the compilation date and time,
and the page number; then `SOURCE` and the source path; a blank line; and a ruler that numbers the 80 source columns.

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

## Later additions

The listing grows section by section: a machine-readable JSON form, diagnostics placed on their lines, the options
section, the program map, the data map, the cross-reference, the statistics and verb counts, the map from each COBOL
statement to its object code, and a hyperlinked HTML form. The `>>LISTING` and `>>PAGE` directives of 2002 and later
are accepted today and do not yet change the listing.
