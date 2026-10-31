## Stall messages are filed as misrouted (PB4190)

On 2026-10-30 the operator loop's progress check sent the operator six STALL messages about the Mythos lane's wave
20261780, and three that afternoon about its PB4150 experiment. The operator cannot act on another lane's agents. The
cause is that the shared dispatch ledger (`inflight-groups.json`) records no owning lane, so `progress.py#post_stalls`
mails every stalled item to the operator. kb/Work PB4190 asks the ledger to record the dispatching lane and the reporter
to mail that lane.
