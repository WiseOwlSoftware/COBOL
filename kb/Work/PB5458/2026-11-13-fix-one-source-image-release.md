**Landing (wave 20262017 A, 2026-11-13).** An image's git now has ONE source, `scripts/toolchain.py` `image_release`:
the newest release of actions/runner-images tagged for the image (`ubuntu26/`, `win25-vs2026/`; `image_tag_prefix`), a
prerelease being rolled out included and a draft never, and the git its readme names AT THAT TAG (`image_readme(label,
ref)`). `latest` and `bump` read it through `_image_git`, and the nightly `images` job's `check --scope image` reads it
through `_image_moved`: an image git that differs from the manifest is RED only when the newest release carries it too
(the manifest is behind the release `bump` reads), a `ROLLOUT` warning (green) when the job ran on an image other than
that release, and RED when the release cannot be read. Branch review R1-1 (rule decided by the Mythos session,
2026-11-13): ROLLOUT holds only while the manifest names the newest release's git; a job on another image with the
manifest off the release (ahead of every release, or behind both image and release) is MISMATCH naming all three gits.

**Re-probe before the fix (2026-11-13):** `python scripts/toolchain.py latest` printed `LATEST   git-linux 2.56.0`; the
readme on `main` still named `Image Version: 20261101.186.1` / `- Git 2.56.0`, while the readme at tag
`ubuntu26/20261111.196` (a prerelease, created 2026-11-11T22:55Z) names `Image Version: 20261111.196.1` / `- Git 2.56.1`.
After the fix the same command prints `BEHIND   git-linux 2.56.0 — the newest release is 2.56.1`, so `bump` (kb/Work
PB5459's branch) now moves GIT_LINUX_VERSION to 2.56.1.

**Sibling sweep (every other `latest` source):** dotnet-sdk (Microsoft's release index, the publisher's own metadata),
python (actions/python-versions' manifest, the list setup-python installs from), jdk (Oracle's current-releases API),
pwsh, gh, jq, uv (GitHub `releases/latest`, the release itself), semgrep (PyPI, the index it installs from): each reads the
release or the install source, not a document about one. git-windows read the same lagging readme and is fixed by the
same change.

**Only GitHub's runner shows** that the `images` job, on the real image, prints OK or ROLLOUT rather than MISMATCH once the
manifest names the newest release's git; read the first nightly after this lands and PB5459's bump: `MISMATCH` with
`the newest … release … carries` means the bump has not landed; `ROLLOUT` lines are warnings and the job is green.
