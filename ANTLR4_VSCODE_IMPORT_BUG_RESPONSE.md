Thanks for the quick reply. I tested your suggestion thoroughly and have concrete results.

## Original grammar file structure

14 grammar files (3,225 lines total), split across two directories:

```
Grammar/
  CobolLexer.g4              # lexer grammar (448 lines, 300+ tokens, multiple modes)
  CobolParserCore.g4         # master parser: tokenVocab = CobolLexer; import CobolExpressions, ...
  CobolDialect.g4            # dialect-specific parser rules
  CobolParserGenerics.g4     # generic/template extension rules
  CobolParserJsonXml.g4      # JSON/XML extension rules
  CobolParserOO.g4           # OO COBOL extension rules
  CobolPreprocessor.g4       # preprocessor grammar
  .antlr/
    CobolLexer.tokens        # extension-generated cache
  Core/
    CobolExpressions.g4      # imported parser fragment (no tokenVocab)
    CobolData.g4             # imported parser fragment (no tokenVocab)
    CobolControlFlow.g4      # imported parser fragment (no tokenVocab)
    CobolIO.g4               # imported parser fragment (no tokenVocab)
    CobolSpecialNames.g4     # imported parser fragment (no tokenVocab)
    CobolReportWriter.g4     # imported parser fragment (no tokenVocab)
    CobolExtensionsJsonXml.g4 # imported parser fragment (no tokenVocab)
```

In this layout, every imported grammar in `Core/` showed hundreds of "Unknown token reference" errors because the extension couldn't resolve tokens -- the lexer was in the parent directory.

## Test 1: Your proposed solution with the original layout (FAILS)

I added `options { tokenVocab = CobolLexer; }` to `Core/CobolExpressions.g4` while the lexer remained in `Grammar/`.

Result: **151 "Unknown token reference" errors persisted.** The extension searches for the lexer grammar relative to the parser grammar file's directory. Since `CobolLexer.g4` was in `Grammar/` but the parser fragment was in `Grammar/Core/`, the extension couldn't find it.

I also tried copying `CobolLexer.tokens` into `Core/` -- same result. The extension needs the actual `.g4` file, not just the `.tokens` file.

## Test 2: Same-directory baseline (WORKS)

I created a minimal test case with all files in one directory:

```
Core/
  SimpleLexer.g4        # lexer: PLUS, MINUS, IDENTIFIER, INTEGERLIT, WS
  SimpleTest.g4         # parser: options { tokenVocab = SimpleLexer; }
```

Result: **Zero warnings.** Token resolution works when the lexer `.g4` file is in the same directory as the parser grammar.

## Test 3: Isolating the variable

To confirm the issue was directory layout (not grammar complexity), I tested progressively complex lexers in `Core/` -- `caseInsensitive = true`, `@members` with C# code, multiple lexer modes, and all three combined. **All worked with zero warnings** when co-located in the same directory.

## Final grammar file structure (WORKS for tokens)

I moved the lexer grammar into `Core/` alongside the parser fragments and added `tokenVocab = CobolLexer` to each imported grammar:

```
Grammar/
  CobolParserCore.g4         # master parser: tokenVocab = CobolLexer; import CobolExpressions, ...
  CobolDialect.g4            # dialect-specific parser rules
  CobolParserGenerics.g4     # generic/template extension rules
  CobolParserJsonXml.g4      # JSON/XML extension rules
  CobolParserOO.g4           # OO COBOL extension rules
  CobolPreprocessor.g4       # preprocessor grammar
  Core/
    CobolLexer.g4            # lexer grammar (MOVED HERE from Grammar/)
    CobolExpressions.g4      # options { tokenVocab = CobolLexer; } (ADDED)
    CobolData.g4             # options { tokenVocab = CobolLexer; } (ADDED)
    CobolControlFlow.g4      # options { tokenVocab = CobolLexer; } (ADDED)
    CobolIO.g4               # options { tokenVocab = CobolLexer; } (ADDED)
    CobolSpecialNames.g4     # options { tokenVocab = CobolLexer; } (ADDED)
    CobolReportWriter.g4     # options { tokenVocab = CobolLexer; } (ADDED)
    CobolExtensionsJsonXml.g4 # options { tokenVocab = CobolLexer; } (ADDED)
```

Result: **All "Unknown token reference" errors are gone.** The ANTLR4 tool still generates correctly (updated the build script paths). All 216 unit + 183 integration + 60 NIST tests pass.

## What this solved and what it did not

**Solved:** All token reference warnings across all 7 imported grammar fragments. The extension now resolves all 300+ token types correctly.

**Not solved:** Cross-file parser rule references between sibling imported grammars. For example, `CobolExpressions.g4` references `dataReference` which is a parser rule defined in `CobolParserCore.g4`. The extension reports these as "Unknown parser rule" because it doesn't know that `CobolExpressions.g4` and `CobolParserCore.g4` share the same master grammar context. Similarly, sibling imports can't see each other's rules.

This second issue is the import-chain dependency problem from my original report. The extension processes each grammar file independently and doesn't propagate rule visibility through the `import` relationship. The `references` array in `SourceContext.ts` tracks which grammars import a given grammar, but this reverse lookup isn't used during rule resolution.

## Summary

| Issue | Status | Solution |
|-------|--------|----------|
| Token references in imported grammars | **Solved** | Move lexer `.g4` to same directory as parser fragments + add `tokenVocab` to each |
| Cross-file parser rule references | **Open** | Requires extension to propagate rule visibility through the import chain |

The token issue has a viable user-side solution (co-locate files + add `tokenVocab`). The parser rule issue does not -- it requires an extension change to resolve rules through the importing grammar's context.

Note that a PR to propagate the importing grammar's dependencies to imported grammars (through the existing `references` chain in `SourceContext.ts`) would fix **both** issues in one change. Imported grammars would inherit the master grammar's `tokenVocab` and see sibling imports' rules -- matching the ANTLR4 tool's resolution semantics. This would also eliminate the need for the co-location workaround.

Would you be open to such a PR?
