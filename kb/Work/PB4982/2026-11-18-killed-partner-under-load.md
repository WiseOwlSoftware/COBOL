## A sibling: `AKilledPartnerEndsTheOtherEndsLinkAndNothingElse` failed once under load (listing step 4a, w20262104d)

On branch `listing-step-4a` (head 39e0347f1c, a change to the listing and the text stages that touches no messaging code), a
whole run of the Unit assembly (`test_population.py scrubbed dotnet test tests/WiseOwl.Cobol.Tests.Unit --no-build`, the
Conformance and RepoDrift assemblies following it on the same host) reported
`SocketTransportTests.AKilledPartnerEndsTheOtherEndsLinkAndNothingElse` Failed after 2 m, 1 of 41,583. The class alone, re-run
at once at the same build, passed 30 of 30 in 241 ms. Same shape as this note's case: a SocketTransport test that waits on the
partner's end, red only when the host is loaded, so the wait is the mechanism to fix, not the test to retry.
