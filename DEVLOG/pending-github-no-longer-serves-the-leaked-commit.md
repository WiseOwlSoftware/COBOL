## GitHub no longer serves the leaked commit 0953e584ed

After support ticket #5047892, `gh api repos/WiseOwlSoftware/COBOL/commits/0953e584edd26146d642e42dc3359e9a983c6636` answers HTTP 422
"No commit found" (2026-11-24 00:06 UTC). PB6143 is landed; the push-time guard of PB6136 (on main since #2039) stops a repeat.
