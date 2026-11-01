---
title: Getting started
description: Write, compile and run a first COBOL program, read a compiler error, and see what the compiler produced.
sidebar:
  order: 2
---

This page takes a program from source to a running assembly. It assumes you have finished [Install](../install/).

## Write a program

Save this as `hello.cob`. It is in fixed reference format, the default: the program text sits in columns 8 to 72, and
column 7 is the indicator area.

```cobol
       IDENTIFICATION DIVISION.
       PROGRAM-ID. HELLO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 GREETING PIC X(20) VALUE "Hello from COBOL".
       PROCEDURE DIVISION.
           DISPLAY GREETING
           STOP RUN.
```

## Compile and run it

```bash
cobol hello.cob -o hello.dll
dotnet hello.dll
```

The program prints its greeting, padded with spaces to the 20 character positions of `GREETING`:

```text
Hello from COBOL
```

`cobol hello.cob --run` compiles and then runs the program in one step. The command's exit status is the program's:
a `STOP RUN WITH ERROR STATUS 3` makes `cobol --run` exit with 3, and so does `dotnet hello.dll`.

## What the compiler produced

The compile wrote four files beside `hello.dll`:

| File | What it is |
|---|---|
| `hello.dll` | the program, a .NET assembly |
| `hello.runtimeconfig.json` | the file `dotnet` reads to find the .NET 10 runtime |
| `WiseOwl.Cobol.Runtime.dll` | the COBOL runtime library the program calls |
| `hello.g.cs` | the C# the compiler generated, which Roslyn built into `hello.dll` |

Open `hello.g.cs` to see how your program maps to .NET: a COBOL record becomes a `record struct` and an elementary
item becomes a native field. To run the program on another machine, copy `hello.dll`, `hello.runtimeconfig.json` and
`WiseOwl.Cobol.Runtime.dll` together.

## Read a compiler message

Change `DISPLAY GREETING` to `DISPLAY GREETINGS` and compile again:

```text
  hello.cob(7,12): error COBOL1639: 'GREETINGS' is not defined — no declaration in this source element gives the name 'GREETINGS', so the statement's reference identifies no resource (ISO §8.4.2.1: "a statement shall contain a reference that uniquely identifies that resource"). Check the spelling, or declare the item.
```

A message names the file, the line and column, the severity (`error` or `warning`), a diagnostic code (`COBOL`
followed by four digits) and the reason, usually with the clause of the standard that requires it. The compile stops
with exit status 65 and writes no assembly. [The `cobol` command](../user-guide/command-line/) lists the exit statuses.

## Next

- [The `cobol` command](../user-guide/command-line/) describes every option.
- [Editions](../user-guide/editions/) explains `--std`, which selects COBOL 1985, 2002, 2014 or 2023.
- [Source format and COPY libraries](../user-guide/source-format/) covers free-form source and copybooks.
- [.NET interoperability](../dotnet-interop/) shows how to call a COBOL program from a C# program.
