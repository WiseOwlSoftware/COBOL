---
title: The source listing
description: When the compiler writes a source listing, its sections (options, source, program map, data map, statistics and verbs, object code, diagnostics), how it is paginated, its hyperlinked HTML form, its JSON form and schema, and the options that control it.
sidebar:
  order: 10
---

The compiler writes a **source listing**: every physical line of the source file and of each COPY library text, in the
order the compiler read them, paginated for reading or printing, and on request the same listing as a hyperlinked HTML page
for reading in a browser and as a JSON document for programs to read.

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
| `--listing-output <BASE>` | write the listing to `BASE.lst` (and `BASE.lst.json`, `BASE.lst.html`); `-` writes it to standard output instead of a file, in the one format requested. Default: the output assembly's path without its extension |
| `--listing-format text\|json\|html` | a format to write: `text` is the paginated listing `BASE.lst`, `json` the JSON listing `BASE.lst.json`, `html` the HTML page `BASE.lst.html` (below). Repeat it for more than one (`--listing-format text --listing-format html`); default `text`. With `--listing-output -` exactly one format may be named |
| `--listing-sections <LIST>` | the sections to produce, a comma list of `source`, `programs`, `diagnostics`, `options`, `statistics`, `data-map`, `object-map`, `object-code`, `standard` and `all` (below); default `standard`, which is every section but the data map, the object-code map and the object code |
| `--listing-page-length <N>` | lines per page, the four heading lines included (default 60); `0` paginates only where a page must start (below). At least 5 otherwise |
| `--listing-page-width <N>` | columns per page, 100 to 255 (default 132) |

Each listing option other than `--listing off` asks for a listing at any edition. A value outside its range is a
command-line error, reported before anything is compiled (exit status 1), never a COBOL diagnostic.

## Sections

The listing is made of sections, each starting a page, in this order:

| Section | `--listing-sections` word | What it holds |
|---|---|---|
| Options | `options` | every option of the compilation, its value and whether it was given or left at its default; then the compiler, the runtime, .NET and the operating system |
| Source | `source` | every line of the source and of each library text, with the diagnostics under their lines and the `>>DISPLAY` output (below) |
| Program map | `programs` | every program, its kind, the listing lines it spans, and the programs nested in it; for each program the compiler bound, its COMMON, INITIAL, RECURSIVE and PROTOTYPE attributes, the name it is externalized as, and the .NET type it became |
| Data map | `data-map` | every data item, condition-name, index-name, constant, report, CD and file of every program, class and method, in the order written, with its level, offset, length, usage, picture and the .NET field it became; and, in the source section, each data description line's offset, length and usage in the right margin |
| Statistics | `statistics` | the lines by kind and by file, the programs, the statements, the diagnostics by severity, and each verb with its count and lines |
| Object-code map | `object-map` | each COBOL statement's object code: the .NET method that holds it and its IL offset range, read from the assembly the compilation wrote and its PDB (below) |
| Object code | `object-code` | under each COBOL statement, its IL instructions, decoded from the assembly (below) |
| Diagnostics | `diagnostics` | the summary of every diagnostic; without this section no diagnostic is printed under its line either |

`standard` names every section but the data map, the object-code map and the object code, and is the default; `all` names every
section the compiler can produce, those three included. Words combine: `--listing-sections source,statistics` writes those two. A
section the compilation could not produce (the program map of a source that was not parsed, the data map of a program the compiler
refused, the object code of a compilation that wrote no assembly) prints its title and why. The JSON listing holds the same sections as the
text listing: a section not asked for is absent from both.

The options section reads, for example:

```text
OPTION                  ORIGIN     VALUE
source                  specified  prog.cob
--output                specified  out/prog.dll
--std                   specified  85 (ANSI X3.23-1985)
--permissive            default    off
...
--listing-sections      default    standard
compiler                host       30.0.0
.NET                    host       .NET 10.0.0
```

`default` means the option has the compiler's own default value (`--std` is `2023` by default, so `--std 85` is
`specified`), `specified` that it was given another value, and `host` a fact of the compiler or the machine it ran on.

The program map lists each program in source order, a nested program indented under the program that contains it:

```text
    ID  KIND       FIRST    LAST  NAME
     0  program        1      25  OUTER
     1  program       15      22    INNER
```

The statistics count each line of the listing by kind (source, comment, blank, ...), and the lines omitted by a false
`>>IF` branch, copied from library text and not listed, for the whole listing and for each file; a library text copied
twice counts twice. They count the programs and the statements (a statement nested in another counts too), the diagnostics
by severity, and list each verb, as written (`GO` for GO TO), with the number of statements it begins and the listing line
of each:

```text
VERB                      COUNT  LINES
ADD                           2  11 15
MOVE                          2  9 13
```

The counts are read from the source as the compiler parsed it, so they are there even when the program has errors. A syntax
error is the exception to their being complete: the parser recovers from it and goes on, but it may skip source to do so, even
every statement after the error. The statistics then count only what the parser kept, and say so under `STATEMENTS`; the
program map says the same under its last program, and the JSON header's `parse` is `Recovered` (`Parsed` when the parse had
no error, `NotParsed` when the source was not parsed). The statistics hold no timings, so two listings of one source differ
only where the compilations did.

The data map lists every name the data division declares, in the order written, in every program, class, method and
interface: each data description entry (FILLER included), each level-88 condition-name, each INDEXED BY index-name, each
constant, each level-66 entry, each type declaration (marked `TYPE DECLARATION`, with its condition-names and index-names; it
has no storage, so no offset, bytes or field), each report (`rd`) with its report group description entries (an entry with a
SUM clause is its sum counter, a numeric item with the PICTURE's digits as its length and no picture or usage of its own; an
elementary entry with no COLUMN clause, the unprintable item its PICTURE describes), and each CD (`cd`, its FOR phrase on the row under it)
with the record area the standard describes for it. An item the standard defines implicitly, a CAPACITY IN register or an
item of a CD's record area, is marked `IMPLICIT`; the compiler's own temporaries are not listed. The entries a TYPE clause
copies are listed on the line of the entry that writes the clause, in its program, at their offsets there. An entry is
indented under the group it belongs to, a condition-name under its conditional variable, an index-name under its table, a
report group entry under its report and a CD's area and records under the CD.
For a data item it gives the listing line it is declared on, its level, its offset from the start of its level-1 record in
bytes (for an item of a table, its first occurrence's), its length in character positions (what `FUNCTION LENGTH` returns),
the bytes it occupies (every occurrence of a table), its usage and picture; and, on the row under it, its OCCURS,
DEPENDING ON, INDEXED BY, REDEFINES and RENAMES facts (each naming another entry by its `#id`), GLOBAL, EXTERNAL and BASED,
and the field of the compiled program that holds it with its .NET type (none for a LINKAGE SECTION item, whose storage is the
caller's; a method's LOCAL-STORAGE item is held in the method's local variable). A condition-name's row under it gives its values,
a constant's its specification. The file map follows: each file, the line of its file control entry, its organization,
access mode and record descriptions, and on the row under it its RECORD KEY, each ALTERNATE RECORD KEY (with DUPLICATES),
its RELATIVE KEY item, the reports it is written to and the name its file connector has in the run unit:

```text
    ID   LINE  NAME                           ORGANIZATION     ACCESS     RECORDS
     1      6  IF1                            INDEXED          DYNAMIC    1
                RECORD KEY IR-KEY (#2)  ALTERNATE KEY IR-ALT (#3) DUPLICATES  CONNECTOR P5::IF1
     2     11  RPTF                           SEQUENTIAL       SEQUENTIAL
                REPORTS #6  CONNECTOR P5::RPTF
```

```text
    ID   LINE  LVL KIND   NAME                           OFFSET LENGTH  BYTES  USAGE           PICTURE
     1      5      const  K                                   -      -      -
                            AS 5
     2      6  01  data   REC                                 0     19     19
                            .NET REC : _T_0
     3      7  05  data     A                                 0      3      3  DISPLAY         X(3)
                              .NET A : string
     4      8  05  data     T                                 3      4     12
                              OCCURS 3  INDEXED BY #5  .NET T : _T_2[]
```

With the data map, the source section prints the same offset, length and usage in the margin of each line that declares a
data item, from column 100 of a page 132 columns wide or wider (`OFF=0 LEN=3 DISPLAY`). The data map is not produced when the
compiler refused the program, since the sizes it would show are those of a program that did not compile.

## What a page holds

Each page begins with heading lines: the compiler and its version, the edition (for example
`ANSI X3.23-1985 (--std 85)`), the name of the program the page's first line belongs to, the compilation date and time,
and the page number; then the section's title (`SOURCE` and the source path, `OPTIONS`, `PROGRAMS`, `DATA MAP`,
`STATISTICS` or `DIAGNOSTICS`); a blank line; and, on a source page, a ruler that numbers the 80 source columns. A page outside the source
section names the first program of the source.
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
copy is in error, the diagnostic is under the second copy's line. The column counts the characters of the line as listed, tabs
expanded: a character outside the Basic Multilingual Plane (an emoji in a literal) counts once, in the text and the JSON alike.

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

When any diagnostic is an edition flag, the summary ends with a table of the flagged constructs by edition: a construct the
edition being compiled calls obsolete or archaic (`COBOL0903`), one it removed (`COBOL0902`, an error, or a warning under
`--permissive`), and, with `--flag-extensions`, a nonstandard extension in use (`COBOL2894`). Each row gives the edition whose
rule flags the construct (the edition that made it obsolete or removed it; for an extension, the edition being compiled), the
kind, how many diagnostics flag it, the construct with its identifier, and the listing line of each of those diagnostics. A
program compiled with `--std 2023 --permissive --flag-extensions` that writes a debugging line, a continued word, CLOSE WITH LOCK,
two EXIT PROGRAM statements and USAGE COMP-3:

```text
FLAGGED CONSTRUCTS BY EDITION
EDITION  KIND      COUNT  CONSTRUCT; LINES
   2014  removed       1  the fixed-form debugging line (indicator D) (debugging-line-removed-2014); 17
   2023  obsolete      1  the fixed continuation indicator (hyphen in column 7) (col7-continuation-obsolete-2023); 16
   2023  obsolete      2  the EXIT PROGRAM statement (exit-program-archaic-2023); 19 20
   2023  removed       1  the CLOSE WITH LOCK phrase (close-with-lock-removed-2023); 18
   2023  removed       1  continuation of a COBOL word in fixed-form reference format (fixed-form-word-continuation-removed-2023);
                          16
   2023  extension     1  USAGE COMP-3 / COMPUTATIONAL-3 (packed decimal) (usage-comp-3); 13
```

The table is the diagnostics' own: each flag is recorded where the compiler raises it, so the table and the list above it
always agree. A rule an edition added that `--permissive` relaxes to a warning (a later edition's restriction, under a code of its
own) is a diagnostic of that rule, not a flagged construct, and is not in the table.

## The JSON listing

`--listing-format json` writes the listing as one JSON document, `BASE.lst.json`: the stable form for a program to read,
where the text listing is for people. It holds everything the text listing is made from, and more: every file read with
its SHA-256 digest, every COPY statement with its outcome and nesting, every line with its kind (source, comment,
directive, ...), whether it was listed and whether a page starts before it, the text as written and, for the
DATE-COMPILED paragraph and a line REPLACE changed, what the compiler compiled in its place, every COPY statement's
SUPPRESS phrase, every `>>DISPLAY` transfer to the listing (`displays`), every program with its listing lines, every
diagnostic with its code, severity, message, listing line, file, line and column, the options (`options`), the statistics
with every line kind and severity counted (`statistics`), the data map (`dataMap`: its `items` and `files`, each naming the
others by id), each bound program's `attributes`, each definition line's `margin`, and which sections were asked for
(`sections`). Each diagnostic also carries the
citations its message makes as data, `citations`: for the example above
`[{ "edition": 2023, "locator": "13.18.40.3", "rule": "SR14" }]`. A diagnostic that is an edition flag carries it as `flag`
(`kind`, `construct`, `description`, `edition`), and `flaggedConstructs` is the summary's table, each entry naming its
diagnostics by id. A line lists the ids of the diagnostics placed on it. The text and
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

## The object-code map and the object code

Every compilation that writes an assembly also writes its debugging symbols beside it, as a portable PDB (`prog.pdb` beside
`prog.dll`). The PDB holds one sequence point for each COBOL statement, at the line and column where the statement is written,
in the source file or the library text that holds it (and one more at the verb for its code after a statement nested in it, such as
an inline PERFORM's step and test); nothing else of the compiled program has one. So a .NET debugger stops once per COBOL statement
and shows the COBOL source (twice, rarely, on a statement whose first code is a flag the compiler cannot show the optimizer keeps),
and the listing reads each statement's object code back from the assembly and the
PDB. Neither names generated source: the map is the same whichever back end compiled the program. The PDB names each file relative
to the main source's directory (`prog.cob`, `lib/PAYREC.cpy`), so the same source compiles to the same assembly wherever it lies;
point the debugger's source path at that directory if it does not find the files.

The object-code map (`object-map`) lists the methods that hold the program's statements, each with its metadata token and IL size,
then, in listing order, each statement's listing line and column, its method and its IL offset range:

```text
ASSEMBLY  out/prog.dll

METHOD TOKEN        SIZE  NAME
     1 0x06000005    321  Cobol.prog._PRG_PROG::__Dispatch

  LINE  COL METHOD  IL RANGE
     9   12      1  IL_0012-IL_0034
    15   12      1  IL_0091-IL_00B1
    15   12      1  IL_00E3-IL_011B
    16   15      1  IL_00B1-IL_00E3
```

A statement's code runs from its sequence point to the next statement's. A statement with a statement nested in it has a second
range for the code it runs after that statement: above, the inline `PERFORM VARYING` on line 15 has its initial setting before
the `ADD` nested in it (line 16) and its step and test after it. A statement that compiles to no code (`CONTINUE`, a statement no
execution reaches, a condition the compiler evaluated) has no range. A copybook copied twice holds each statement twice, and each
copy's code is placed on its own block of lines.

The object code (`object-code`) prints, for each range, the statement's line and then its IL instructions, ILDASM-style: each
offset, the instruction and its operand (a number, a quoted string, a branch target, or the type, field or method a token names):

```text
     9   12  Cobol.prog._PRG_PROG::__Dispatch  IL_0012-IL_0034
                        MOVE A TO B
             IL_0012  ldarg.0
             IL_0013  ldarg.0
             IL_0014  ldfld        Cobol.prog._PRG_PROG::A
```

The compiled code protects some statements with .NET exception handling: an OPEN or CLOSE restores the file's mode in a
`finally`, a size check catches the overflow it reports, a statement under `>>TURN ... CHECKING ON` filters the exception
conditions it handles. A method's exception-handling clauses are part of its object code, so the object code prints them where
ILDASM does, among the instructions at their IL offsets: `.try {` where a protected block begins and `} // end .try` where it
ends, then each handler of the block, `catch <type> {` (the type named as an instruction's operand names one), `finally {`,
`fault {`, or a filter's `filter {`, `} // end filter` and `{ // handler`, each closed by `} // end handler`. Several clauses that
protect one block share one `.try`. Blocks nest, and each level indents two columns, the instructions inside it too:

```text
    16   12  Cobol.prog._PRG_PROG::__Dispatch  IL_0064-IL_00A2
                        ADD 1 TO N ON SIZE ERROR DISPLAY "SIZE" END-ADD
             .try {
               .try {
                 IL_0064  ldarg.0
                 ...
                 IL_008B  leave.s      IL_0094
               } // end .try
               catch WiseOwl.Cobol.Runtime.CobolSizeError {
                 IL_008D  pop
                 IL_008E  leave.s      IL_0094
               } // end handler
```

A boundary prints in the range that holds its offset, so a block that ends where the next statement's code begins closes at the top
of that statement's range; one at the end of the method prints after the last instruction. The JSON listing holds each method's
clauses (`regions`: the kind, the protected block's and the handler's offsets, a filter's start, a catch's type) and, in each range
with instructions, the lines it prints among them, each with its offset, its depth and the clause it belongs to.

Both sections need an assembly: a check-only compilation, or one the compiler refused, prints why they are absent.

## The HTML listing

`--listing-format html` writes the listing as one HTML page, `BASE.lst.html`, for reading in a browser. The page is
self-contained: its style and its small script are inside it and it loads nothing from anywhere else, so it can be mailed,
archived or opened offline. It holds the sections asked for, in the order of the text listing, and is a rendering of the same
document as the text and the JSON listing, so the three always agree.

- **The source.** Every line of the source and of each library text is a row: its listing line number, its inclusion depth, its
  marker (the same markers as the text listing), its line in its file and its text. A line where the text listing starts a page
  carries a rule, and starts a page when the page is printed.
- **Inclusions.** Each COPY library text is a block under the COPY statement's line, opened by the same banner as the text listing
  (`COPY text-name OF library-name -> file (depth n)`); click the banner to fold it. A library text copied with `SUPPRESS` is folded
  to begin with.
- **Lines the text listing does not print.** Lines a `>>LISTING OFF` or COPY `SUPPRESS` turned off, lines a false `>>IF` branch
  omitted, and the further lines of a replaced DATE-COMPILED comment-entry are on the page, greyed, in a folded block that says how
  many lines it holds and why ("3 lines not listed (40-42)"). What a `>>DISPLAY` directive prints (`DISPLAY: <data>`) stays outside
  every folded block, a `SUPPRESS` block included: it is listed whatever a LISTING directive or `SUPPRESS` says, so the library
  text's folded block ends before it and its further lines continue in a block whose banner ends "(continued)".
- **Diagnostics.** Each diagnostic stands under its line, the column it names highlighted in the line's text; the diagnostic
  summary at the end lists them all and links each to its place, and its table of flagged constructs by edition links each
  listing line to the diagnostic under it.
- **Object code.** With `object-map` or `object-code` in the sections, each statement's line holds a folded block per stretch of its
  code: its method and IL range, and with `object-code` its instructions with the method's exception-handling lines among them,
  as the text listing prints them.
- **Links.** A data description line's margin links to its data map entry; a data map entry's line, and each entry its facts name
  (`REDEFINES #3`, `INDEXED BY #5`, a file's keys and records), are links; so are the program map's first and last lines, the
  object-code map's lines and methods, and each verb's lines in the statistics.

The bar at the top links to each section and holds a filter: type text to show only the source lines that contain it, or tick
*lines with diagnostics* to show only those; *Expand all* and *Collapse all* open and fold every block.

## Later additions

The listing grows section by section: the cross-reference is next, and with it each name on a source line becomes a link to its
definition and each definition lists its references. It joins `--listing-sections` with its builder, so `all` always names exactly
the sections the compiler can produce.
