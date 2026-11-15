## Entry 3562 — 2026-11-13 03:26 PST — PB5458: an image's git comes from the image's newest release, and the nightly `images` job tells a rollout from a manifest behind

The nightly of 2026-11-12 (run 43270832950) was red in its `images` job, `MISMATCH git-linux 2.56.1 — toolchain.env names
2.56.0`, while `python scripts/toolchain.py latest` printed `LATEST   git-linux 2.56.0` the same day, so `bump` could not
clear the red. The re-probe on 2026-11-13 showed why: `latest` read the git from the image's readme on actions/runner-images
`main`, which still described image 20261101.186.1 (git 2.56.0). actions/runner-images publishes each image as a release
tagged `ubuntu26/20261111.196` when GitHub starts rolling it out (a prerelease until the rollout completes) and moves
`main`'s readme only afterwards; the job's log names that release (`Image Release: …/releases/tag/ubuntu26%2F20261111.196`),
and the readme at that tag names git 2.56.1.

The fix gives an image's git one source, `toolchain.image_release`: the newest non-draft release tagged for the image
(prereleases included), and the git its readme names at that tag. `latest` and `bump` read it, and so does `check --scope
image`: a git that differs from the manifest is red only when the newest release carries it too (a red `bump` clears), a
`ROLLOUT` warning when the job ran on an image other than the newest release (the rollout's old image, or one newer than
any published release), and red when the release cannot be read. The branch review's lead R1-1 narrowed ROLLOUT to a
manifest that names the newest release's git: a manifest ahead of every release, or behind both the image and the
release, is red naming all three gits, two self-test cases that failed before that change and pass after it. Round 2's
lead R2-1 made that red's `bump` remedy true: an image's git follows its newest release either way (`Newest.state`, the
one decision `latest` and `bump` share), so `latest` reports a manifest ahead of the release AHEAD with exit 1 and `bump`
sets it back, while an installable tool still never moves backward. The nightly `images` step gains `GH_TOKEN`, since the
releases come from api.github.com. The design doc (DESIGN-test-build-ci.md §1.4 and its nightly table), the nightly
workflow's header and the module docstring now describe it. `toolchain.env` and `scripts/cloud/setup-env.sh` are
untouched: the move to git-linux 2.56.1 is kb/Work PB5459's bump, rerun after this lands.

The self-test's publisher answers now carry the 2026-11-12 shapes (a rolling-out prerelease, a draft, the arm64 and
plain-win25 tags, the stale `main` readme); before the fix two cases failed (`=== TOOLCHAIN SELF-TEST: FAIL — 2 ===`: git
read as 2.56.0 from `main`, and the readme-without-git case), after it the self-test passes with five new image-scope
cases (manifest behind, older image mid-rollout, image newer than every release, release unreadable, no release read when
the git matches). Live, `latest` now prints `BEHIND   git-linux 2.56.0 — the newest release is 2.56.1`. The sibling sweep
found no other tool whose `latest` source is a document about a release rather than the release or its install source.

