// Deliberate violations — the negative control for scripts/semgrep/invariants.yml.
//
// This file is NOT compiled (it is outside every .csproj). It exists so a rule that has silently stopped matching
// is caught: `scripts/semgrep/verify.py` asserts EVERY rule fires here, and that the greenfield tree's finding
// count has not grown. A rule that never fires reports green and is worse than no rule at all — an earlier draft
// of the rule set had exactly that failure, from an invented suppression construct that disabled two rules.
//
// Each violation is annotated with the rule it must trip. Keep one clean example per rule.

namespace CobolNet.SemgrepTestData;

public sealed record BoundFakeNode(int Id, string RenderedText);   // cobolnet-bound-node-carries-rendered-text

public class Violations
{
    private byte[] _persistedState = new byte[256];                // cobolnet-no-persistent-byte-storage

    public void Numerics()
    {
        decimal scaled = 1.5m;                                     // cobolnet-no-decimal
        System.Numerics.BigInteger wide = 2;                       // cobolnet-no-biginteger
        _ = scaled;
        _ = wide;
    }

    public void Diagnostics()
    {
        // TODO: this is the silent-no-op shape the loud-failure invariant forbids  <- cobolnet-no-silent-todo
        // The bypass shape is a bare code literal passed to an emit API, not merely the string existing:
        Emit(Severity.Error, "COBOLNET1999", "bypasses the catalog");   // cobolnet-raw-diagnostic-code-literal
    }

    private enum Severity { Error }
    private static void Emit(Severity severity, string code, string message) { }
}
