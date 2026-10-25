## PB3308 recurs as a sustained outage: the aka.ms SDK link's origin refuses public access, and every code merge group fails

From about 18:50Z on 2026-10-25 every merge-group run whose matrix runs failed `Setup .NET 10` in every building job:
`dotnet-install` follows the aka.ms link to `dotnetorigin.blob.core.windows.net`, which answers `409
PublicAccessNotPermitted`, retries the same URL three times and gives up, while `builds.dotnet.microsoft.com` serves the
same SDK. PRs #576 and #578 bounced (#578 twice) and the code groups of #580 and #581 failed the same way; notes-only
groups skip the matrix and kept merging. kb/Work PB3308 records the recurrence, is raised to MAJOR, and its fix shape now
names a feed that bypasses the aka.ms redirect plus a cache of the installed SDK, since a retry would not have helped.
No finisher was dispatched for the bounced branches (infrastructure, recorded by `landing_measure.py record`).
