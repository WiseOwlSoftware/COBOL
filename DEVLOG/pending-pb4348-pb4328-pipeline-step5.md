## PB4348 a cache saves only what its producer finished; PB4328 pipeline step 5 (in progress)

**PB4348 (the package-cache job saved a partial NuGet set under main's exact key).** Re-probed by restoring the old
workflow under the new audit: `CiBoundsDriftTests.NoCacheSavesWhatAFailedStepLeftBehind` fired on both saves of
`package-cache` (the NuGet packages after `dotnet restore`, the GnuCOBOL tarball after its fetch; both producers carry
`continue-on-error: true` and no `id`). Fix: the producers gained the ids `restore` and `fetch`, and each save runs only
`if: steps.<id>.outcome == 'success'`, which implies the old conditions (a producer skipped on a cache hit or a failed
warm-up is not `success`). The audit names the producer (the nearest earlier step that is not a cache step) rather than
every earlier `continue-on-error` step, which would have tied the tarball's save to `dotnet restore`. Sibling sweep
`git grep -n 'actions/cache/save@' -- .github`: three saves; the third, `setup-toolchain`'s, follows an `install` step
that does not continue on error. DESIGN-test-build-ci.md §1.4 states the rule.
