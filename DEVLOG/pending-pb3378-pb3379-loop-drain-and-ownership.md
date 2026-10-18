## PB3378-PB3380 filed, and the owner's SORT and file I/O test rule becomes MANDATORY-PRACTICES I11

The operator files three orchestration defects found on 2026-10-18 between 08:20 and 09:30, and puts the owner's
standing test rule into the skills.

- **PB3378:** the owner's drain reaches a wave unit only as a sentence in a steer or the last handoff. One handoff
  that dropped it planned wave 1120 with four new out-of-scope groups (PB3365, PB3358, PB3273, PB3355) beside the
  PB1154 finisher. The fleet launched at 08:56, and the operator wound it down with the loop's fleet stop at 08:58.
- **PB3379:** the loop reviews, and plans finishers for, any branch the lifecycle tool lists, whatever orchestrator
  owns it. It reviewed the Mythos lane's running wave-1118 group A2 (PB3332) and was about to finish it on Sonnet.
- **PB3380** (filed at the Mythos lane's request): in a successor chain, `wf_rolling_wave.js` never carries a
  predecessor's review fixes forward. A successor that is itself held is never landable when it returns, because the
  project's `landable` means "reviewed CLEAN" since PB3027. So `onResult` reviews and fixes the predecessor alone, on
  its own branch. Wave 1118's A2 and A3 carry none of A's seven review fixes (ancestry checked). The public base's
  `rolling-wave.js` reviews only the chain's tail and is unaffected. The workstream skill's successor bullet now
  describes this, and says to plan a chain deeper than a pair one link per wave until the fix lands.
- **The owner's test rule** (2026-10-17 ~22:25: "test cases for sort and file I/o must cover both the success and
  abnormal exception cases") becomes MANDATORY-PRACTICES **I11**, its one home.
  - The implementer spec template and the finisher brief carry it, and `check_practices.py` requires both, so every
    implementer and finisher sees it, campaign or not (PB3369's cases 2 and 3).
  - The review skill's branch form checks it.
  - PB3369 records what remains: the two campaign copies and a computed binding.
- Each note now names the skill files its fix must update in the same change (owner 2026-10-18: keep the skills in
  sync with every fix).

Checks: `check_practices.py` GREEN; `test_plan_wave.py` 239/239; `test_dispatch_guard.py` 23/23; `work.py check` clean.
