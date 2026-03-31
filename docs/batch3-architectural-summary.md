# Batch 3 Architectural Summary

**Date**: 2026-03-30
**Items**: M407 (CURRENCY SIGN WITH PICTURE SYMBOL), M411 (SCREEN SECTION)
**Status**: Design complete, awaiting implementation authorization

---

## Architectural Risks

| Risk | Severity | Mitigation |
|------|----------|------------|
| PICMODE exploit may fail if ANTLR rule matching changes | Low | PIC_STRING rule is a simple regex; "SYMBOL" is unambiguous non-whitespace. Well-tested by existing PIC string handling. |
| PicEnvironment constructor change breaks CIL emission | Medium | CilExpressionEmitter hardcodes the PicEnvironment ctor signature. Adding CurrencyOutputChar changes the ctor. Must update CIL emission and PicDescriptor ctor simultaneously. |
| New lexer tokens (SCREEN, BELL, etc.) shadow IDENTIFIER | Medium | Keywords like SCREEN, AUTO, FULL could be used as data-names in existing programs. Placing them before IDENTIFIER makes them reserved. Programs using these as names will break. Mitigation: accept as reserved words per COBOL spec. |
| SCREEN SECTION scope creep | High | Full screen I/O runtime is a large feature. M411 scope must be limited to grammar + basic semantics. Runtime is a separate item. |
| Regression in 95 NIST + 922 unit + 311 integration tests | Medium | All grammar changes must pass guard.sh. Lexer token additions are the highest-risk area (new keywords shadowing identifiers). |

---

## Architectural Dependencies

```
M407 dependency chain (linear):
  CobolSpecialNames.g4
    -> PicEnvironment.cs
    -> SemanticBuilder.cs
    -> SemanticModel.cs
    -> Compilation.cs
    -> PicUsageResolver.cs
    -> PicDescriptorFactory.cs (no change, just passthrough)
    -> PicRuntime.cs
    -> CilExpressionEmitter.cs

M411 dependency chain:
  CobolLexer.g4 (new tokens)
    -> CobolScreen.g4 (new file)
    -> CobolParserCore.g4 (import)
    -> CobolData.g4 (screenSection in dataDivision)
    -> SemanticBuilder.cs (new visitor)
    -> SemanticModel.cs (new ScreenSection property)
    -> BoundScreenItem.cs (new file)

Cross-dependencies:
  M407 and M411 are independent -- no cross-dependency.
  M407 should be done first (smaller, cleaner, fewer files).
```

---

## Expected Binder/Runtime Touchpoints

**M407 (8 files modified, 0 new):**

| Layer    | Files                                                         |
|----------|---------------------------------------------------------------|
| Grammar  | CobolSpecialNames.g4                                          |
| Runtime  | PicEnvironment.cs, PicRuntime.cs                              |
| Compiler | SemanticBuilder.cs, SemanticModel.cs, Compilation.cs, CilExpressionEmitter.cs |
| Shared   | PicDescriptor.cs (constructor change for CIL ctor signature)  |

**M411 (5 files modified, 2-3 new):**

| Layer     | Files                                                            |
|-----------|------------------------------------------------------------------|
| Lexer     | CobolLexer.g4 (~16 new tokens)                                  |
| Grammar   | CobolScreen.g4 (NEW), CobolData.g4, CobolParserCore.g4          |
| Compiler  | SemanticBuilder.cs, SemanticModel.cs                             |
| New types | BoundScreenItem.cs (NEW), possibly BoundScreenSection.cs (NEW)   |
| Generated | CobolParserCore.cs (regenerated from grammar)                    |

---

## Expected Test Surface Area

| Category                  | M407 | M411 | Total |
|---------------------------|------|------|-------|
| Parser tests (positive)   | 4    | 12   | 16    |
| Parser tests (negative)   | 2    | 2    | 4     |
| Binder tests              | 5    | 6    | 11    |
| Runtime formatting tests  | 5    | 0    | 5     |
| Integration tests         | 3    | 3    | 6     |
| **Total new tests**       | **19** | **23** | **42** |

---

## Expected NIST Impact

**Direct impact: Zero.** No NIST NC-series test exercises CURRENCY SIGN WITH PICTURE
SYMBOL or SCREEN SECTION.

**Indirect impact:**
- M407 enables future SM suite coverage (SPECIAL-NAMES features)
- M411 enables future screen I/O test programs
- Neither change affects the existing 95 NC pass rate

---

## New Sub-Gaps Discovered

| ID         | Title                                           | Severity | Notes |
|------------|-------------------------------------------------|----------|-------|
| (potential)| Screen I/O runtime (ACCEPT/DISPLAY rendering)   | P2       | M411 provides grammar + semantics only; runtime terminal I/O is separate |
| (potential)| CRT STATUS data item handling                   | P3       | ACCEPT screen sets CRT STATUS; requires runtime wiring |
| (potential)| CURSOR clause runtime wiring                    | P3       | SPECIAL-NAMES CURSOR clause interacts with screen ACCEPT |
| (potential)| Multi-character currency strings (COBOL-2002+)  | P3       | M407 handles single-char only; multi-char needs PicRuntime expansion |
| (potential)| Lexer keyword shadowing mitigation              | P2       | New SCREEN/AUTO/FULL tokens may shadow existing data-names |

---

## Implementation Order

1. **M407 first** -- smaller scope, linear dependency chain, no new files
2. **M411 second** -- larger scope, new lexer tokens (regression risk), new grammar file
3. **Guard gate** -- full test suite after each item, not just at the end
