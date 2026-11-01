## Entry 2890 — 2026-10-31 16:47 PDT — kb/Work: file PB4272 and PB4273, the backend seam's interface and assembly split

The owner proposed putting the code generator in a separate DLL so generators can be substituted behind one
interface, and corrected the first assessment: `InternalsVisibleTo`, not a public API, gives a backend assembly the
model's internals. Two notes join the PB2293 campaign. PB4272: the seam's own contract is Roslyn-shaped
(`BackendArtifact` returns Roslyn `Diagnostic`s, `WriteSource` is C#-only, `BackendFactory.For` takes the binder
host, and the model stores C# `@keyword` spellings through `SyntaxFacts`). PB4273, the campaign's last structural
step: a `WiseOwl.Cobol.Model` assembly that grants friend access to the backends only, `RoslynBackend` in
`WiseOwl.Cobol.Backend.Roslyn` and the CIL backend in `WiseOwl.Cobol.Backend.Cil`, composed at build time by the
CLI, so a backend that reaches the binder fails to compile. Run-time DLL loading is rejected: the contract is the
whole bound tree, runtime ABI and CLR naming, so backends ship in lockstep with the compiler.

