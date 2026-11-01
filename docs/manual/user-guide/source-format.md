---
title: Source format and COPY libraries
description: Fixed and free reference format, the SOURCE FORMAT directive and --source-format, and how COPY finds library text.
sidebar:
  order: 3
---

## Reference format

A compilation group is written in fixed form or free form. The default is fixed form (ISO/IEC 1989:2023 section
7.3.24.3 general rule 2).

- **Fixed form** keeps the sequence number in columns 1 to 6, the indicator area in column 7, and program text in
  columns 8 to 72. A `*` or `/` in column 7 makes the line a comment, and a `-` continues a literal or word from the
  line before. Any character in column 7 other than a space, `*`, `/` or `-` is an error (COBOL2616).
- **Free form** has no columns: a line is program text from its first character, and comments start with `*>`.

`--source-format` chooses the format the source starts in:

| Value | Meaning |
|---|---|
| `fixed` | fixed form (the default) |
| `free` | free form |
| `auto` | the compiler detects fixed or free form from the layout of the text |

A free-form program compiled in fixed form fails on its first lines, because the first letters of the program fall in
the indicator area:

```text
  freefmt.cob(1,7): error COBOL2616: the indicator area (column 7) holds 'f', which is not a fixed indicator: ...
```

`--source-format free` (or `auto`) compiles it. `--source-format auto` is an implementor choice (Annex A.1 item 158)
and a nonstandard extension (conformance statement section 3.2); state the format with `--source-format` or with the
directive to stay within the standard.

### The SOURCE FORMAT directive

`>>SOURCE FORMAT IS FIXED` and `>>SOURCE FORMAT IS FREE` change the format from that line on, through the next such
directive or the end of the compilation group (section 7.3.24.3 general rule 1). In fixed form a directive is written
in the program-text area, from column 8 (section 7.3.3 syntax rule 3). A directive on the first line may itself be
written in either form (section 7.3.24.3 general rule 4).

## COPY libraries

`COPY name.` inserts the library text `name` at that point of the source. The compiler looks for the text in this order
(conformance statement, Annex A.1 item 40, which follows GnuCOBOL's order):

1. the compiler's current working directory;
2. each directory named by `--copy DIR`, in the order given;
3. with `--nist` only, the `copylib` directory beside the source file's directory.

The directory that holds the source file is searched only when it is one of those. A project that keeps its copybooks
beside its sources compiles from that directory, or names it with `--copy .`.

In each place the compiler tries the name as written, then with the suffixes `.CPY`, `.CBL`, `.COB`, `.cpy`, `.cbl` and
`.cob`, in that order, and uses the first file that exists. A name that already contains a period after its last
directory separator is tried only as written. `COPY name OF library` and `COPY name IN library` take the text from the
first searched place that has a subdirectory named `library`.

Given `lib/greeting.cpy`:

```cobol
       01 GREETING PIC X(21) VALUE "Hello from a copybook".
```

and a program with `COPY greeting.` in its WORKING-STORAGE SECTION:

```bash
cobol usecopy.cob --copy lib --run
```

compiles and runs it. Without `--copy lib` the compile stops with `error CBL3620: COPY copybook 'greeting' not found.
Searched: ...` followed by the places it looked.

Library text begins in the reference format in effect at the COPY statement (section 7.3.24.3 general rule 3). A
`>>SOURCE FORMAT` directive inside library text holds until the next directive or the end of that text, and then the
format reverts to the one in effect at the COPY statement (general rule 5).
