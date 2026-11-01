## kb/Work: PB4337 retired (the gap was only in the abandoned PB380 design), PB3987's fourth witness

**PB4337 retired.** The clerk filed PB4337 from the round-2 review of branch `worktree-wf_15d43d1b-129-9`. The note
said nothing reads `CollationKeyCache.DefaultConfig` through `CollationRuntime.Initialize`. The PB380 fix that landed
is PR #934, and PR #935 records 129-9 as ABANDONED. Main's `CollationRuntimeInitializeTests` drives `Initialize` and
reads `DefaultConfig` in both of its tests, so the gap belonged only to 129-9's `CacheConfigurationLatch` and does not
exist on main.

**PB3987 gains its fourth witness.** At about 02:39Z the enqueue unit sent a finisher into the bounced PR #930's
worktree. The finisher's first commit was a clean merge of main. That merge put the row back in WAITING TO LAND, and
the landing lane landed the branch from inside the finisher's worktree 85 seconds later. It reused the old gate, made
its own merge there, and re-queued #930, which bounced on the same red at 03:06Z. The race cost one merge-group run.
The lane still has no verb that holds a branch while its finisher works.
