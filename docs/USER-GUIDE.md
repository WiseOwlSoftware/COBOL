# CobolSharp User Guide

## Installation

### As a .NET Global Tool
```bash
dotnet tool install -g CobolSharp
```

### From Source
```bash
git clone https://github.com/BrentRector/CobolSharp.git
cd CobolSharp
dotnet build
```

## Compiling COBOL Programs

```bash
# Basic compilation
cobolsharp compile myprogram.cob

# Specify output path
cobolsharp compile myprogram.cob -o output/myprogram.dll

# Run the compiled program
dotnet output/myprogram.dll
```

## Compiler Output

For each compiled program, the compiler generates:
- `<name>.dll` — the .NET assembly
- `<name>.pdb` — portable PDB for debugging
- `<name>.runtimeconfig.json` — .NET runtime configuration
- `CobolSharp.Runtime.dll` — runtime support library (copied automatically)

## Source Format

CobolSharp supports both **free-form** and **fixed-form** COBOL source:

- **Free-form** (default): No column restrictions. Comments use `*>`.
- **Fixed-form**: Auto-detected when source has numeric sequence numbers in columns 1-6. Column 7 indicator (`*` for comment, `-` for continuation).

You can force the format with the `>>SOURCE FORMAT` directive:
```cobol
>>SOURCE FORMAT IS FREE
```

## COPY Statement

Include copybooks with the COPY statement:
```cobol
COPY MY-COPYBOOK.
COPY MY-COPYBOOK REPLACING ==OLD-NAME== BY ==NEW-NAME==.
```

Copybooks are searched in the source file's directory. Additional search paths can be configured programmatically.

## Diagnostics

Compiler errors include file, line, and column information:
```
myprogram.cob(15,12): error CS0401: Undefined data-name 'WS-NAEM'. Did you mean 'WS-NAME'?
```

## Supported COBOL Features

See [CONFORMANCE.md](CONFORMANCE.md) for the full feature matrix.

## Intrinsic Functions

Use intrinsic functions with the `FUNCTION` keyword:
```cobol
COMPUTE WS-RESULT = FUNCTION SQRT(144).
DISPLAY FUNCTION UPPER-CASE("hello").
DISPLAY FUNCTION CURRENT-DATE.
```

Supported function categories: math, string, date/time, financial, aggregates (~70 total).
