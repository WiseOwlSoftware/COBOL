# CobolSharp

A production-quality COBOL compiler targeting .NET, implementing ISO/IEC 1989:2023.

## Quick Start

```bash
# Compile a COBOL program
cobolsharp compile hello.cob

# Run the compiled program
dotnet hello.dll
```

## Installation

```bash
# Install as a .NET global tool
dotnet tool install -g CobolSharp

# Or build from source
git clone https://github.com/BrentRector/CobolSharp.git
cd CobolSharp
dotnet build
```

## Features

- **Full COBOL parsing**: IDENTIFICATION, ENVIRONMENT, DATA, and PROCEDURE divisions
- **Data types**: PICTURE clause (all symbols), USAGE (DISPLAY, BINARY, PACKED-DECIMAL, INDEX, POINTER, NATIONAL)
- **Data hierarchy**: Groups, OCCURS, REDEFINES, RENAMES, level 66/77/88
- **Arithmetic**: ADD, SUBTRACT, MULTIPLY, DIVIDE, COMPUTE with operator precedence
- **Control flow**: IF/ELSE/END-IF, EVALUATE/WHEN, PERFORM (inline, out-of-line, THRU, TIMES, UNTIL), GO TO
- **String operations**: STRING, UNSTRING, INSPECT (parsing)
- **File I/O**: Sequential, indexed, and relative file organizations with full OPEN/READ/WRITE/CLOSE
- **Intrinsic functions**: ~70 functions (math, string, date/time, financial, aggregates)
- **Preprocessor**: COPY with REPLACING, REPLACE, fixed-form auto-detection
- **Subprograms**: CALL/CANCEL with BY REFERENCE/CONTENT/VALUE
- **Advanced**: Report Writer, Screen Section, OO COBOL, exception handling (parsing)
- **Diagnostics**: File/line/column positions, "Did you mean...?" suggestions
- **CIL output**: Mono.Cecil-based .NET assembly emission

## Sample Program

```cobol
       IDENTIFICATION DIVISION.
       PROGRAM-ID. HELLO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-NAME PIC X(20) VALUE "World".
       PROCEDURE DIVISION.
           DISPLAY "Hello, " WS-NAME "!".
           STOP RUN.
```

## Architecture

```
COBOL Source → Preprocessor → Lexer → Parser → Semantic Analysis → CIL Code Gen → .NET Assembly
```

- **Implementation**: C# on .NET 8+
- **CIL emission**: Mono.Cecil
- **Parser**: Hand-written recursive descent
- **Runtime**: CobolProgram base class with byte[] storage + decimal computation

## License

MIT License — Copyright (c) 2026 Brent Rector
