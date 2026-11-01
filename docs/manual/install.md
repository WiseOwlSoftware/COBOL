---
title: Install
description: What you need to run the cobol command and the programs it compiles, how to build the compiler from source, and how to check the install.
sidebar:
  order: 1
---

## What you need

To compile and run COBOL programs you need the .NET 10 runtime on the machine, on Windows or Linux. The compiler
and every program it produces run on .NET 10, and `cobol --run` starts the program through the `dotnet` command, so
`dotnet` has to be on your `PATH`. The .NET 10 SDK includes the runtime.

To build the compiler from source you also need the tools the repository README lists under "What you need": the
.NET SDK named in `global.json`, PowerShell 7, Java 21 (the build runs the checked-in ANTLR jar when a grammar
changes) and Git.

## Build the compiler from source

The compiler is not yet published as a NuGet package. Build it from the repository:

```bash
git clone https://github.com/WiseOwlSoftware/COBOL.git
cd COBOL
dotnet build WiseOwl.Cobol.sln
```

Build the whole solution, not one project. The first build regenerates the parser from the grammars and takes a few
minutes; later builds are quick.

The build leaves the compiler at `src/WiseOwl.Cobol.Cli/bin/Debug/net10.0/cobol` (`cobol.exe` on Windows). Put that
directory on your `PATH`, or run the file by its full path.

## Check the install

```bash
cobol --version
cobol --help
```

`cobol --help` lists every option. If `cobol` prints its usage and `dotnet --list-runtimes` shows a
`Microsoft.NETCore.App 10.` line, the install is complete. Continue with [Getting started](../getting-started/).

## What a compile deploys

Each compile writes the program's assembly, its `.runtimeconfig.json`, a copy of the COBOL runtime library
`WiseOwl.Cobol.Runtime.dll` and the C# the compiler generated (`.g.cs`) beside the output file. On Windows and Linux
x64 the runtime library copied is the precompiled (ReadyToRun) image for that platform; on any other platform it is
the portable runtime. The set of files is everything a machine with the .NET 10 runtime needs to run the program.
