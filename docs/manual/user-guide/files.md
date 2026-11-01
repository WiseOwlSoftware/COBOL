---
title: Files and I-O
description: The basics of file handling in a compiled program, where files are found, how text is encoded, and where the determinations are recorded.
sidebar:
  order: 7
---

This page covers what every program with files needs to know. The conformance statement (`docs/CONFORMANCE.md`,
section 7) records each determination the standard leaves to the implementor, one Annex A.1 row at a time, and names
the test that proves it.

## Where a file is found

The operand of a SELECT's ASSIGN clause (a literal, a word, or the content of a data item named by USING) is mapped to
a host path (Annex A.1 items 71 and 73). A name that already contains a period, a slash or a backslash is the path as
written. Any other name is lowercased, given the extension `.txt` and placed in the current directory. A relative path is
relative to the current working directory of the process that runs the program. `ASSIGN TO DISK "a.dat"` and
`ASSIGN TO "a.dat"` name the same file; `DISK` and `PRINTER` are the only device classes, because every file is a file
of the host file system. This program writes one line to `notes.txt` and reads it back:

```cobol
       IDENTIFICATION DIVISION.
       PROGRAM-ID. FIL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT NOTES ASSIGN TO "notes.txt"
               ORGANIZATION IS LINE SEQUENTIAL
               FILE STATUS IS NOTES-STATUS.
       DATA DIVISION.
       FILE SECTION.
       FD NOTES.
       01 NOTE-REC PIC X(20).
       WORKING-STORAGE SECTION.
       01 NOTES-STATUS PIC XX.
       PROCEDURE DIVISION.
           OPEN OUTPUT NOTES
           MOVE "price 5 €" TO NOTE-REC
           WRITE NOTE-REC
           CLOSE NOTES
           OPEN INPUT NOTES
           READ NOTES
           DISPLAY NOTE-REC
           CLOSE NOTES
           OPEN INPUT NOTES
           READ NOTES
           READ NOTES
           DISPLAY NOTES-STATUS
           STOP RUN.
```

It prints the record, then the status `10` of the second READ after the one record, the end-of-file condition. The
program creates only `notes.txt`. A data file never has a catalog or attribute file beside it: WiseOwl COBOL writes and
reads nothing but the file itself, and the attributes it validates when a file is opened are those its own bytes record
(Annex A.1 item 129).

## How text is encoded

- **Line sequential files** are UTF-8 text with no CODE-SET clause: the `€` above is written as the three bytes
  `E2 82 AC`, trailing spaces of the record are not written, and each line ends with the host's newline (CR LF on
  Windows, LF on Linux and macOS).
- **Sequential record files and report files** with no CODE-SET clause hold one byte per character position in ISO/IEC
  8859-1. A character above U+00FF has no such byte, and WiseOwl COBOL never writes a substitute for it silently (Annex
  A.1 items 31, 146 and 159).
- **National items** (`PIC N`, USAGE NATIONAL) are two bytes per character position, UTF-16 with the high-order byte
  first.

## Closing files

Every file the run unit has open is closed when the run unit ends, normally or at a `STOP RUN` (ISO/IEC 1989:2023
section 14.6.11, general rule 2). A program called from a .NET host has its files closed when its run unit ends, which
is when `RunUnit.Run` returns or a `STOP RUN` ends it; see [Run units and STOP RUN](../run-units/).

## Where to read more

The conformance statement carries the rest: the limits of a relative or indexed file (item 107), record sizes on the
medium (item 146), sharing and record locking (the Annex A.4.7 row, which is claimed for every organization), and
commit and rollback (the Annex A.4.3 row).
