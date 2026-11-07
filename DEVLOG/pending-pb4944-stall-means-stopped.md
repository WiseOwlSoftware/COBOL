## PB4944: a STALL should mean an agent stopped, not that it ran long

On 2026-11-07 the loop mailed about a dozen STALL notices across the fix lane and Mythos's 1985 campaign, and every one
was a healthy agent. The notice fires on elapsed time, and the 60-minute limit is the owner's sizing rule, so it mixes
"this agent stopped" with "this work is bigger than one link". Filed as kb/Work PB4944: STALL becomes no commit, gate-log
write or tool call for about 20 minutes, and over-size becomes a count in the PROGRESS line. Whether long campaigns get
their own limit is the owner's decision. No code changes in this entry.
