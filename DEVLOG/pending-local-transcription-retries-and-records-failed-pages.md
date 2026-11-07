## The local transcription backend retries and records a failed page instead of stopping the run

PB4912. `transcribe_pdf.py pages --local` stopped the whole run on the first Ollama HTTP error (HTTP 500 after 54 of 224 pages on 2026-11-06). Re-probed with a planted fake backend: the traceback reproduced (HTTPError uncaught in `local_pages`).

Fix at the one place: `local_pages` now sends each request through `post_with_retries` (3 tries, 20 s apart; OSError, HTTPException and ValueError count as a failed try). A page that fails every try, whose reply did not stop, or that the stamp check refused is recorded in `local.json` as `{model, error, at}` and the run goes on; the function returns the failed pages, `main` exits 1 for any, `status` lists them, and `pages <list> --local OTHER` retries them, the success replacing the failure record. Sibling arms swept: the batch path already retries through the SDK (`CLIENT_RETRIES`); the `done_reason` and stamp arms of the local path now record the same way.

Self-test (`test_transcribe_pdf.py`): flaky page, always-failing page, torn reply, non-JSON reply, status listing, retry on another model, exit codes. Closes no inventory row (tooling).
