---
title: Run units and STOP RUN
description: What a run unit is, how a program run by cobol or called from a .NET host begins and ends one, and what STOP RUN and a fatal exception do to a host.
sidebar:
  order: 4
---

A run unit is the set of programs that run together with the files, EXTERNAL items, switches and locale they share.
Every program has one WORKING-STORAGE per run unit, and it keeps its last-used state from call to call until the
run unit ends or the program is canceled (an INITIAL program is initialized on every call).

## A program run on its own

A program built by `cobol` and started with `dotnet program.dll`, or with `cobol --run`, begins its own run unit when
`Main` starts. When the run unit ends, the process ends with the run unit's exit status:

| Ending | Process exit status |
|---|---|
| the main program's GOBACK, EXIT PROGRAM or `STOP RUN` | 0 |
| `STOP RUN WITH ERROR STATUS n` | n |
| a fatal exception condition (an abnormal termination) | 1 |

A normal ending performs the actions of ISO/IEC 1989:2023 section 14.6.11, including an implicit CLOSE for every file
that is open (general rule 2). Section 14.6.12 requires the operating system to indicate an abnormal termination when
it can, which the status 1 does. A fatal exception writes its name and reason to the standard error stream, for
example:

```text
abnormal run-unit termination: EC-BOUND-SUBSCRIPT (fatal): subscript 5 is outside 1..3 (ISO 8.4.2.3.4 GR2)
```

## A program called from a .NET host

A .NET program that calls a COBOL program begins its run unit with `RunUnit.Run`, and the COBOL program ends it
without ending the host. In a host, a `STOP RUN`, and a fatal condition that terminates the run unit, end the run unit
only. The compiler's runtime performs the section 14.6.11 actions (every file of the run unit is closed), starts a new
run unit as the host's current one, and throws one documented, catchable exception,
`WiseOwl.Cobol.Runtime.RunUnitEndedException`. Its `ExitStatus` is the `STOP RUN` status, or 1 for an abnormal
termination, and its `Abnormal` property says which kind of ending it was. The runtime never calls `Environment.Exit`,
and no other exception type reaches the host for a run-unit ending. This is the determination for Annex A.1 item 167
in the conformance statement. Section 14.9.42.4 general rule 6 says STOP "transfers control to the operating system",
and for a COBOL program called from .NET the host is that system's counterpart.

Given this COBOL program, which counts its activations in WORKING-STORAGE and ends the run unit on the second:

```cobol
       IDENTIFICATION DIVISION.
       PROGRAM-ID. STOPPER.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 CALLS PIC 9 VALUE 0.
       PROCEDURE DIVISION.
           ADD 1 TO CALLS
           DISPLAY "call number " CALLS
           IF CALLS = 2
               STOP RUN WITH ERROR STATUS 7
           END-IF
           GOBACK.
```

this C# host

```csharp
using System;
using WiseOwl.Cobol.Runtime;

try
{
    RunUnit.Run(_ =>
    {
        Cobol.stopper.STOPPER.Call();   // call number 1: GOBACK returns here
        Cobol.stopper.STOPPER.Call();   // call number 2: STOP RUN ends the run unit
        Console.WriteLine("not reached");
    });
}
catch (RunUnitEndedException ended)
{
    Console.WriteLine($"run unit ended: status {ended.ExitStatus}, abnormal {ended.Abnormal}");
}
RunUnit.Run(_ =>
{
    Cobol.stopper.STOPPER.Call();       // a new run unit: call number 1 again
});
Console.WriteLine("host continues");
```

prints

```text
call number 1
call number 2
run unit ended: status 7, abnormal False
call number 1
host continues
```

The host's `Cobol.stopper.STOPPER.Call` is the generated host entry; see [.NET interoperability](../../dotnet-interop/)
for it and for how to build such a host. A call made outside `RunUnit.Run` joins the run unit current for the host's
execution context, and that run unit ends at process exit.
