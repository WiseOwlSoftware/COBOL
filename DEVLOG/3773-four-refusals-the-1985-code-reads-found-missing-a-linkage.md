## Entry 3773 — 2026-11-16 09:13 PST — Four refusals the 1985 code reads found missing: a Linkage subscript, an INSPECT counter slice, a report group's JUSTIFIED and a reserved COPY name

Source the standard forbids compiled in four places (kb/Work PB5846, PB5859, PB5850, PB5848). Each was re-probed on this tree first and reproduced.

**PB5846, a Linkage item that is no USING operand, as a subscript or reference-modification operand (every edition).** `SubscriptBinder.ResolveSubscriptName` resolved the name with no linkage screen; `T (L2-N)` compiled and read storage with no argument behind it. The position walk now queues `NameResolution.ScreenLinkageReference` with its other screens, so an abandoned walk (the D18 route screens the operand through `Identify`) reports once. Sweep: of the six binder files that look a name up, the other five are data-division or probe lookups; a drift test (`ReferenceIdentificationDriftTests`) closes the set, so a new lookup must ask the screen or be named with the reason it is no procedure-division reference. Witnesses: two refusals and a control at 85 (fixed form) and at 2023.

**PB5859, INSPECT TALLYING with a reference-modified counter (every edition).** `CheckInspectTallyCounter` read the base item's PICTURE, so `N1 (1:2)` passed the binder and Roslyn refused the C# (CS1503, exit 70). `ItemCategory.IsElementaryNumericReference` now answers class numeric of the place, and the message cites the edition under compile (1985 VI.6.18.3 SR6 at --std 85). Sweep: 23 numeric-operand positions probed with a sliced operand at --std 85; only INSPECT fell through to C#. `RefModCategoryDriftTests` keeps the raw base-PICTURE class-numeric read out of the verbs and the statement validation.

**PB5850, JUSTIFIED and BLANK WHEN ZERO on a report group entry below level 01 at --std 85.** `ScreenReportEntryFormats` refused no group that wrote either; it does now, from 1985 VI.5.6.3 SR1, VI.5.4.3 SR1 and XIII.3.9.4 GR2. 2002 and later keep SR15's refusal.

**PB5848, a COPY text-name or library-name that is a reserved word.** The names never reached the reserved-word screen. `VersionConformancePass.FlagCopyNameReservedWords` asks the one table of the edition under compile of each name the preprocessor recorded, through the same `ReportReservedWord` as the tree walk (COBOL0901). A user-defined word below 2002, a system-name from 2002 (2023 8.3.2.4.1, 8.3.2.3.1), so the 2023 half holds too. A literal name is not asked.

Rows: witnesses recorded on 1985-GR-X.5.1-4, 1985-SR-VI.6.18.3-6, 1985-SR-VI.5.6.3-1, 1985-SR-VI.5.4.3-1, 1985-SR-IV.4.2.2.1-2 and 2023 SR-13.7.3-4, SR-14.9.22.3-5, SR-8.3.2.1-1, SR-8.3.2.1-5. Public API baselines and the 37 characterization cases are green; the gate's verdict is recorded at landing.

