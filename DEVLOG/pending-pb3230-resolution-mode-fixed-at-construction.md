## PB3230: what a reference resolution may do is fixed when its resolver is constructed

kb/Work PB3230 (an R2 finding of the PB1754 architecture review) named three pieces of ambient mutable state in the
reference-resolution subsystem that §8.3 (4) of DESIGN-architecture-review gave no owner. Re-probed at 27faa4c114, all three
were there: a `Probing { get; set; }` flag that `ResolveImpl`, `ProbeDeclaration` and two object-property sniffs saved,
set and restored, read at 50 lines of the subsystem; five settable delegates (`FreezeOdoExtent`, `MaterializeSegment`,
`RefModCheckingAt`, `BindPropertyObject`, `ApplyObjectView`) that `StatementBinder.AttachSegmentMaterializer` installed
with `??=` on the unit's shared resolver, plus the statement binder calling back into the resolver's `SegmentDeferred`;
and seven identity-keyed report sets, four in the resolver and its qualification part and three in `ConditionBinder`.

The statement site is now ONE interface, `IStatementSite`, passed to the `ReferenceResolver` constructor.
`StatementBinder` implements it and constructs its own procedure resolver, and the data-division resolvers are
constructed with `site: null`, so procedure and data-division resolution are two constructions. The materializer
returns a `SegmentValue` (a temporary, or none and whether it was unbuilt) and the resolver marks its own deferral.
A probe is a second construction of the funnel, `ReferenceResolver.Prober`, with the same binder, site and report ledger
and parts of its own. `Probing` is get-only: every speculative ask goes to the prober, and a commit asked while
probing goes back to the committer. One report per written reference is one ledger, `ReferenceReports`, keyed by the
written node's identity and a `ReferenceReportRule` and asked through `ReferenceResolver.ReportOnce`, which a probe is
never granted. It absorbed all seven sets. `ResolutionModeDriftTests` pins the three by reflection: no part holds a
delegate, the mode, the site, the prober and the ledger cannot be written, no part or the condition binder keeps a
`HashSet`, and `StatementBinder` is the one statement site.

The note also asked for each resolution's effects to come back as data, applied by one commit step, with the probe-only
readings in `OoBinder.OoBindPropertyObject` deleted. That part was not built, and §8.3 (4) now says why. The D18
materializer binds a whole expression through the statement binder: its diagnostics, its pre-ops and compiler temps
named in allocation order. A probe that ran it and then dropped the effects would renumber every later temporary and
reorder the diagnostic stream the §4.1 oracle compares in order. So the probe keeps its effect-free readings.

Behavior neutrality: `compare_oracle.py` reported `IDENTICAL — 23549 cases`. No inventory row changed
(`closes_rows: []`, behavior-neutral restructuring).
