# PB2652, 2026-11-22: `--suppress` ships; two decisions (wave 20262139 group D)

**Re-probe (base d87e47fa1d).** `cobol pb595_turn_inside_format3_perform.cob --source-format free` prints
`warning COBOL2187: ...`, exit 0; adding `--suppress COBOL2187` printed `Unrecognized command or argument '--suppress'`, exit 1.

**What shipped.** One rule, `WarningSuppression` (`src/WiseOwl.Cobol.Frontend/Diagnostics/WarningSuppression.cs`): a name is a
diagnostic code, a catalog descriptor's `Id` or its `ResolvedSuppressKey`, matched without regard to case. Both sinks ask it before
they keep a warning: `DiagnosticBag.Add` (every way into the front end's bag, `AddBeforeEffects` included) and
`EditionContext.AddWarningOnce` (the one way a binder warning is kept). The identity a name other than the code needs now reaches the
sink: `Diagnostic.Descriptor` is stamped by `EditionContext.Error(DiagnosticDescriptor, …)`, `Warning(DiagnosticDescriptor, …)` and
`Declined`, which used to forward `descriptor.Code` alone. `CompilerDriver.Options.Suppress` carries the names (an expansion's own
compilation is given the same), the CLI's `--suppress NAME` (repeatable) fills it, and the listing's options section names it. A name
no diagnostic answers to (catalog codes, ids and keys, construct-register codes, front-end descriptor codes) is a CLI argument error.

**Decision 1: an error is never muted.** The owner question the finding kept ("may --suppress mute an Error-severity
recognized-not-implemented deferral, or only warnings?") is a design decision under the 2026-11-10 delegation, decided here: only
warnings (and information). An error refuses the program; a compilation that fails with its reason muted fails with no reason, and a
deferral that is muted while the compile still fails tells the user nothing. A name that also names an error is accepted (a removed
construct's code is an error strictly and a warning under `--permissive`) and mutes only the warnings it names.

**Decision 2: the §2023:4.2.2 conformance-checking suboption is deferred to its own note.** §2023:4.2.2: "This warning mechanism shall
provide a suboption for selection or suppression of checking for violations of the set of conformance rules specified in 14.8.2,
Parameters and 14.8.3, Returning items, and in 9.3.8.2.3, Conformance between interfaces." Those checks are errors today
(COBOL1683-1688, 1736, 2237, 2238, 2470, 2471), and suppressing a CHECK is not muting a line: the compile must then proceed past a
nonconforming argument, and what the generated activation does with it (the argument's representation crossing a formal of another
class) is a code-generation question no part of this change answers. `--suppress` does not touch them (they are errors). The suboption
is a GAP of §2023:4.2.2, filed as a lead in the wave report for its own note; it is not closed here.
