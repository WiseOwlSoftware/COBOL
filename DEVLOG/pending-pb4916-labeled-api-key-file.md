## PB4916: the API key file holds one labeled key per Claude account, and every API tool names the account it bills

**Re-probe.** A planted key file of two labeled entries (invented labels, two keys) made `api_key.read_api_key` return
the first label: every tool that bills a Claude call (fuzz_claude through differential_fuzz, fuzz_batch,
spec/transcribe_pdf) would have sent the label as its key, and none could choose the account.

**The fix, one reader.** `read_api_key(path, account=None)` parses the file as entries: a key line is one token of
`KEY_SHAPE`, the non-key line before it is its label. A file with one key still needs no account. With several the
account is required and is matched as a whole word of a label, by its name in `model_rules.json` or by the sign-in
`account.py` reads from that account's config dir at run time (never written into the repository). No match, several, or
an account named for a file's one unlabeled key is a RuntimeError naming the path and the labels, never a key. It returns
an `ApiKey` whose `repr` leaves the key out and whose `account` is the account NAME billed. `read_run_key` keeps a run to
the account its record names, because a Message Batch belongs to the account that created it.

**The callers.** All four take `--api-account` with one help text (`api_key.ACCOUNT_HELP`). `fuzz_batch.py` records
`api_account` in `batch.json` and `collect` keeps it; `transcribe_pdf.py` records it in `state.json`, keeps it on every
later command and prints it in `status`; `differential_fuzz.py` records it in `run.json` through
`fuzz_claude.use_api_key_file(path, account)`. `docs/DOC_INDEX.md` and `DESIGN-differential-fuzz.md` describe the format.

**Witnesses.** Planted key files and fake clients only, no API call: the api_key self-test (a bare key, a byte-order
mark, a pair chosen by name and by sign-in, a pair with no account, an unknown account, a prefix of a sign-in, two labels
naming one account, the record's account kept and another refused, no key in any error), and the account cases in the
fuzz_claude, fuzz_batch, differential_fuzz and transcribe_pdf self-tests.

**Self-review.** One reader, one help text, one rule for a run's recorded account. An unreadable accounts table falls
back to matching by name, and the error that follows names the labels, so no key is chosen silently. The account lookup
restores the caller's `sys.path`. The gate's verdict is in the branch's gate record.
