## PB4998: the audit's batch requests are built in cache layers

Anthropic told the owner on 2026-11-08 that this account's prompt-cache hit rate was low. The direct API traffic is the
edition audit's Message Batches passes, and none of their requests carried a cache breakpoint. The route pass sent the
same 7,000-token list of 2023 clauses in each of its 247 requests, and the map pass repeated each 1985 clause's routed
candidates for every rule of that clause and kind, 47 % of its input on the last run.

`message_batches.py` now owns the cache: an hour's TTL, a cached text block and system, each reply's cache write and
read in its recorded usage, and `usd` pricing them, so the `--max-usd` guard sees what a cached run costs. `audit_batch.py`
builds each request in three layers: the pass's instructions as the cached system, the group's shared part as a cached
first user block (the route pass's clause list, a map request's clause candidates), and the item's own part last. A map
item now stores its clause candidates and its wording candidates apart. The transcriber's prompt is below Opus 5.5's
512-token minimum and is unchanged. The branch review's lead on the closest-first order case (it planted an order that
was already numeric) is fixed in the same test file.
