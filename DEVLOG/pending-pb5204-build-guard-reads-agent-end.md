## PB5204: the build guard reads a workflow agent's return instead of timing its transcript

The build guard held a build for 120 s after any subagent write, even when that write was a workflow agent's return.
That return is a tool result marked `toolEndsTurn`, after which the workflow never wakes the agent, so
`fleet_active_build.finished` now reads it and such an agent is never live. An Agent-tool subagent's `end_turn` is not
an end, since a background task's notification can wake it (41 such cases in the project's transcripts), so it keeps
the time windows.
