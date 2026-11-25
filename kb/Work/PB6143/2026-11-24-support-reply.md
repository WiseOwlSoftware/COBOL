# PB6143 - GitHub Support's reply (ticket #5047892)

GitHub Support answered the ticket on 2026-11-23 at 23:29 UTC: it cleared the repository's unreferenced commits, since no ref
named the SHA, and said the commit link would answer 404.

Re-checked 2026-11-24: `gh api repos/WiseOwlSoftware/COBOL/commits/0953e584edd26146d642e42dc3359e9a983c6636` answers HTTP 422
"No commit found for SHA", and `https://github.com/WiseOwlSoftware/COBOL/commit/0953e584edd26146d642e42dc3359e9a983c6636`
answers 404. The ticket needs no reply; the note stays landed.
