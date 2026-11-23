## The exact-value MAX/MIN/ORD-MAX/ORD-MIN lane takes the one selection kernel

kb/Work PB6055. PB2635 unified the leftmost-tie selection loop as `ExtremeIndex` over an `ISelectionOrder`, but
`CobolIntrinsics.ExactValue.cs#SelectExact` kept a private copy for the native SDIDI lane over `CobolExact`. Re-probe on
this build (MAX (0.5 0.5E0 0.25E0) = 0.5, ORD-MIN (0.5 0.25E0 0.25E0) = 02, ORD-MAX (0.5E0 0.5 0.25E0) = 01) matched the
spec's leftmost-tie rule (§2023:15.59.4, §2023:15.71.4, §2023:15.72.4) before and after, as expected of a behaviour-neutral change.
Fix: a new `ExactOrder` (`CobolExact.Compare`) in `CobolIntrinsics.Selection.cs`; `SelectExact` is deleted and its six
callers (MaxExact, MinExact, OrdMaxExact, OrdMinExact, MIDRANGE, RANGE) call `ExtremeIndex`. No golden: no output changes.
No inventory rows closed. Sibling sweep: `grep SelectExact src` returns nothing; every other carrier already used ExtremeIndex.
