## Entry 3502 — 2026-11-11 15:09 PST — PB5335 filed: the code generator may read a level-66 alias's own empty description at five sites

The group C finisher (PB5120, worktree-wf_c902d12a-c75-3 at aafe84ed03) fixed the binder screens that asked
`Place.DenotedItem` for a RENAMES alias's attributes, and named five code-generator sites that read it the same way
(CallEmitter twice, ProgramEmitter, OoEmitter, ActivationDescriptions). It did not probe them. kb/Work PB5335 records the
lead as an analysis with the probe owed: a CALL BY REFERENCE of a non-THROUGH alias of a group against the group itself.
The PB5125 finisher's lead is recorded beside its note: its Mechanism's status-24 citation is corrected to the relative-file rule, §1985:VIII.1.3.4.

