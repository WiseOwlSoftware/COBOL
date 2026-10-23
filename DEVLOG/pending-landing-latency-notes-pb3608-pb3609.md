## Where pending branches wait: review latency, not landing (PB3608, PB3609 filed; PB3581, PB3584 review leads fixed)

The owner asked how to get pending branches to main efficiently. Measured at ~16:00 PDT: all three gate slots free, the
merge queue empty, and of the 20 branches the survey printed IN FLIGHT, 13 were already merged (PB3609: the survey's
180-minute age guard labels a merged branch IN FLIGHT; it should guard only deletion). About six were truly unlanded, and
the ones that were finished waited for a reviewer: the loop reviews only inside an `enqueue` unit, and units run one at a
time for an hour or more, while the landing lane starts a landing within three minutes (PB3608: a review lane beside the
landing lane). Two PB3340 branches with no shared history were found the same way; the Mythos lane kept the loop's
(`worktree-wf_f6f89a51-99e-1`) and PB3340's note records it.

The branch review of `fix-self-test-wall-leads` (CLEAN) left two LOW leads, fixed here: PB3581's saving is 2.4 minutes a
landing as measured (16.0 against the guard's 13.6 on pr-442; the run wall 16.4 to 14.1), not the 3 to 4 minutes
estimated before landing, and pr-445's guard cell reads 13.7; PB3584's design section now names the two files its Sites
paragraph names.
