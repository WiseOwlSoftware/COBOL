# CobolSharp Developer Log

A chronological narrative of building a production COBOL compiler from the ISO/IEC 1989:2023
specification, targeting .NET. This log captures the thinking, decisions, failures, breakthroughs,
and lessons learned — intended as source material for a series of articles.

---

## Entry 001 — 2026-03-13: The Beginning

### Context
Starting from a blank repository containing only the 1,261-page ISO/IEC 1989:2023 COBOL
specification PDF. The goal: build a production-quality, fully standards-compliant COBOL compiler
that targets .NET.

### Key Decisions Made

**Why .NET as the target platform?**
We considered several options — LLVM IR, JVM bytecode, native x86/ARM, and .NET CIL. We chose
.NET for several compelling reasons:

1. **Decimal arithmetic is built in.** COBOL lives and dies by exact decimal math. .NET's
   `decimal` type is 128-bit base-10 — it maps almost perfectly to COBOL's `COMP-3`/packed
   decimal. On LLVM or native targets, we'd have to build or import a decimal math library
   from scratch, which is a massive and error-prone undertaking.

2. **Precedent validates the approach.** Micro Focus Visual COBOL and Fujitsu NetCOBOL both
   target .NET in production. This isn't a research experiment — it's a proven path.

3. **Interop story.** .NET assemblies can be called from C#, F#, VB.NET. This means COBOL
   programs compiled by our tool can participate in modern .NET applications, which is the
   entire value proposition of a COBOL modernization tool.

4. **Runtime services for free.** Garbage collection, threading, I/O, string handling, and a
   massive standard library. We don't need to build a runtime from scratch.

**Why C# as the implementation language?**
Same ecosystem as our target. We can reference Roslyn's architecture for design patterns. The
tooling (debugger, profiler, IDE support) is best-in-class.

**Why hand-written recursive descent parser?**
COBOL's grammar is notoriously context-sensitive. `PICTURE` clauses contain characters that are
operators elsewhere. Area A/B rules in fixed-form affect parsing. Inline PERFORM creates scoping
that depends on paragraph ordering. Parser generators (ANTLR, yacc) struggle with these
ambiguities. Roslyn uses hand-written recursive descent for C# for similar reasons — the control
you get is worth the verbosity.

**Why Mono.Cecil for CIL emission?**
System.Reflection.Emit works but has a clunky API and limited PDB support. Mono.Cecil is the
industry standard for .NET IL manipulation (used by Unity, Fody, many others). It gives us clean
APIs for emitting instructions, defining types, and writing debug symbols.

### Architecture Sketch
We defined a 5-stage pipeline:
```
Source → Preprocessor → Lexer → Parser → Semantic Analysis → CIL Code Gen → .NET Assembly
```

This is deliberately traditional. No novel compilation techniques — the novelty is in handling
COBOL's enormous spec surface area correctly.

### The Scale of the Problem
The spec has 2,090 table-of-contents entries across 1,261 pages. For comparison, the C11 spec
is ~700 pages and the C# spec is ~800 pages. COBOL is genuinely one of the largest language
specifications in existence. We broke this into 6 phases with ~60 task groups to make it
tractable.

### What's Next
Phase 1: scaffold the .NET solution and get "Hello, World!" compiling from COBOL to a running
.NET executable. This is the proof-of-concept that validates every architectural decision above.

---

## Entry 002 — 2026-03-13: Expanding the Key Technical Decisions

### Why This Matters
The initial plan had a one-line-per-decision summary table for technical choices. That's fine for
quick reference but terrible for understanding *why* we made each choice. Since this project will
become an article series, and since future sessions need to understand the reasoning (not just the
conclusion), we expanded every decision into a full analysis.

### The Interesting Tensions

**The "transpile to C#" temptation (KTD-4):** It's genuinely appealing — let Roslyn handle all
the hard CIL work, and we just emit C# source. But COBOL's control flow kills this idea. How do
you express `PERFORM paragraph-a THRU paragraph-d` in C#? It means "execute all paragraphs from
a through d in source order, then return." There's no C# construct for that. You'd need labels
and gotos, computed dispatch, or some state-machine transformation — all of which produce
unreadable C# that's impossible to debug. Going straight to CIL means we can emit exactly the
branch instructions we need.

**The decimal problem (KTD-5):** This one has layers. The naive approach is "just use .NET
`decimal` for all numeric data." But then you hit REDEFINES — where a numeric field and an
alphanumeric field share the same memory location. Or group MOVE — where a group item containing
numeric and string subfields is bulk-copied as raw bytes. COBOL programs *routinely* inspect and
manipulate the byte-level representation of numeric data. You can't do that with a `decimal`.

So we went dual-layer: `byte[]` for storage (preserving the byte-level semantics COBOL depends
on), `decimal` for computation (leveraging .NET's built-in base-10 arithmetic). The
marshal/unmarshal cost is the price we pay — but it only occurs on arithmetic operations, which
are a small fraction of most COBOL programs' execution time (MOVEs dominate).

**Parser generators vs. hand-written (KTD-3):** This is the decision most likely to be
second-guessed. ANTLR is powerful and would save us thousands of lines of parser code. But we
identified five specific ways COBOL breaks parser generators — PICTURE clauses, Area A/B
rules, COPY/REPLACE preprocessing, PERFORM THRU scoping, and implicit scope terminators. Each
one requires context that grammar-driven parsers don't naturally provide. Roslyn's team made the
same choice for C# (which is far less context-sensitive than COBOL), and their reasoning
convinced us.

### What's Next
Same as before — Phase 1 implementation. But now the plan document is robust enough that someone
reading it cold understands not just *what* we're building, but *why* every major choice was made.

---

## Entry 003 — 2026-03-13: The Meta-Story — Human-AI Collaboration as Content

### The User's Request
The user made an important framing request: this project isn't just about building a compiler.
It's also about documenting how a human and an AI collaborate on a large, complex engineering
project — warts and all. Specifically:

- **Log AI missteps honestly.** When I (Claude) make wrong decisions, misunderstand instructions,
  produce incorrect code, or go down dead ends — document it. Don't minimize or be defensive.
- **Track friction points.** When the user gets frustrated, document what caused it and why.
- **Document the collaboration pattern itself.** This is research into human-AI pair programming.

### Known Patterns from Prior Projects
The user shared observations from previous AI-assisted projects that are worth recording because
they're likely to recur:

1. **Session drift**: The longer a session runs, the more the AI tends to go off-track. Responses
   become less precise, less aligned with the user's intent, and less productive. This is likely
   caused by accumulated context competing for attention and perhaps compaction artifacts.

2. **Fresh session ramp-up cost**: Starting a new session solves the drift problem but creates a
   new one — the AI starts cold and needs to rebuild context. This can waste significant time
   re-explaining the project state.

3. **Context compaction losses**: When conversation history gets compressed to fit the context
   window, important details get lost. Decisions that were thoroughly discussed earlier become
   forgotten, leading to re-litigation or contradictory behavior.

### Our Mitigations
We're running on Opus with 1M token context, which is substantially larger than previous models.
This should delay compaction significantly. But we're not relying on it — our defense is
external state:

- **PROJECT_PLAN.md**: Always reflects the true current state of what's done and what's next.
  A fresh session reads this file and immediately knows where to pick up.
- **DEVLOG.md**: Captures the reasoning and narrative so a fresh session understands *why*
  things are the way they are, not just *what* they are.
- **Persistent memory**: Claude's memory system carries critical instructions across sessions
  without needing to re-read them.
- **Detailed commit messages**: Git history itself tells the story of how the code evolved.

The hypothesis: with these four layers of external memory, the ramp-up cost for a fresh session
should be minutes, not the long re-orientation the user has experienced in past projects.

We'll see if this holds. If it doesn't, that failure is itself valuable content for the articles.

### A Note on Honesty
This is an unusual ask. Most AI interactions optimize for appearing competent. The user is
explicitly asking for the opposite — they want to see where the AI fails, what causes it, and
how recovery happens. This is more valuable for the articles than a sanitized narrative of
flawless execution. The compiler will work eventually regardless; the interesting story is the
journey and the collaboration dynamics.

---

## Entry 004 — 2026-03-13: Defense-in-Depth Against Context Loss (Claude's Summary)

After setting up the transparency and session management rules, here's the system we have in
place — four layers of external state that together should make any session (fresh or continued)
productive quickly:

| Layer | What it preserves |
|-------|-------------------|
| `PROJECT_PLAN.md` | Current state — what's done, what's next |
| `DEVLOG.md` | Reasoning — why things are the way they are |
| Persistent memory | Process rules — how to behave across sessions |
| Git commit messages | Code evolution — forensic trace of every change |

The 1M token context window on Opus should give us much longer productive sessions before
drift becomes an issue. But when it does happen, the commitment is to flag it rather than
quietly degrading. And if we need a fresh session, the ramp-up should be fast: read the plan,
read the latest devlog entries, check git log, and go.

This is a testable hypothesis. If it works, it's a replicable pattern for long-running
AI-assisted projects. If it doesn't, understanding *why* it failed is equally valuable.

---

*End of entries for 2026-03-13*
