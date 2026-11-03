## Entry 3043 — 2026-11-03 13:03 PST — PB4504: the differential fuzz can use a Claude model as its program writer

The differential fuzz asked a local Ollama model for every program, so it could not run in a cloud session, which has
no local model, and could not use a Claude model at all. The owner wanted the cloud credit used with "an appropriate
Claude model" writing the programs.

`scripts/fuzz_claude.py` is the Claude writer: `claude_chat` returns the same reply shape as `fuzz_ollama.ollama_chat`,
through `claude -p` with no tools, no session persistence, no project settings or MCP servers, the fuzz's own system
prompt, JSON output and a per-call dollar cap. The exchange so far goes in as one labelled turn, and every call's cost
adds to a meter. `differential_fuzz.py --writer claude --model <alias>` selects it, and `--max-usd` stops new programs
once the meter reaches the ceiling. Everything after the generation (extraction, repair rounds, verdicts) is unchanged,
because both writers return the same shape. Both scripts' self-tests cover the new paths.

