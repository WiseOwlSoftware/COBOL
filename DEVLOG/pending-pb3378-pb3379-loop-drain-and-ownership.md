## PB3378, PB3379, PB3380: the loop's drain is carried by prose, branches have no owner, successor chains drop review fixes

The operator files three orchestration defects found on 2026-10-18 between 08:20 and 09:05.

- **PB3378:** the owner's drain reaches a wave unit only as a sentence in a steer or the last handoff. One handoff
  that dropped it planned wave 1120 with four new out-of-scope groups (PB3365, PB3358, PB3273, PB3355) beside the
  PB1154 finisher. The fleet launched at 08:56, and the operator wound it down with the loop's fleet stop at 08:58.
- **PB3379:** the loop reviews, and plans finishers for, any branch the lifecycle tool lists, whatever orchestrator
  owns it. It reviewed the Mythos lane's running wave-1118 group A2 (PB3332) and was about to finish it on Sonnet.
- **PB3380** (filed at the Mythos lane's request): in a rolling-wave successor chain, a successor merges its
  predecessor at the implementer-DONE head, while the predecessor's review finishers fix it on its own branch only.
  Wave 1118's A2 and A3 never received A's seven review fixes, and the lane stopped the wave at 09:03.

The notes ask for:
- a supervisor drain setting substituted into the unit's planning command;
- a branch owner recorded in the dispatch ledger and honoured by the loop;
- one successor model that carries reviewed heads forward, pinned by a three-link test.

No code changes in this landing.
