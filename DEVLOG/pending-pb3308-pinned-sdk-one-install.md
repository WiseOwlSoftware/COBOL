## PB3308: CI installs one pinned .NET SDK from the feeds, cached, never through the aka.ms redirect

From about 18:50Z on 2026-10-25 every code merge group failed `Setup .NET 10` in every building job, and PRs #576, #578,
#580 and #581 bounced. Each job ran `actions/setup-dotnet` with `dotnet-version: 10.0.x`, which resolves "the newest
10.0" through aka.ms. The redirect's storage origin answered `409 PublicAccessNotPermitted`, and dotnet-install retried
that one URL and gave up. The re-probe at 19:22Z still showed the 409. `builds.dotnet.microsoft.com` served the same
10.0.403 archives and their `.sha512` files, `ci.dot.net/public` answered 404, and the feed's own release metadata still
named 10.0.402. So "the newest 10.0" was not even a single answer, and a bare retry, the note's first fix shape, would have
hit the same 409 again.

The fix gives the SDK one install. `scripts/install-dotnet-sdk.sh` pins the exact SDK (10.0.403, a version global.json's
10.0.100 and `latestMinor` accept) and lists its feeds, builds.dotnet.microsoft.com first and ci.dot.net second. It
downloads the archive and its `.sha512` from the first feed that serves them and checks the SHA-512. It writes a
completion marker only after a full unpack. A transfer that breaks off is retried on the same feed, and a refusal moves on
to the next feed. An exact version never goes through aka.ms.

The composite action `.github/actions/setup-dotnet` replaces all six `actions/setup-dotnet` steps. It restores the SDK
from the Actions cache, keyed on the version, OS and architecture. It installs with the script on a miss, saves the SDK,
puts it first on PATH, and fails unless `dotnet --version` reports the pin. A merge group's ref is new every time, and
main's push run skips the matrix, so a new `dotnet-sdk-cache` job saves the SDK under main on every push, on Ubuntu and
Windows, where every merge group can restore it. A failed warm-up only warns. `setup-wsl.sh` now calls the script, and
`setup-env.sh`, which runs before the clone, carries the script's install block byte for byte. That makes kb/Work PB1957
row 8 (a): CI, WSL and the cloud image build with the same SDK patch.

`DotnetSdkInstallDriftTests` (Unit, a tree sweep) holds the whole shape. It checks that global.json accepts the pin under
its rollForward policy and that the feeds start at the CDN and never touch aka.ms. Every job that runs `dotnet` must use
the action first, and no workflow, action or setup script may name `actions/setup-dotnet`, `dotnet-install` or aka.ms. It
also pins the action's steps, the warm-up job and the setup-env block's equality. The script's `--self-test` runs the real
install over local file:// feeds (fallback, checksum, marker, retry, no retry on a refusal), and the checksum, fallback and
marker witnesses were each shown to fail on a mutated copy. The sibling sweep covered every other external fetch a CI job
makes (checkout, NuGet restore, pip, the GnuCOBOL testsuite, the vendored ANTLR jar), and none resolves a version through
a redirect. Self-review: one install procedure with one drift-pinned copy (setup-env.sh cannot reach the repository); no
hand edits to six steps. Owner actions remain: paste the new setup-env.sh into the cloud environment, re-run setup-wsl.sh,
and re-arm #576 and #578 once this lands. No inventory row (process tooling).
