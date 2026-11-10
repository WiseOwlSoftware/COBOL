## PB5204: the build guard reads whether a subagent has ended instead of timing its transcript

The build guard held a build for 120 s after any subagent write, even when that write was the agent's last. A transcript
states its agent's end (an assistant message with `stop_reason: end_turn`, or a workflow agent's tool result marked
`toolEndsTurn`), so `fleet_active_build.finished` now reads it and a finished agent is never live. The time windows remain
only for an agent whose state the transcript does not state. On the session's 20 real transcripts, all ended, none is live.
