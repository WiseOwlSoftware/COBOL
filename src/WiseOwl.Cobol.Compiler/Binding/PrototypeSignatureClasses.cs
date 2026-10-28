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
/// round splits nothing, so the classes do not depend on the order prototypes are presented in), and a class's identity
/// is its founder's — the first member presented — externalized name
/// (§8.3.2.2: "all instances of a given name that is externalized to the operating environment shall identify the same
/// kind of entity"), spelled <c>TO name</c> as a pointer's USAGE clause spells it. A prototype that took ISO §12.3.8.4
/// GR10 c) / GR11 c) — the details come from the external repository, so there is no compile-time signature to compare —
/// is a class of its own, marked as such: <see cref="PrototypeSignatures.ExternalIdentity"/> of its own name, which the
/// conformance relations let conform to any restricted pointer of its kind, as <see cref="PrototypeSignatures.Same"/>
/// lets an unresolved signature conform (kb/Work PB3162). A prototype that is its own founder keeps the identity it
/// always had; only an ALIAS of a founder changes.</para>
/// <para><b>Two kinds of member (DESIGN-external-repository §6.4 parts 1-3; kb/Work PB2100).</b> A GROUP prototype is one
/// the compilation group declares; a DECODED prototype is one a repository record describes
/// (<see cref="RestrictedPrototype.Origin"/>, <see cref="Admit"/>). Members are keyed by ORIGIN AND NAME
/// (<see cref="PrototypeKey"/>): a group name is classified once (§8.3.2.2: one externalized name, one program), and a
/// decoded <c>P</c> is a member of its own, placed by <see cref="PrototypeSignatures.Same"/> over its DECODED signature,
/// never by the group's <c>P</c> — the key is right inside one group and wrong the moment a second source of prototypes
/// exists, because keying by name types a decoded pointer by NAME, which GR25 forbids. <see cref="Admit"/> adds the
/// decoded prototypes and RE-RUNS the refinement over every member from ONE starting block, group members first in their
/// original order, so a decoded <c>B</c> whose signature is <see cref="PrototypeSignatures.Same"/> as the group's <c>B</c>
/// lands in that block rather than founding a second one (two types for one signature), and each block a group member
/// founds keeps its founder and so its spelling: no group pointer's identity moves. A block founded by a decoded member is
/// spelled <c>DECODED k TO name</c>, <c>k</c> its founder's ADMISSION number (the order <see cref="Admit"/> added decoded
/// members, from 1, fixed before the re-run and independent of the round a block splits off in). Every group spelling
/// begins <c>TO </c> and every decoded one <c>DECODED </c>, and neither is the runtime's reserved <c>EXTERNAL TO </c>
/// (<see cref="PrototypeSignatures.ExternalIdentity"/>), whatever an <c>AS</c> literal holds, so two blocks never share an
/// identity string and <see cref="PrototypeSignatures.SameType"/> (string equality of identities) stays "same block".</para>
/// <para><b>Scope.</b> One instance per compilation group (<see cref="BindSession"/>). A group founder's identity is
/// comparable between two descriptions of one run unit only while both were compiled in one group; the canonical,
/// group-independent signature identity replaces the spelling in slice 7 (kb/Work PB2103), and this class stays the ONE
/// place a signature class is formed.</para>
/// </summary>
internal sealed class PrototypeSignatureClasses
{
    private readonly Namespace _programs = new();
    private readonly Namespace _functions = new();

    /// <summary>Every pointer this instance stamps, group pointers first (the order they were presented in).</summary>
    private readonly List<DataItem> _pointers = [];
    private readonly HashSet<DataItem> _stamped = new(ReferenceEqualityComparer.Instance);
    private int _admitted;

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

    /// <summary>The identity of a prototype's signature class (<c>TO name</c>, its founder's externalized name; <c>DECODED k TO
    /// name</c> for a block a decoded member founded). A prototype <see cref="Classify"/> or <see cref="Admit"/>
    /// partitioned answers its block; a GROUP prototype no restricted pointer names (a program-address-identifier's
    /// prototype, asked by the procedure binder) joins the class whose founder it has the same signature as, under the
    /// identities the partition stamped, or founds one of its own — no stamped pointer is restricted to it, so the
    /// partition stays stable. A decoded prototype not yet admitted is a defect of the caller.</summary>
    public string Identity(RestrictedPrototype prototype) =>
        SpaceOf(prototype).Identity(prototype);

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
            bool isNew = SpaceOf(prototype).Present(prototype, ref _admitted);
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
    /// each round.</summary>
    private void Run()
    {
        _programs.Restart();
        _functions.Restart();
        while (true)
        {
            foreach (var pointer in _pointers)
                if (pointer.RestrictedPrototype is { } prototype)
                    pointer.PointerRestrictionIdentity = Identity(prototype);
            // Both namespaces refine under ONE stamping (a program prototype's formal may be a function-pointer), so
            // neither short-circuits the other.
            bool programsSplit = _programs.Refine(), functionsSplit = _functions.Refine();
            if (!programsSplit && !functionsSplit) return;
        }
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
    private sealed class Namespace
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
        /// its own and joins no block). A decoded member takes the next admission number. False when it was already one.</summary>
        public bool Present(RestrictedPrototype prototype, ref int admitted)
        {
            if (prototype.Signature is not { } signature) return false;
            var key = new PrototypeKey(prototype.Origin, prototype.ExternalizedName);
            if (_members.ContainsKey(key)) return false;
            var member = prototype.Origin is null
                ? new Member(PrototypeSignatures.OwnIdentity(prototype.ExternalizedName), signature)
                : new Member("DECODED " + ++admitted + " " + PrototypeSignatures.OwnIdentity(prototype.ExternalizedName), signature);
            _members[key] = member;
            (prototype.Origin is null ? _group : _decoded).Add(member);
            return true;
        }

        /// <summary>Start the refinement over: every member in ONE block, the group's first in their original order.</summary>
        public void Restart()
        {
            _blocks = [];
            if (_group.Count + _decoded.Count == 0) return;
            var all = new Block();
            foreach (var member in _group) all.Add(member);
            foreach (var member in _decoded) all.Add(member);
            _blocks.Add(all);
        }

        /// <summary>One round: splits every block by <see cref="PrototypeSignatures.Same"/> against the founder of each
        /// part (the founder stays first, so a block's identity survives the split). True when any block split.</summary>
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
                    if (home is null) next.Add(home = new Block());
                    home.Add(member);
                }
                split |= next.Count - first > 1;
            }
            _blocks = next;
            return split;
        }

        public string Identity(RestrictedPrototype prototype)
        {
            if (prototype.Signature is not { } signature)
                return PrototypeSignatures.ExternalIdentity(prototype.ExternalizedName);   // GR10 c) / GR11 c)
            if (_members.TryGetValue(new PrototypeKey(prototype.Origin, prototype.ExternalizedName), out var known))
                return known.Block!.Founder.Identity;
            if (prototype.Origin is not null)
                throw new InvalidOperationException($"PrototypeSignatureClasses: the decoded prototype '{prototype.ExternalizedName}' of {prototype.Origin.Description} was asked for before Admit classified it.");
            // A group prototype no pointer named: join the block whose founder it has the same signature as, or found one.
            int ignored = 0;
            Present(prototype, ref ignored);
            var member = _members[new PrototypeKey(null, prototype.ExternalizedName)];
            var home = _blocks.Find(b => PrototypeSignatures.Same(b.Founder.Signature, signature));
            if (home is null) _blocks.Add(home = new Block());
            home.Add(member);
            return home.Founder.Identity;
        }
    }

    /// <summary>A prototype in the partition: its class identity were it a founder (<c>TO</c> its externalized name, or
    /// <c>DECODED k TO</c> it), its signature, and the block it is in now.</summary>
    private sealed class Member(string identity, CalleeSignature signature)
    {
        public string Identity { get; } = identity;
        public CalleeSignature Signature { get; } = signature;
        public Block? Block { get; set; }
    }

    /// <summary>One block of the partition; its first member is its founder, whose identity is the block's.</summary>
    private sealed class Block
    {
        public List<Member> Members { get; } = [];
        public Member Founder => Members[0];

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
