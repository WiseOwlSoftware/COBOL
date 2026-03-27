# ANTLR4 VSCode Extension: False Warnings with `import` Statement in Split Grammar Files

## Project Structure

```
Grammar/
  CobolLexer.g4                    # lexer grammar (defines all tokens)
  CobolParserCore.g4               # master parser grammar
  Core/
    CobolExpressions.g4            # imported parser grammar fragment
    CobolData.g4                   # imported parser grammar fragment
    CobolControlFlow.g4            # imported parser grammar fragment
    CobolIO.g4                     # imported parser grammar fragment
    CobolSpecialNames.g4           # imported parser grammar fragment
    CobolReportWriter.g4           # imported parser grammar fragment
    CobolExtensionsJsonXml.g4      # imported parser grammar fragment
```

## How It Works (Correctly, at the ANTLR4 Tool Level)

The master parser grammar `CobolParserCore.g4` declares:

```antlr
parser grammar CobolParserCore;

options {
    tokenVocab = CobolLexer;
    superClass = CobolParserCoreBase;
}

import CobolExpressions, CobolData, CobolSpecialNames, CobolReportWriter,
       CobolIO, CobolControlFlow, CobolExtensionsJsonXml;
```

Each imported file (e.g., `CobolExpressions.g4`) is a standalone `parser grammar` with NO `options` block — no `tokenVocab`, no `superClass`:

```antlr
parser grammar CobolExpressions;

// Rules that reference tokens defined in CobolLexer.g4
condition
    : logicalOrExpression
    ;
// ... uses PLUS, MINUS, IDENTIFIER, INTEGERLIT, etc.
```

The ANTLR4 **tool** (command-line `java -jar antlr-4.13.2-complete.jar`) handles this correctly:
1. It reads `CobolParserCore.g4` and sees `tokenVocab = CobolLexer`
2. It reads the `import` list and inlines all rules from the imported grammars
3. Token references in imported grammars (e.g., `PLUS`, `IDENTIFIER`, `SUB_COLON`) resolve correctly because they're resolved in the context of the master grammar's `tokenVocab`
4. The tool generates correct `CobolParserCore.cs` with all rules merged

This is the documented ANTLR4 behavior for grammar composition via `import`.

## The VSCode Extension Problem

The ANTLR4 VSCode extension (Mike Lischke's `mike-lischke.vscode-antlr4`) analyzes each `.g4` file **independently**. When it opens an imported grammar file like `CobolExpressions.g4`, it sees:

1. A `parser grammar` declaration
2. NO `tokenVocab` option
3. Rules that reference tokens like `PLUS`, `MINUS`, `IDENTIFIER`, `INTEGERLIT`, `SUB_COLON`, `SIGNED_INTEGERLIT`, etc.

Since the extension doesn't know this file is imported by `CobolParserCore.g4` (which provides the `tokenVocab`), it treats every token reference as an **undefined implicit token** and produces warnings/errors for each one.

## Symptoms

When editing any file in `Core/`:

- **Every token reference is flagged** as "implicit token definition" or "unknown token" — dozens to hundreds of warnings per file
- **Token-dependent features break**: no syntax highlighting for tokens, no go-to-definition for token names, no hover documentation for tokens
- **Cross-rule references between imported files fail**: if `CobolData.g4` references a rule defined in `CobolExpressions.g4`, the extension can't resolve it because it doesn't know both files are imported into the same master grammar
- **The master grammar `CobolParserCore.g4` analyzes correctly** because it has `tokenVocab = CobolLexer` directly

## What the Extension Should Do

The extension should:

1. **Detect when a grammar file is imported by another grammar.** When opening `CobolExpressions.g4`, scan `.g4` files in the workspace for `import CobolExpressions` statements.

2. **Resolve token vocabulary through the import chain.** If `CobolExpressions.g4` is imported by `CobolParserCore.g4`, and `CobolParserCore.g4` has `tokenVocab = CobolLexer`, then token references in `CobolExpressions.g4` should be resolved against `CobolLexer.g4`.

3. **Resolve cross-file rule references through shared import context.** If `CobolData.g4` and `CobolExpressions.g4` are both imported by the same master grammar, rule references between them should resolve.

4. **Handle the `import` search path.** ANTLR4's tool searches for imported grammars in the same directory as the importing grammar AND in a configurable library path. The extension should use the same resolution logic. In this project, the master grammar is in `Grammar/` and imports are in `Grammar/Core/`, and the ANTLR tool is invoked with `-lib Core` to resolve them.

## Minimal Reproduction

1. Create `Lexer.g4`:
```antlr
lexer grammar Lexer;
PLUS : '+' ;
NUM  : [0-9]+ ;
WS   : [ \t\r\n]+ -> skip ;
```

2. Create `ExprRules.g4`:
```antlr
parser grammar ExprRules;
expr : NUM (PLUS NUM)* ;
```

3. Create `Parser.g4`:
```antlr
parser grammar Parser;
options { tokenVocab = Lexer; }
import ExprRules;
program : expr EOF ;
```

4. Open `ExprRules.g4` in VSCode with the ANTLR4 extension installed.
5. **Expected:** No warnings — `NUM` and `PLUS` resolve via the import chain.
6. **Actual:** Warnings on `NUM` and `PLUS` as undefined/implicit tokens.

## Impact

For large grammars split across many files (like this COBOL compiler with 7 imported grammar fragments and 300+ token types), the false warnings make the extension's diagnostics panel unusable. Developers must either:
- Ignore all extension warnings (defeating the purpose)
- Add redundant `options { tokenVocab = ... }` to each imported file (which ANTLR4 technically allows but is semantically wrong — the imported grammar shouldn't declare its own tokenVocab)
- Consolidate into a single monolithic grammar file (defeating the purpose of `import`)

## ANTLR4 Tool Version

The project uses ANTLR 4.13.2 (`antlr-4.13.2-complete.jar`). The `import` behavior has been stable since ANTLR4's inception and is documented in *The Definitive ANTLR 4 Reference* §15.4.
