// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.Exceptions;
using CobolNet.Runtime.IO;

namespace CobolNet.Runtime;

/// <summary>
/// The runtime base class of every emitted COBOL class (OO deep-dive D2 — <c>class Foo : CobolObject</c>, or
/// <c>: Base</c> whose own root is CobolObject; ISO/IEC 1989:2023 §11.2/§11.3). One reflection-free home for:
/// <list type="bullet">
/// <item>universal/dynamic INVOKE (<see cref="__CobolInvoke"/> — D10: a method-name held in a data item, or a
/// universal <c>OBJECT REFERENCE</c> receiver, cannot bind statically; each emitted class overrides with a
/// switch over its own method roster and chains to <c>base.__CobolInvoke</c>, so the search follows the class
/// hierarchy exactly like §9.3.6 method resolution — AOT/WASM-safe, no reflection);</item>
/// <item>the null-receiver guard (<see cref="RequireNonNull"/> — §14.9.23.4 GR5: INVOKE on a null object
/// reference raises EC-OO-NULL through the landed §14.6.13 EC engine, never a raw
/// <see cref="NullReferenceException"/>);</item>
/// <item>object identity/NULL/<c>IS class</c> semantics ride C# directly (<c>is null</c> /
/// <c>ReferenceEquals</c> / <c>is Class</c>) — no members needed here.</item>
/// </list>
/// Rejected alternative (D2): deriving straight from System.Object — universal dispatch would then need
/// reflection or a marker interface, defeating AOT safety.
/// </summary>
public abstract class CobolObject
{
    /// <summary>Dynamic/universal method dispatch (D10/D-U2; §14.9.23.3 SR6/SR7): each emitted class
    /// overrides with a switch over the methods it DECLARES that are not overrides (an override wins through
    /// C# virtual dispatch from the BASE class's case; inherited methods resolve through the <c>default:</c>
    /// base chain — the chain IS §9.3.6 resolution order), unboxing each <see cref="CobolInvokeArg"/> after
    /// a descriptor-equality check (§14.9.23.4 GR7c — runtime conformance; mismatch → EC-OO-UNIVERSAL,
    /// Table 13 fatal). Reaching THIS default means no class in the hierarchy declares the name →
    /// EC-OO-METHOD (GR7b). <paramref name="returning"/> is null exactly when the INVOKE had no RETURNING —
    /// presence mismatches in either direction are EC-OO-UNIVERSAL in the generated cases.</summary>
    public virtual void __CobolInvoke(string name, CobolInvokeArg[] args, CobolInvokeArg? returning) =>
        throw new CobolFatalException("EC-OO-METHOD",
            $"INVOKE: the runtime class '{GetType().Name}' does not implement a method named '{name}' "
            + "(ISO §14.9.23.4 GR7b)");

    /// <summary>Normalize an identifier-2 method-name value (§14.9.23.4 GR2a): the content IS a
    /// user-defined word — case-insensitive (§8.3.2.2), and the PIC X trailing-space padding is not part
    /// of the name. Generated case labels are the COBOL spelling uppercased.</summary>
    public static string NormalizeMethodName(string raw) => raw.TrimEnd().ToUpperInvariant();

    /// <summary>The INVOKE null-receiver guard (§14.9.23.4 GR5): emitted before every instance dispatch —
    /// <c>CobolObject.RequireNonNull(recv).M(args)</c> — so a null object reference raises EC-OO-NULL (fatal,
    /// Table 13) through the EC engine.</summary>
    public static T RequireNonNull<T>(T? receiver) where T : class =>
        receiver ?? throw new CobolFatalException("EC-OO-NULL",
            "INVOKE: the object reference used as the receiver is null (ISO §14.9.23.4 GR5)");

    // ── Per-object instance-file connectors (M2-OO-1i, ISO §9.1.4) ──────────────────────────────────────────
    // An OBJECT-paragraph (non-EXTERNAL) file connector belongs to the object instance: it is minted per object
    // (CobolFile.MintInstanceKey), registered in the emitted ctor, tracked here, and implicitly CLOSED when the
    // object is deleted (§9.1.4 — the runtime executes an implicit CLOSE "for file connectors in an object when
    // the object is deleted"; the §9.1.4 NOTE licenses this happening during garbage collection). The run-unit
    // CobolFile.CloseAll() at run-unit termination remains the backstop for objects not yet finalized.

    /// <summary>The minted connector keys of this object's instance files (null until the emitted ctor tracks the
    /// first one). Each class's ctor tracks its OWN files, so an inherited-plus-own object accumulates the whole
    /// chain here (the finalizer closes them all).</summary>
    private System.Collections.Generic.List<string>? __instFiles;

    /// <summary>Every COBOL object suppresses finalization by default — the overwhelming common case owns no files,
    /// and a finalizer on the universal object root would put EVERY object on the GC finalization queue. Only an
    /// object that actually tracks an instance file re-registers (<see cref="__TrackInstanceFile"/>), so the §9.1.4
    /// deletion-time CLOSE burdens the GC for exactly the objects that need it.</summary>
    protected CobolObject() => System.GC.SuppressFinalize(this);

    /// <summary>Record a per-object instance-file connector key for the deletion-time implicit CLOSE (§9.1.4).
    /// Called from the emitted object constructor after the file registers; the FIRST tracked file re-arms the
    /// finalizer this object suppressed at construction.</summary>
    protected void __TrackInstanceFile(string key)
    {
        if (__instFiles is null) { __instFiles = new(); System.GC.ReRegisterForFinalize(this); }
        __instFiles.Add(key);
    }

    /// <summary>The §9.1.4 implicit CLOSE at object deletion: request close of every instance-file connector this
    /// object owns. This runs on the GC finalizer THREAD, so it must NOT touch the single-thread file registries
    /// directly — it only ENQUEUES each key (<see cref="CobolFile.EnqueueInstanceClose"/>, lock-free); the mutator
    /// thread performs the actual close when it next drains (§9.1.4's NOTE licenses this GC-deferred timing; the
    /// run-unit <c>CobolFile.CloseAll()</c> drains + is the backstop). Reached only for file-owning objects (see the
    /// ctor's SuppressFinalize).</summary>
    ~CobolObject()
    {
        if (__instFiles is { } fs)
            foreach (var k in fs) CobolFile.EnqueueInstanceClose(k);
    }
}
