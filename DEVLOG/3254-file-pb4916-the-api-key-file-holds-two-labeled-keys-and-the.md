## Entry 3254 — 2026-11-07 08:57 PST — File PB4916: the API key file holds two labeled keys, and the one reader returns the label

A note-only change. The owner put both Claude accounts' API keys into the one key file, each under a label line naming
its account. `scripts/api_key.py` returns the first non-empty line, which is now a label, so every tool that bills a call
would send the label as its key, and none can name which account to bill. PB4916 makes the one reader parse labeled
entries and select a key by account, requires the account whenever the file holds more than one key, and adds
`--api-account` to every caller.

