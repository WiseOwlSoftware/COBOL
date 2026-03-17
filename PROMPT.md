# CobolSharp — Architectural Doctrine & Development Rules

This document governs all AI-assisted development on this project. Every rule exists because
it was violated, caught by the user, and corrected — often more than once. These are not
suggestions. They are hard requirements derived from 13+ sessions of building a production
COBOL compiler.

Read this file in full before making any code change.

---

## The Project

CobolSharp is a production COBOL compiler (ISO/IEC 1989:2023) targeting .NET 8+ CIL, implemented
in C#. The compiler pipeline is:

```
Source → Preprocessor → Lexer → Parser (ANTLR4) → BoundTree → IR (Lowering) → CIL (Mono.Cecil)
```

Key architectural layers:
- **BoundTreeBuilder** — Binds parse tree to typed, symbol-resolved bound nodes
- **Binder** — Lowers bound nodes to IR instructions (never emits IR directly from parse tree)
- **CilEmitter** — Emits .NET CIL from IR instructions

The canonical dispatch points (use these, never bypass them):
- `BindIdentifierWithSubscripts` — all identifier binding from parse tree
- `ResolveExpressionLocation` — all bound expression → IrLocation resolution
- `EmitLocationArgs` / `EmitLocationArgsWithPic` — all CIL location emission
- `IrLocation` hierarchy — all data storage references (IrStaticLocation, IrElementRef, IrRefModLocation)

---

## Architectural Doctrine

These four patterns have appeared repeatedly across the project's history. They are the primary
failure modes. Memorize them.

### Doctrine 1: Use the canonical abstraction — never route around it

When a canonical abstraction exists for a concept, every code path that touches that concept
MUST go through it. No exceptions. No shortcuts. No "just this once."

**Violations found in this project:**
- `EmitExpression` called `GetStorageLocation` directly instead of using `IrLocation`
- `LowerCondition` used `as BoundIdentifierExpression` instead of `ResolveExpressionLocation`
- `BindPrimaryExpression` extracted `IDENTIFIER().GetText()` instead of `BindIdentifierWithSubscripts`
- Arithmetic operand binding bypassed the identifier binder entirely
- `ACCEPT FROM DATE` initially bypassed lexer tokens
- `INSPECT` initially bypassed region abstraction

**The fix is always the same:** extend the canonical abstraction to handle the new case.
If no abstraction exists, create one before implementing the feature.

**The invariant:** "If something touches data bytes, it either already has an IrLocation,
or calls ResolveExpressionLocation first." This invariant applies to every abstraction in the
compiler — substitute the relevant concept.

### Doctrine 2: Every bug is a pattern — fix the pattern, not the instance

When you find a structural flaw in one place, assume it exists elsewhere until proven otherwise.
Compilers are systems. Systems repeat patterns.

**Procedure:**
1. Stop.
2. Identify the pattern (e.g., "direct identifier extraction bypassing binding").
3. Search the entire codebase for all instances (`grep`, `Grep` tool, etc.).
4. Replace every instance with the canonical abstraction.
5. Add regression tests that would catch any future recurrence.

Do all of this in ONE sweep. Never fix a single instance and move on.

**Violations found in this project:**
- Fixed one `IDENTIFIER().GetText()`, four more existed in arithmetic operand binding
- Fixed one `GetStorageLocation` bypass, others existed in emitter init code
- Fixed `FractionDigits` in one place, four more needed fixing
- Fixed ref-mod in `LowerMove` only, all other lowering methods needed it too

**The cost of single-instance fixes:** future regressions, inconsistent behavior, architectural
drift, tests that pass for the wrong reasons.

### Doctrine 3: Integrate at the abstraction boundary, never bolt onto leaves

When a feature touches multiple subsystems, integrate it at the abstraction layer, not at each
consuming leaf.

**Wrong:** Adding `if (source is BoundReferenceModificationExpression)` to LowerMove,
LowerDisplay, LowerAccept, LowerInspect, etc.

**Right:** Adding `BoundReferenceModificationExpression` handling to `ResolveExpressionLocation`
(one place), then all consumers get it for free.

**Violations found in this project:**
- Reference modification initially bolted onto LowerMove as a type-check cascade
- OCCURS initially wired into MOVE/DISPLAY only, not unified into IrLocation
- File I/O had legacy FileRuntime bolted next to CobolFileManager
- NEXT SENTENCE was impossible because sentences weren't modeled in the IR

### Doctrine 4: Every concept has exactly one dispatch point

Before making any code change, ask three questions:

1. **Is there a single, canonical dispatch point for this concept?**
   If yes: extend it. If no: create it. Never wrap around it.

2. **Is the type logic centralized or smeared across call sites?**
   If smeared: stop and refactor toward a unified resolver before adding the new case.

3. **Am I modifying a leaf when the concept is more general?**
   If yes: I'm bolting, not integrating. Step back and find the right abstraction layer.

If any answer is "yes, but I'll fix it later" — fix it now. "Later" is how technical debt
accumulates and wrappers rot.

---

## Development Rules

### Code Quality

- **Production quality always.** Never choose "simplest", "minimal blast radius", or "good
  enough for now." This compiler will be maintained for 5+ years. There is no existing user
  base requiring backward compatibility. Refactor anything, rewrite anything.

- **Never change valid COBOL source to work around compiler bugs.** If valid source fails to
  compile, the compiler is broken. Fix the compiler.

- **Implement from the spec.** When the user provides a spec, implement it exactly. Don't
  investigate whether existing code covers it, don't optimize by skipping parts, don't prove
  the spec is unnecessary.

- **Never claim spec compatibility without a citation.** Every output diff vs expected is a
  bug until an ISO spec section says otherwise.

### Layer Discipline

- **Binder produces bound nodes only, never IR.** Lowering turns bound nodes into IR. The
  CilEmitter turns IR into CIL. Don't skip layers.

- **When changing a type, propagate through ALL layers at once.** Never wrap the old type in
  the new type at call sites as a "transitional" step. Trace the data flow end-to-end, change
  all layers in one pass.

- **When adding a new variant, refactor the dispatch generically first.** Never add another
  `if (source is NewType)` branch to each caller. Add the case to the canonical dispatch
  method, and all callers get it for free.

### Grammar & Parsing

- **Never change grammar without explaining the problem and proposed solution to the user
  first and getting approval.**

- **Always make proper fixes.** If a token is needed, add it to the lexer. Never use
  `IDENTIFIER` as a catch-all.

- **Keep grammar documentation in sync** when changing ANTLR4 grammar files.

- **ANTLR picks the first matching alternative.** Put literals before `arithmeticExpression`
  in ambiguous rules.

### Testing

- **Every new statement must ship with parser + CIL emitter + output-verifying test together.**
  No partial implementations. All in the same commit.

- **Compile and test after every change, no exceptions.** Even if a change "shouldn't" affect
  behavior.

- **Every output diff vs expected is a bug** until a spec citation says otherwise. "Close
  enough" is never acceptable.

- **When debugging failures, do ONE test at a time.** Don't keep retrying batch approaches
  that fail.

### Process

- **Maintain DEVLOG.md** with narrative of decisions, failures, dead ends, and breakthroughs.
  This is source material for an article series on human-AI collaboration in compiler
  construction.

- **Log all AI missteps honestly** in DEVLOG.md. Be radically transparent. When you make a
  wrong decision, go down a dead end, or cause frustration — log it clinically. This is data
  collection, not self-flagellation.

- **Update PROJECT_PLAN.md** with progress after each session.

- **Write detailed, forensically-traceable git commit messages** describing all
  adds/changes/removals.

- **Manage context wisely.** Keep external docs updated for quick session resumption. Flag
  context drift early.

- **Never run unbounded filesystem searches.** Kill stale background tasks.

---

## The Meta-Rule

All four doctrines reduce to one principle:

**Respect the abstraction boundary.**

When you bypass it, you create a rogue path. When you fix one rogue path without scanning for
others, you leave landmines. When you bolt a feature onto leaves instead of integrating at the
boundary, you create N rogue paths at once. When you don't have a single dispatch point, you
don't have an abstraction boundary at all.

The compiler gets simpler every time you unify a pattern. It gets harder every time you add a
special case. Choose unification every time.
