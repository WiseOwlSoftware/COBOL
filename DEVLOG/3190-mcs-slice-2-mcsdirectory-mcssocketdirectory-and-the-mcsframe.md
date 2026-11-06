## Entry 3190 — 2026-11-05 20:15 PST — MCS slice 2: McsDirectory, McsSocketDirectory and the McsFrame codec (PB4735)

DESIGN-communication-facility.md §9 item 2 / DESIGN-message-control.md §6. New `src/WiseOwl.Cobol.Runtime/Mcs/` holds the three types both
messaging facilities share. Re-probe: PB4739 (the prerequisite) had landed; nothing of the slice existed, and `MessageStore.Open` read
`WISEOWL_COBOL_MCS` itself.

- `McsDirectory.Resolve(configured)`: configured, else the variable, else the current directory, absolute. `Canonical` walks from the root
  resolving each existing component's link (a target is itself walked; 40 links is a loop), upper-cased on Windows; `Identity` is the first 16 hex
  digits of its SHA-256. Both are lazy, so the communication facility, which only opens files, pays no walk. `MessageStore.Open` calls
  `Resolve(null).Path`; `MessageStore.DirectoryVariable` is deleted and `RuntimeConfig` names `McsDirectory.Variable` (one definition).
- `McsSocketDirectory`: `<base>/wiseowl-mcs-<uid>/<h>/` on Unix (base `$XDG_RUNTIME_DIR` if absolute, existing and `Fits(33)`, else `/tmp`), the
  temp path plus `wiseowl-mcs\<h>\` on Windows; directories 0700 and an existing one open to group or others is refused with
  `McsSocketDirectoryRefusedException`. `MaxPathBytes` is 107 (103 on macOS), and `PlatformAssumptionTests` now asserts it equals the measured limit.
  Design correction: `Fits` is a member of the resolved directory (its `<S>` varies), recorded in §6 with the exception type.
- `McsFrame`: `u32 length | u8 kind | payload`; the design did not say what `length` counts, §6 now says the payload; kind `Ring` only.
- Tests: `unit:McsResolutionTests` (14 cases: links in a middle/final component, chains, loops, case variants, identity vector, fallback boundary, worst
  cases, mode 0700 and refusal on Unix, frame encode/read/refusals). No COBOL-visible behaviour changed, so no conformance golden applies.
- Self-review: the frame's payload read allocates `length` bytes up front; unreachable until S2a adds kinds that carry payloads (a corrupt 2 GiB header
  would allocate), so S2a must read large payloads incrementally.

Review round 1: R1-1 (blocking) fixed by splitting McsDirectory.Resolve into the env-reading entry and a pure three-input overload, with TheEnvironmentVariableIsTheSecondChoiceAndTheCurrentDirectoryTheThird witnessing the precedence. R1-2 fixed: McsSocketDirectory.Create is internal and AnExistingUserLevelOpenToGroupOrOthersIsRefusedWhateverTheIdentityLevelIs covers the user level (Linux leg). R1-3 left as a design lead for the slices that consume the identity.

