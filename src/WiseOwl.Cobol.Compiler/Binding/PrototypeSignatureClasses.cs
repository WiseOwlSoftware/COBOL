// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Runtime.CompilerServices;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Runtime;

namespace WiseOwl.Cobol.Binding;

/// <summary>
/// ⛔ THE TYPE OF A RESTRICTED PROGRAM- OR FUNCTION-POINTER, as ONE identity per signature (kb/Work PB2464).
/// ISO §13.18.60.4 GR25 — "A restricted program-pointer shall contain only the predefined address NULL or the
/// address of a program with the same signature as that identified by the specified program-prototype-name" — and GR26,
/// its function-pointer twin, type a restricted pointer by the SIGNATURE of the prototype it names, never by the
/// prototype's name: two pointers restricted to two differently named prototypes of one signature hold the same set of
/// values. §14.9.39.3 SR20 / SR22 therefore admit a SET between them
/// (<see cref="PrototypeSignatures.Same"/>), and §14.8.2.3.2's "If either is a restricted pointer, both shall be
/// restricted and of the same type" is the same question asked at an argument. Before this type every relation that
/// had no host to resolve a prototype name through (the §9.3.6 match of a UNIVERSAL INVOKE, whose two descriptions
/// travel to run time; the typed conformance comparator) compared the NAMES.
/// <para><b>The class.</b> The prototypes are partitioned into classes by the ONE same-signature test
/// (<see cref="PrototypeSignatures.Same"/>), as the coarsest stable partition (<see cref="Classify"/>: refined until a
/// round splits nothing, so the classes do not depend on the order prototypes are presented in). A prototype that took
/// ISO §12.3.8.4 GR10 c) / GR11 c) — the details come from the external repository, so there is no compile-time signature
/// to compare — is a class of its own, marked as such: <see cref="PrototypeSignatures.ExternalIdentity"/> of its own
/// name, which the conformance relations let conform to any restricted pointer of its kind, as
/// <see cref="PrototypeSignatures.Same"/> lets an unresolved signature conform (kb/Work PB3162).</para>
/// <para><b>The identity (DESIGN-external-repository §6.4; kb/Work PB2103).</b> A class's identity is CANONICAL and
/// group-independent: <see cref="PrototypeSignatures.CanonicalIdentity"/> of its minimized signature graph,
/// <c>SIG &lt;hex&gt;</c>. A group describes a method's <c>RETURNING PROGRAM-POINTER TO P1</c> and a separately compiled group
/// its receiver <c>TO P2</c> of one signature with ONE string, so the run-time relations (§9.3.6 match rule 3 e), SET's
/// validity, §14.9.4.4 GR3 d)) admit the pair GR25 makes one type — the founder's name each group's partition picked, the
/// identity this replaced, was comparable only between descriptions compiled in one group. While the partition is
/// refined, a pointer carries its prototype's PROVISIONAL identity, its block's position in the round
/// (<see cref="Namespace.Refine"/> compares by it); the canonical identities are stamped once the partition is stable.</para>
/// <para><b>Two kinds of member (§6.4 parts 1 and 2; kb/Work PB2100).</b> A GROUP prototype is one the compilation group
/// declares; a DECODED prototype is one a repository record describes (<see cref="RestrictedPrototype.Origin"/>,
/// <see cref="Admit"/>). Members are keyed by ORIGIN AND NAME (<see cref="PrototypeKey"/>): a group name is classified once
/// (§8.3.2.2: one externalized name, one program), and a decoded <c>P</c> is a member of its own, placed by
/// <see cref="PrototypeSignatures.Same"/> over its DECODED signature, never by the group's <c>P</c> — keying by name types a
/// decoded pointer by NAME, which GR25 forbids. <see cref="Admit"/> adds the decoded prototypes and RE-RUNS the refinement
/// over every member from ONE starting block, so a decoded <c>B</c> whose signature is
/// <see cref="PrototypeSignatures.Same"/> as the group's <c>B</c> lands in that block rather than founding a second one (two
/// types for one signature).</para>
/// <para><b>Scope.</b> One instance per compilation group (<see cref="BindSession"/>); the identities it stamps are the
/// same in every group that describes the same signatures.</para>
/// </summary>
internal sealed class PrototypeSignatureClasses
{
    private readonly Namespace _programs = new(function: false);
    private readonly Namespace _functions = new(function: true);

    /// <summary>Every pointer this instance stamps, group pointers first (the order they were presented in).</summary>
    private readonly List<DataItem> _pointers = [];
    private readonly HashSet<DataItem> _stamped = new(ReferenceEqualityComparer.Instance);

    /// <summary>⛔ THE PARTITION REFINEMENT (kb/Work PB3101; OO_DESIGN D6 item 7, the wave-1080 design decision):
    /// classify every prototype a restricted pointer of <paramref name="pointers"/> resolves to, and stamp each pointer
    /// with its class (<see cref="DataItem.PointerRestrictionIdentity"/>). Every resolved prototype starts in ONE block
    /// of its namespace; each round stamps every pointer with its prototype's CURRENT block and splits each block by
    /// <see cref="PrototypeSignatures.Same"/>, which compares the formals' and returning items' descriptions — and a
    /// formal that is itself a restricted pointer is described by the block its own prototype is in. A round that splits
    /// nothing is the coarsest stable partition: two prototypes share a class unless some finite path through their
    /// formals reaches a difference, the greatest fixed point "the same signature" is for prototypes that name each other
    /// or themselves (ISO §13.18.60.4 GR25: "the address of a program with the same signature as that identified by the
    /// specified program-prototype-name"; kb/Work PB2495). Called once per compilation group, after every forest's
    /// pointers have their <see cref="DataItem.RestrictedPrototype"/> (<c>BinderDriver.ResolvePointerRestrictions</c>).</summary>
    public void Classify(IReadOnlyList<DataItem> pointers)
    {
        Take(pointers);
        Run();
    }

    /// <summary>⛔ ADMIT DECODED PROTOTYPES INTO THE REFINEMENT (DESIGN-external-repository §6.4 part 2; kb/Work PB2100): add
    /// every prototype <paramref name="decodedPointers"/> reach — and, through their signatures, the whole graph of
    /// prototypes those name, so a recursive record is classified as the coarsest stable partition (kb/Work PB3101) — then
    /// RE-RUN the refinement over ALL members of both namespaces from one starting block, group members first in their
    /// original order, re-stamping every pointer this instance holds. It never appends the new members to the blocks the
    /// group's run left: <see cref="Namespace.Refine"/> only splits, so an incremental admit would give a decoded
    /// <c>B</c> whose signature is <see cref="PrototypeSignatures.Same"/> as the group's <c>B</c> a block of its own, two
    /// types for one signature, against §13.18.60.4 GR25.</summary>
    /// <param name="decodedPointers">The restricted pointers of a decoded record's forest, each holding a
    /// <see cref="DataItem.RestrictedPrototype"/> whose <see cref="RestrictedPrototype.Origin"/> is its decoded origin.</param>
    public void Admit(IReadOnlyList<DataItem> decodedPointers)
    {
        foreach (var pointer in decodedPointers)
            if (pointer.RestrictedPrototype is { Origin: null } group)
                throw new InvalidOperationException($"PrototypeSignatureClasses.Admit was given a pointer restricted to the group prototype '{group.ExternalizedName}'; a group pointer is classified by Classify.");
        Take(decodedPointers);
        Run();
    }

    /// <summary>The canonical identity of a prototype's signature class (<see cref="PrototypeSignatures.CanonicalIdentity"/>).
    /// A prototype <see cref="Classify"/> or <see cref="Admit"/> partitioned answers its block's; a GROUP prototype no
    /// restricted pointer names (a program-address-identifier's prototype, asked by the procedure binder) joins the class
    /// whose founder it has the same signature as, under the identities the partition stamped, or founds one of its own — no
    /// stamped pointer is restricted to it, so the partition stays stable. A decoded prototype not yet admitted is a defect of
    /// the caller.</summary>
    public string Identity(RestrictedPrototype prototype) =>
        SpaceOf(prototype).BlockOf(prototype) is { } block ? Canonical(block) : PrototypeSignatures.ExternalIdentity(prototype.ExternalizedName);

    /// <summary>A block's canonical identity, computed once its partition is stable (and again after an
    /// <see cref="Admit"/> re-ran it).</summary>
    private string Canonical(Block block) =>
        block.Canonical ??= PrototypeSignatures.CanonicalIdentity(block,
            b => (b.Function, b.Founder.Signature),
            pointer => pointer.RestrictedPrototype is { } p ? SpaceOf(p).Known(p) : null);

    private Namespace SpaceOf(RestrictedPrototype prototype) => prototype.Function ? _functions : _programs;

    /// <summary>Record the pointers, and present each prototype they name (and, for a decoded one, every prototype its
    /// signature's pointers name) to its namespace.</summary>
    private void Take(IReadOnlyList<DataItem> pointers)
    {
        var pending = new Queue<DataItem>(pointers);
        while (pending.Count > 0)
        {
            var pointer = pending.Dequeue();
            if (pointer.RestrictedPrototype is not { } prototype || !_stamped.Add(pointer)) continue;
            _pointers.Add(pointer);
            bool isNew = SpaceOf(prototype).Present(prototype);
            if (prototype.Origin is null || !isNew || prototype.Signature is not { } signature) continue;
            // A decoded signature reaches further prototypes through its own restricted-pointer formals.
            foreach (var root in signature.Formals.Select(f => f.Item).Concat(signature.Returning is { } r ? [r] : []))
            {
                if (root.RestrictedPrototype is not null) pending.Enqueue(root);
                foreach (var inner in DataItem.DescendantsOf(root))
                    if (inner.RestrictedPrototype is not null) pending.Enqueue(inner);
            }
        }
    }

    /// <summary>Refine to the coarsest stable partition from one starting block per namespace, re-stamping every pointer
    /// with its prototype's PROVISIONAL identity each round, then stamp the canonical identities.</summary>
    private void Run()
    {
        _programs.Restart();
        _functions.Restart();
        while (true)
        {
            foreach (var pointer in _pointers)
                if (pointer.RestrictedPrototype is { } prototype)
                    pointer.PointerRestrictionIdentity = SpaceOf(prototype).Provisional(prototype);
            // Both namespaces refine under ONE stamping (a program prototype's formal may be a function-pointer), so
            // neither short-circuits the other.
            bool programsSplit = _programs.Refine(), functionsSplit = _functions.Refine();
            if (!programsSplit && !functionsSplit) break;
        }
        // The partition is stable: each pointer takes its class's canonical identity. Every class's identity is computed
        // under the provisional stamps (CanonicalIdentity numbers the pointers of the graph it writes and restores them), so
        // no class reads another's canonical spelling half-way through.
        var canonical = _pointers.Select(p => p.RestrictedPrototype is { } prototype ? Identity(prototype) : null).ToList();
        for (int i = 0; i < _pointers.Count; i++)
            if (canonical[i] is { } identity) _pointers[i].PointerRestrictionIdentity = identity;
    }

    /// <summary>A prototype's key in a namespace: where it came from and its externalized name. A group prototype's origin
    /// is null (the group), so a group name is classified once; a decoded prototype's is its record's
    /// <see cref="PrototypeOrigin"/>, compared by reference, so two decoded <c>P</c>s of different records (or the decoded
    /// and the group <c>P</c>) are different members. The name is compared by the Annex C fold (§8.3.2.2).</summary>
    private readonly record struct PrototypeKey(PrototypeOrigin? Origin, string ExternalizedName)
    {
        public bool Equals(PrototypeKey other) =>
            ReferenceEquals(Origin, other.Origin) && CobolNames.Same(ExternalizedName, other.ExternalizedName);

        public override int GetHashCode() =>
            HashCode.Combine(Origin is null ? 0 : RuntimeHelpers.GetHashCode(Origin), CobolNames.Comparer.GetHashCode(ExternalizedName));
    }

    /// <summary>One namespace's classes (a program prototype and a function prototype of one name are two prototypes,
    /// §9.4): its blocks, each led by its founder, and every member already classified.</summary>
    private sealed class Namespace(bool function)
    {
        private List<Block> _blocks = [];
        // A prototype is classified once per key (an externalized name is ONE program in a run unit, §8.3.2.2 rule 2) —
        // and a self entry (BinderDriver.ProgramPrototypesOf) rebuilds its signature object on every call.
        private readonly Dictionary<PrototypeKey, Member> _members = [];
        // The members in the order the partition starts from: the group's, in the order they were presented, then the
        // decoded, in admission order (§6.4 part 2).
        private readonly List<Member> _group = [];
        private readonly List<Member> _decoded = [];

        /// <summary>Make a prototype with a compile-time signature a member (GR10 c) / GR11 c), no signature, is a class of
        /// its own and joins no block). False when it was already one.</summary>
        public bool Present(RestrictedPrototype prototype)
        {
            if (prototype.Signature is not { } signature) return false;
            var key = new PrototypeKey(prototype.Origin, prototype.ExternalizedName);
            if (_members.ContainsKey(key)) return false;
            var member = new Member(signature);
            _members[key] = member;
            (prototype.Origin is null ? _group : _decoded).Add(member);
            return true;
        }

        /// <summary>Start the refinement over: every member in ONE block, the group's first in their original order.</summary>
        public void Restart()
        {
            _blocks = [];
            if (_group.Count + _decoded.Count == 0) return;
            var all = new Block(function);
            foreach (var member in _group) all.Add(member);
            foreach (var member in _decoded) all.Add(member);
            _blocks.Add(all);
        }

        /// <summary>One round: splits every block by <see cref="PrototypeSignatures.Same"/> against the founder of each
        /// part. True when any block split.</summary>
        public bool Refine()
        {
            bool split = false;
            var next = new List<Block>(_blocks.Count);
            foreach (var block in _blocks)
            {
                int first = next.Count;
                foreach (var member in block.Members)
                {
                    Block? home = null;
                    for (int i = first; i < next.Count && home is null; i++)
                        if (PrototypeSignatures.Same(next[i].Founder.Signature, member.Signature)) home = next[i];
                    if (home is null) next.Add(home = new Block(function));
                    home.Add(member);
                }
                split |= next.Count - first > 1;
            }
            _blocks = next;
            return split;
        }

        /// <summary>A member's identity during the refinement: its block's position in the round, in this namespace —
        /// unique per block, compared only inside one round.</summary>
        public string Provisional(RestrictedPrototype prototype) =>
            Known(prototype) is { } block
                ? string.Create(System.Globalization.CultureInfo.InvariantCulture, $"{(function ? "F" : "P")}#{_blocks.IndexOf(block)}")
                : PrototypeSignatures.ExternalIdentity(prototype.ExternalizedName);

        /// <summary>The block of a prototype this namespace has classified, or null.</summary>
        public Block? Known(RestrictedPrototype prototype) =>
            prototype.Signature is not null && _members.TryGetValue(new PrototypeKey(prototype.Origin, prototype.ExternalizedName), out var m)
                ? m.Block : null;

        /// <summary>The block of a prototype, classifying a GROUP prototype no pointer named on the way: it joins the block
        /// whose founder it has the same signature as, or founds one. Null for a prototype with no compile-time signature.</summary>
        public Block? BlockOf(RestrictedPrototype prototype)
        {
            if (prototype.Signature is not { } signature) return null;
            if (Known(prototype) is { } known) return known;
            if (prototype.Origin is not null)
                throw new InvalidOperationException($"PrototypeSignatureClasses: the decoded prototype '{prototype.ExternalizedName}' of {prototype.Origin.Description} was asked for before Admit classified it.");
            Present(prototype);
            var member = _members[new PrototypeKey(null, prototype.ExternalizedName)];
            var home = _blocks.Find(b => PrototypeSignatures.Same(b.Founder.Signature, signature));
            if (home is null) _blocks.Add(home = new Block(function));
            home.Add(member);
            return home;
        }
    }

    /// <summary>A prototype in the partition: its signature, and the block it is in now.</summary>
    private sealed class Member(CalleeSignature signature)
    {
        public CalleeSignature Signature { get; } = signature;
        public Block? Block { get; set; }
    }

    /// <summary>One block of the partition — one signature class of its namespace; its first member is its founder, whose
    /// signature describes the class, and its canonical identity is computed once the partition is stable.</summary>
    private sealed class Block(bool function)
    {
        public bool Function { get; } = function;
        public List<Member> Members { get; } = [];
        public Member Founder => Members[0];
        public string? Canonical { get; set; }

        public void Add(Member member)
        {
            Members.Add(member);
            member.Block = this;
        }
    }
}

/// <summary>Where a decoded prototype comes from (DESIGN-external-repository §6.4 part 1): the repository record that
/// describes it, so two records' prototypes of one name are two members of the partition and a decoded prototype is never
/// the group's. Compared by reference — the decoder makes one per record it decodes.</summary>
internal sealed class PrototypeOrigin(string description)
{
    /// <summary>The record, for a diagnostic: the entry's externalized name and the prototype's index in its
    /// <c>ReferencedPrototypes</c>.</summary>
    public string Description { get; } = description;
}

/// <summary>The prototype a restricted program- or function-pointer's <c>TO prototype-name-1</c> phrase resolves to
/// through its unit's prototype tables (<c>BinderDriver.ProgramPrototypesOf</c> / <c>UserFunctionsOf</c>, ISO §8.4.6.8 /
/// §8.4.6.6 and §12.3.8.4 GR10 / GR11): its namespace, its externalized name, and its signature — null when GR10 c) /
/// GR11 c) take the details from the external repository, so there is nothing to compare at compile time. Attached to
/// the pointer as <see cref="DataItem.RestrictedPrototype"/> (kb/Work PB2495) so that the signature classes can be
/// computed over every prototype a pointer names (<see cref="PrototypeSignatureClasses.Classify"/>).
/// <paramref name="Origin"/> is null for a prototype the compilation group declares, and the record it was decoded from
/// otherwise (<see cref="PrototypeSignatureClasses.Admit"/>).</summary>
internal sealed record RestrictedPrototype(bool Function, string ExternalizedName, CalleeSignature? Signature, PrototypeOrigin? Origin = null)
{
    public static RestrictedPrototype Of(ProgramPrototype program) =>
        new(Function: false, program.ExternalizedName, program.Signature);

    public static RestrictedPrototype Of(UserFunctionSignature function) =>
        new(Function: true, function.Externalized, new CalleeSignature(function.Formals, function.Returning));
}
