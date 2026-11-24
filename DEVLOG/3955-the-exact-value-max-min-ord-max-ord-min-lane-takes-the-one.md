## Entry 3955 — 2026-11-22 22:00 PST — The exact-value MAX/MIN/ORD-MAX/ORD-MIN lane takes the one selection kernel

kb/Work PB6055. PB2635 unified the leftmost-tie selection loop as `ExtremeIndex` over an `ISelectionOrder`, but
`CobolIntrinsics.ExactValue.cs#SelectExact` kept a private copy for the native SDIDI lane over `CobolExact`. Re-probe on
this build (MAX (0.5 0.5E0 0.25E0) = 0.5, ORD-MIN (0.5 0.25E0 0.25E0) = 02, ORD-MAX (0.5E0 0.5 0.25E0) = 01) matched the
spec's leftmost-tie rule (§2023:15.59.4, §2023:15.71.4, §2023:15.72.4) before and after, as expected of a behaviour-neutral change.
Fix: a new `ExactOrder` (`CobolExact.Compare`) in `CobolIntrinsics.Selection.cs`; `SelectExact` is deleted and its six
callers (MaxExact, MinExact, OrdMaxExact, OrdMinExact, MIDRANGE, RANGE) call `ExtremeIndex`. No golden: no output changes.
No inventory rows closed. Sibling sweep: one more copy stood, `CobolIntrinsics.Wide.cs#SelectWide` (the aligned 256-bit lane of
MAX, MIN, ORD-MAX and ORD-MIN, which a power argument reaches at every edition); it is deleted the same way, its four callers
passing a new `WideOrder` (`CompareAligned`) to `ExtremeIndex`, and a search for a leftmost-strict loop in `src` now finds only
the kernel. `SelectionRuleCarrierAgreementTests` pins the exact-value and the wide lanes to it, ties and -0 against 0 included.
The landing refused the first tip (`code_read_landing.py`, kb/Work PB6056): five stored 1985 code reads named `SelectExact`.
They were re-read on the branch against the 1985 text (§1989:A.2.27.4, §1989:A.2.32.4, §1989:A.2.33.4 with the simple-condition
rules of §1985:VI.6.3.1.1), with probes at `--std 85` of the exact, wide, aligned and string lanes, and imported with
`code_reads.py import --replace` (slice 1985-c05-pb6055): 5 CONFORMS, the refuter upheld 5.

