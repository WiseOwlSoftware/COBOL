Thanks for the quick reply. I tested your suggestion thoroughly and have concrete results. Your proposal works great when all grammar files are co-located in the same folder. However, it required me to slightly re-organize my file structure and only eliminated half the issues.

## My original grammar file structure

14 grammar files (3,225 lines total), split across two directories:

| File | Lines | Purpose |
|------|------:|---------|
| CobolParserCore.g4 | 775 | Master parser + top-level rules |
| CobolIO.g4 | 493 | File I/O statements + FILE-CONTROL |
| CobolLexer.g4 | 448 | Lexer (tokens + SUBSCRIPT mode) |
| CobolData.g4 | 273 | Data division rules |
| CobolExpressions.g4 | 255 | Expressions, conditions, comparisons |
| CobolControlFlow.g4 | 198 | PERFORM, IF, EVALUATE, GO TO, etc. |
| CobolParserOO.g4 | 160 | OO COBOL (parse-only) |
| CobolParserJsonXml.g4 | 118 | JSON/XML extensions (parse-only) |
| CobolSpecialNames.g4 | 111 | SPECIAL-NAMES clauses |
| CobolParserGenerics.g4 | 101 | Generic/template extensions |
| CobolDialect.g4 | 100 | Dialect-specific rules |
| CobolPreprocessor.g4 | 98 | Preprocessor grammar |
| CobolReportWriter.g4 | 68 | Report Writer (parse-only) |
| CobolExtensionsJsonXml.g4 | 27 | Extension stubs |

With this file structure:

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

In this layout, every imported grammar in `Core/` showed hundreds of "Unknown token reference" errors because the extension couldn't resolve tokens -- the lexer was in the parent directory. The 5 standalone grammars in `Grammar/` (CobolDialect, CobolParserGenerics, CobolParserJsonXml, CobolParserOO, CobolPreprocessor) don't exhibit the issue because they're still in development and aren't yet imported by the master grammar. The extension analyzes them independently and they don't reference tokens from `CobolLexer.g4`. Only the 7 `Core/` files that are `import`-ed by `CobolParserCore.g4` are affected. Once the in-development grammars are wired into the import chain, they'll need to be co-located as well -- the workaround doesn't scale as the grammar grows.

## Test 1: Your proposed solution with the original layout (FAILS)

I added `options { tokenVocab = CobolLexer; }` to `Core/CobolExpressions.g4` while the lexer remained in `Grammar/`.

Result: VSCode still shows **151 "Unknown token reference" errors.** The extension searches for the lexer grammar relative to the parser grammar file's directory. Since `CobolLexer.g4` was in `Grammar/` but the parser fragment was in `Grammar/Core/`, the extension couldn't find it.

I also tried copying `CobolLexer.tokens` into `Core/` -- same result. The extension needs the actual `.g4` file, not just the `.tokens` file.

## Test 2: Same-directory baseline (WORKS)

I created a minimal test case with all files in one directory:

```
Core/
  SimpleLexer.g4        # lexer: PLUS, MINUS, IDENTIFIER, INTEGERLIT, WS
  SimpleTest.g4         # parser: options { tokenVocab = SimpleLexer; }
```

Result: **Zero warnings.** Token resolution works when the lexer `.g4` file is in the same directory as the parser grammar.

## Final grammar file structure (WORKS for tokens)

I moved my lexer grammar into `Core/` alongside the parser fragments and added `tokenVocab = CobolLexer` to each imported grammar:

Result: VS Code now shows no "Unknown token reference" errors. The ANTLR4 tool still generates correctly (updated the build script paths). All tests pass.

## What this solved and what it did not

**Solved:** All token reference warnings across all 7 imported grammar fragments. The extension now resolves all 300+ token types correctly.

**Not solved:** Cross-file parser rule references between sibling imported grammars. For example, `CobolExpressions.g4` references `dataReference` which is a parser rule defined in `CobolParserCore.g4`. The extension reports these as "Unknown parser rule" because it doesn't know that `CobolExpressions.g4` and `CobolParserCore.g4` share the same master grammar context. Similarly, sibling imports can't see each other's rules.

This second issue is the import-chain dependency problem from my original report. The extension processes each grammar file independently and doesn't propagate rule visibility through the `import` relationship. The `references` array in `SourceContext.ts` tracks which grammars import a given grammar, but this reverse lookup isn't used during rule resolution.

## Summary

| Issue | Status | Solution |
|-------|--------|----------|
| Token references in imported grammars | **Solved** | Move lexer `.g4` to same directory as parser fragments + add `tokenVocab` to each |
| Cross-file parser rule references | **Open** | Requires extension to propagate rule visibility through the import chain |

The co-location solution is also fragile: it only works when all imported grammar fragments and the lexer are in the same directory. The extension resolves `tokenVocab` relative to each grammar file's own directory, so any layout where the lexer is in a parent directory, a peer directory, or a subdirectory relative to an imported grammar will fail. Our original layout (lexer in `Grammar/`, fragments in `Grammar/Core/`) is a natural and common structure that the ANTLR4 tool handles correctly via `-lib`, but the extension cannot.

A PR to propagate the importing grammar's dependencies to imported grammars (through the existing `references` chain in `SourceContext.ts`) would fix **both** issues in one change and work regardless of directory structure. Imported grammars would inherit the master grammar's `tokenVocab` and see sibling imports' rules -- matching the ANTLR4 tool's resolution semantics.

Would you be open to such a PR?
