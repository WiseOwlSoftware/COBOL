---
title: .NET interoperability
description: Calling a WiseOwl COBOL program from C# or another .NET language, what crosses the boundary, what a call can throw, and what is not supported.
sidebar:
  order: 4
---

ISO/IEC 1989:2023 clause 4.2.3 says that when an implementation supports interaction with non-COBOL runtime modules, it
documents the languages and implementations supported, and section 14.2.3 general rule 13 requires it to specify the
restrictions and mechanisms for each supported language product. This page is that documentation.

## What is supported

- **Any .NET language can activate a WiseOwl COBOL program.** A COBOL program is a .NET assembly, and the compiler gives
  every program and user-defined function a public class that a .NET program calls like any static method. The examples
  here are C#.
- **A COBOL program cannot activate non-COBOL code.** A CALL resolves only WiseOwl COBOL programs, and so do a program
  pointer and `ADDRESS OF PROGRAM`. No call-convention other than COBOL exists, and CANCEL does not touch a non-COBOL
  module. A COBOL program that has to use a .NET library does so through the .NET host that calls it, which passes the
  results in as arguments.
- **No external items are shared.** The host and the COBOL program share only what a call passes: the USING arguments
  and the RETURNING item. Each run unit's EXTERNAL items are its own, and COBOL storage is invisible to the host.

These are the determinations for Annex A.1 items 13, 15, 141 and 167 in the conformance statement
(`docs/CONFORMANCE.md`, section 7).

## Calling a WiseOwl COBOL program from .NET

Reference the COBOL assembly and `WiseOwl.Cobol.Runtime.dll`, and deploy both beside your host. The compiler wrote a
copy of the runtime library next to the assembly it produced; use that copy, which is the one the program was built
against.

Every outermost program and every user-defined function has a public static class `Cobol.<assembly>.<NAME>`, where
`<assembly>` is the output file name without its extension and `<NAME>` is the program's name. The class has a method
`Call` whose parameters are the PROCEDURE DIVISION USING formals, and a program's class also has `Cancel()`.

Given `addtax.cob`:

```cobol
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ADDTAX.
       DATA DIVISION.
       LINKAGE SECTION.
       01 AMOUNT   PIC 9(5)V99.
       01 RATE     PIC 9V99.
       01 TOTAL    PIC 9(6)V99.
       PROCEDURE DIVISION USING BY VALUE AMOUNT RATE
                          RETURNING TOTAL.
           COMPUTE TOTAL = AMOUNT + AMOUNT * RATE
           GOBACK.
       END PROGRAM ADDTAX.
```

compiled with `cobol addtax.cob -o addtax.dll`, a C# program calls it as:

```csharp
using System;
using WiseOwl.Cobol.Runtime;

public static class Host
{
    public static void Main()
    {
        RunUnit.Run(_ =>
        {
            decimal total = Cobol.addtax.ADDTAX.Call(100.00m, 0.08m);
            Console.WriteLine(total);   // 108.00
        });
    }
}
```

`Call` here is `public static decimal Call(decimal AMOUNT, decimal RATE)`: both formals are BY VALUE, and the RETURNING
item is the return value.

### How parameters and results are typed

| COBOL | .NET |
|---|---|
| a numeric item with no decimal places | `long`, or `Int128` (`UInt128` for an unsigned 16-byte binary item) when its storage can hold more than a `long` |
| a numeric item with decimal places | `decimal` (the runtime's `CobolDec` beyond 28 digits or 28 places) |
| a floating-point item | its own CLR type |
| an alphanumeric, national, boolean or edited item | `string` |
| an index | `long` |
| a pointer | `ManagedPointer`, `ProgramPointer` or `FunctionPointer` |
| an object reference | the class's CLR type |
| a group | a nested record struct named `<group>_Record`, with `AsImage()` and `FromImage()` for its character image |

A group that holds a DYNAMIC LENGTH item or an OCCURS DYNAMIC table is a variable-length group: its dynamic-length item is
a `string`, its table is an array as long as the table's current capacity, and the record has no image methods, because
its storage has no single image (ISO/IEC 1989:2023 sections 8.5.1.9.1 and 8.5.1.10.3).

How a parameter is passed:

- BY REFERENCE is a C# `ref` parameter. The program works on storage of the formal's description, and the variable
  receives its content when the call ends.
- BY VALUE is a plain parameter.
- An OPTIONAL formal is a nullable parameter, and `null` passes OMITTED.
- The RETURNING item is the return value.

Each value is landed in the formal as a COBOL COMPUTE, SET or MOVE lands it (section 14.2.3 general rules 9 and 10), and
a value that would lose anything on the way in is refused before the program runs (see below).

### Run units

Run your calls inside `RunUnit.Run(ru => { ... })`, which begins a run unit and ends it, closing its files, when the body
returns. A call made outside it joins the run unit current for your execution context. Every call crosses the same
boundary a COBOL CALL crosses: the program keeps one WORKING-STORAGE per run unit until you call `Cancel()`, recursion is
refused unless the program is RECURSIVE, and an INITIAL program is initialized on each call. [Run units and STOP
RUN](../user-guide/run-units/) describes what a `STOP RUN` in the program does to the host.

### What a call can throw

| Exception | When |
|---|---|
| `ArgumentException`, before the program runs | a value would lose anything on the way in (a digit at either end, a sign, or characters beyond the item's length, which is the subclass `ArgumentOutOfRangeException`); a non-OPTIONAL parameter is `null`; you pass one variable to two `ref` parameters whose COBOL descriptions differ (to two parameters of one description, the program gets one storage for both) |
| `CobolCallException`, with the exception-name in `EcName` | the program cannot be activated, or a condition propagates out of it |
| `RunUnitEndedException` | the program executes `STOP RUN`, or a fatal condition ends the run unit; the run unit's files are closed and your process continues |
| `FormatException` | the program leaves a numeric item holding characters that are not a number of its description; that variable is left as it was |

A program that cannot be located, because its assembly is missing, does not load, is of another runtime version or fails
while it registers, is an EC-PROGRAM-NOT-FOUND condition that names the reason.

A program compiled against a different signature than the one it is called with is detected at run time only when it
enables EC-PROGRAM-ARG-MISMATCH checking (`>>TURN EC-PROGRAM-ARG-MISMATCH CHECKING ON`); otherwise it reads the
arguments through its own description.

## Trimmed and AOT applications

A trimmed or native-AOT application has no dynamic loading. Its host references every COBOL assembly the run unit uses
and calls each one's `__CobolModule.EnsureRegistered()` before the first call. A program called by name from COBOL is
then found among the registered ones.

A COBOL program that calls another program by name finds it, when no host registered it, by loading the assembly that
carries it from the application directory: the file named after the program first, then any assembly there that lists
it. That loading is the runtime's one use of reflection, and it sits behind the feature switch
`WiseOwl.Cobol.Runtime.DynamicProbe`, which is on unless the application turns it off. A native-AOT or trimmed
application turns it off in its project file:

```xml
<ItemGroup>
  <RuntimeHostConfigurationOption Include="WiseOwl.Cobol.Runtime.DynamicProbe" Value="false" Trim="true" />
</ItemGroup>
```

A CALL that nothing registered is then an EC-PROGRAM-NOT-FOUND condition saying the dynamic probe is disabled. A trimmed
application that keeps the switch on must keep the whole runtime and deploy the called assemblies untrimmed beside it:

```xml
<ItemGroup>
  <TrimmerRootAssembly Include="WiseOwl.Cobol.Runtime" />
</ItemGroup>
```

## Known limitation: building a host project

The compiler builds a program against the framework's implementation assemblies, so the emitted assembly refers to
`System.Private.CoreLib` rather than to `System.Runtime`. A host project that uses the SDK's reference assemblies cannot
resolve that reference, and its first use of a type from the COBOL assembly fails with error CS0012 ("The type 'Object' is
defined in an assembly that is not referenced"). Until the compiler is changed to build against reference assemblies, a
host has to be compiled against the same implementation assemblies, which is how the compiler's own host tests build
theirs.

<!-- kb/Work PB4047 fixes this limitation; delete this section when it lands. -->

## Interaction with other COBOL implementations

See [Migrating](../migrating/#interaction-between-cobol-implementations-clause-424).
