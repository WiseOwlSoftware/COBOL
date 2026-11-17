// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Numerics;
using WiseOwl.Cobol.Binding.Bound;

namespace WiseOwl.Cobol.Binding;

/// <summary>An exact rational in lowest terms with a positive denominator: the carrier of the type 6 classifier's literal fold
/// (<c>IntrinsicArgumentRules.ExactValue</c>) and of its polynomial coefficients (<see cref="Polynomial"/>). A decimal literal is
/// num/10^k; a quotient of literals may leave any denominator, which is why this is not a scaled decimal.</summary>
internal readonly record struct ExactRational
{
    public BigInteger Numerator { get; }
    public BigInteger Denominator { get; }

    private ExactRational(BigInteger num, BigInteger den)
    {
        if (den.Sign < 0) { num = -num; den = -den; }
        var g = BigInteger.GreatestCommonDivisor(num, den);
        if (!g.IsZero && !g.IsOne) { num /= g; den /= g; }
        Numerator = num;
        Denominator = num.IsZero ? BigInteger.One : den;
    }

    public static ExactRational Zero => new(0, 1);
    public static ExactRational One => new(1, 1);

    /// <summary>The integer <paramref name="value"/>.</summary>
    public static ExactRational Of(BigInteger value) => new(value, 1);

    /// <summary>significand × 10^exp10, the decomposition of a literal.</summary>
    public static ExactRational FromScaled(BigInteger significand, int exp10) => exp10 >= 0
        ? new(significand * BigInteger.Pow(10, exp10), 1)
        : new(significand, BigInteger.Pow(10, -exp10));

    public bool IsInteger => Denominator.IsOne;

    /// <summary>The fraction digits of the value as a finite decimal (a denominator of 2s and 5s only: the larger of the two
    /// exponents), or null when the expansion does not terminate.</summary>
    public int? FractionDigits
    {
        get
        {
            var den = Denominator;
            int twos = 0, fives = 0;
            while (den.IsEven) { den /= 2; twos++; }
            while (den % 5 == 0) { den /= 5; fives++; }
            return den.IsOne ? Math.Max(twos, fives) : null;
        }
    }

    public int Sign => Numerator.Sign;

    /// <summary>The least h with |value| ≤ 10^h (<paramref name="strict"/>: |value| &lt; 10^h); the value is nonzero.</summary>
    public int DecimalExponent(bool strict)
    {
        var num = BigInteger.Abs(Numerator);
        int h = (int)Math.Floor(BigInteger.Log10(num) - BigInteger.Log10(Denominator)) - 1;
        while (!WithinPowerOfTen(num, h, strict)) h++;
        return h;
    }

    private bool WithinPowerOfTen(BigInteger num, int h, bool strict)
    {
        BigInteger lhs = h >= 0 ? num : num * BigInteger.Pow(10, -h);
        BigInteger rhs = h >= 0 ? BigInteger.Pow(10, h) * Denominator : Denominator;
        return strict ? lhs < rhs : lhs <= rhs;
    }

    public static ExactRational operator -(ExactRational a) => new(-a.Numerator, a.Denominator);
    public static ExactRational operator +(ExactRational a, ExactRational b) =>
        new(a.Numerator * b.Denominator + b.Numerator * a.Denominator, a.Denominator * b.Denominator);
    public static ExactRational operator -(ExactRational a, ExactRational b) => a + -b;
    public static ExactRational operator *(ExactRational a, ExactRational b) =>
        new(a.Numerator * b.Numerator, a.Denominator * b.Denominator);
    /// <summary>Division; the caller guarantees a nonzero divisor.</summary>
    public static ExactRational operator /(ExactRational a, ExactRational b) =>
        new(a.Numerator * b.Denominator, a.Denominator * b.Numerator);

    /// <summary>this^n; the caller guarantees a nonzero base when n &lt; 0.</summary>
    public ExactRational Pow(int n) => n >= 0
        ? new(BigInteger.Pow(Numerator, n), BigInteger.Pow(Denominator, n))
        : new(BigInteger.Pow(Denominator, -n), BigInteger.Pow(Numerator, -n));

    /// <summary>The value as a diagnostic prints it: a decimal numeral when the denominator is a product of 2s and 5s (every sum
    /// of decimal literals), otherwise the fraction.</summary>
    public override string ToString()
    {
        if (FractionDigits is not { } k) return $"{Numerator}/{Denominator}";
        var scaled = BigInteger.Abs(Numerator * BigInteger.Pow(10, k) / Denominator);
        string digits = scaled.ToString(System.Globalization.CultureInfo.InvariantCulture).PadLeft(k + 1, '0');
        string text = k == 0 ? digits : $"{digits[..^k]}.{digits[^k..]}";
        return Numerator.Sign < 0 ? "-" + text : text;
    }
}

/// <summary>An ATOM of an argument polynomial (kb/Work PB5891, docs/INTRINSICS_DESIGN.md, the type-6 intake): a leaf term of the
/// expression that is not a constant. An <paramref name="Integer"/> atom takes integer values only (a fixed-point item's integer
/// count of its least unit, an integer function, an index-name, a counter register); a REAL atom any value (a floating-point item, a
/// numeric function). <paramref name="Digits"/> bounds an integer atom's magnitude, |x| &lt; 10^Digits, or is null when it is
/// unknown; <paramref name="Scale"/> is the item's scale, so the item's value is x × 10^(−Scale) (0 for any other atom).</summary>
internal readonly record struct PolynomialAtom(BoundExpr Term, bool Integer, int? Digits, int Scale);

/// <summary>⛔ THE VALUE OF A TYPE 6 ARGUMENT AS A POLYNOMIAL WITH RATIONAL COEFFICIENTS IN ITS ATOMS (kb/Work PB5891). Monomials are
/// kept in ordinal order of their key, so every walk over them, and every diagnostic it names, is deterministic. Immutable: every
/// operation returns a new polynomial.</summary>
internal sealed class Polynomial
{
    /// <summary>The most monomials an expansion may hold; a product or power past it is not expanded (the classifier then reads the
    /// node's structure, <c>IntrinsicArgumentRules.ProvablyIntegral</c>).</summary>
    public const int MaxMonomials = 1024;

    /// <summary>⛔ THE CAP OF THE GRID TEST (docs/INTRINSICS_DESIGN.md, the type-6 intake, rule 3): the grid is evaluated only for a
    /// degree of at most this in every atom, and for at most <see cref="MaxGridPoints"/> points. Past either,
    /// <see cref="FractionDigits"/> is the coefficients' bound.</summary>
    public const int MaxGridDegree = 12;

    /// <summary>The most points the grid test evaluates (see <see cref="MaxGridDegree"/>).</summary>
    public const int MaxGridPoints = 4096;

    /// <summary>One monomial: its factors (atom index, power ≥ 1) in ascending atom order, and its coefficient.</summary>
    public readonly record struct Term((int Atom, int Power)[] Factors, ExactRational Coefficient);

    private readonly SortedDictionary<string, Term> _terms;

    private Polynomial(SortedDictionary<string, Term> terms) => _terms = terms;

    /// <summary>The monomials whose coefficient is not zero, in ordinal order of their key.</summary>
    public IEnumerable<Term> Terms => _terms.Values;

    public bool IsZero => _terms.Count == 0;

    public static Polynomial Zero => new(new(StringComparer.Ordinal));

    public static Polynomial Constant(ExactRational c) => Single([], c);

    /// <summary>The atom <paramref name="atom"/> under the coefficient <paramref name="coefficient"/>.</summary>
    public static Polynomial OfAtom(int atom, ExactRational coefficient) => Single([(atom, 1)], coefficient);

    private static Polynomial Single((int Atom, int Power)[] factors, ExactRational c)
    {
        var p = Zero;
        if (c.Sign != 0) p._terms.Add(KeyOf(factors), new Term(factors, c));
        return p;
    }

    private static string KeyOf((int Atom, int Power)[] factors) =>
        string.Join(",", factors.Select(f => $"{f.Atom:D4}^{f.Power}"));

    private static void Accumulate(SortedDictionary<string, Term> terms, (int Atom, int Power)[] factors, ExactRational c)
    {
        string key = KeyOf(factors);
        var sum = terms.TryGetValue(key, out var prior) ? prior.Coefficient + c : c;
        if (sum.Sign == 0) terms.Remove(key);
        else terms[key] = new Term(factors, sum);
    }

    /// <summary>this + k × other.</summary>
    public Polynomial Add(Polynomial other, ExactRational k)
    {
        var terms = new SortedDictionary<string, Term>(_terms, StringComparer.Ordinal);
        foreach (var t in other._terms.Values) Accumulate(terms, t.Factors, k * t.Coefficient);
        return new(terms);
    }

    /// <summary>k × this.</summary>
    public Polynomial Scale(ExactRational k) => Zero.Add(this, k);

    /// <summary>this × other, or null when the product has more than <see cref="MaxMonomials"/> monomials.</summary>
    public Polynomial? Multiply(Polynomial other)
    {
        var terms = new SortedDictionary<string, Term>(StringComparer.Ordinal);
        foreach (var a in _terms.Values)
            foreach (var b in other._terms.Values)
            {
                Accumulate(terms, MultiplyFactors(a.Factors, b.Factors), a.Coefficient * b.Coefficient);
                if (terms.Count > MaxMonomials) return null;
            }
        return new(terms);
    }

    /// <summary>this^n (n ≥ 0), or null when a partial product has more than <see cref="MaxMonomials"/> monomials.</summary>
    public Polynomial? Power(int n)
    {
        Polynomial? result = Constant(ExactRational.One);
        for (int i = 0; i < n && result is not null; i++) result = result.Multiply(this);
        return result;
    }

    private static (int Atom, int Power)[] MultiplyFactors((int Atom, int Power)[] a, (int Atom, int Power)[] b)
    {
        var powers = new SortedDictionary<int, int>();
        foreach (var (atom, power) in a.Concat(b)) powers[atom] = powers.GetValueOrDefault(atom) + power;
        return [.. powers.Select(kv => (kv.Key, kv.Value))];
    }

    /// <summary>An atom <paramref name="isReal"/> answers true for, from the first monomial (in key order) that holds one, or null
    /// when every monomial is in integer atoms alone.</summary>
    public int? FirstRealAtom(Func<int, bool> isReal)
    {
        foreach (var t in _terms.Values)
            foreach (var (atom, _) in t.Factors)
                if (isReal(atom)) return atom;
        return null;
    }

    /// <summary>⛔ THE ONE INTEGER-VALUED TEST (kb/Work PB5891): the least f for which 10^f × this is an integer for every integer
    /// value of its atoms, or null when there is none or the cap stops the test. The polynomial is in integer atoms alone (the caller
    /// has refused a real one). Sound and exact: a polynomial of degree at most d_i in atom i is integer-valued on Z^n exactly when
    /// it is an integer on the grid {0..d1} × … × {0..dn}, because its coefficients in the binomial basis C(x1, k1) … C(xn, kn) are
    /// integer combinations (iterated forward differences) of its grid values. So f is the most fraction digits the polynomial
    /// takes on that grid. When every coefficient is a multiple of 10^(−f) for some f, that f bounds the answer and the grid only
    /// tightens it; past the cap (<see cref="MaxGridDegree"/>, <see cref="MaxGridPoints"/>) the coefficients' bound is the answer.
    /// <paramref name="witness"/> is a grid point (atom index → value) where the value has the most fraction digits, or a
    /// non-terminating value, when the grid was evaluated and f is not 0.</summary>
    public int? FractionDigits(out IReadOnlyDictionary<int, BigInteger>? witness)
    {
        witness = null;
        int? byCoefficients = 0;
        foreach (var t in _terms.Values)
            byCoefficients = t.Coefficient.FractionDigits is { } k && byCoefficients is { } f ? Math.Max(f, k) : null;
        if (byCoefficients == 0) return 0;
        var degrees = new SortedDictionary<int, int>();
        foreach (var t in _terms.Values)
            foreach (var (atom, power) in t.Factors) degrees[atom] = Math.Max(degrees.GetValueOrDefault(atom), power);
        long points = 1;
        foreach (int d in degrees.Values)
        {
            if (d > MaxGridDegree) return byCoefficients;
            points *= d + 1;
            if (points > MaxGridPoints) return byCoefficients;
        }
        int[] atoms = [.. degrees.Keys];
        var point = new BigInteger[atoms.Length];
        int most = 0;
        while (true)
        {
            var at = new Dictionary<int, BigInteger>();
            for (int i = 0; i < atoms.Length; i++) at[atoms[i]] = point[i];
            int? digits = Evaluate(at).FractionDigits;
            if (digits is null) { witness = at; return null; }
            if (digits > most) { most = digits.Value; witness = at; }
            int j = 0;
            while (j < atoms.Length && point[j] == degrees[atoms[j]]) point[j++] = 0;
            if (j == atoms.Length) break;
            point[j] += 1;
        }
        return most;
    }

    /// <summary>The exact value at <paramref name="point"/> (atom index → integer value; every atom of the polynomial is in it).</summary>
    public ExactRational Evaluate(IReadOnlyDictionary<int, BigInteger> point)
    {
        var sum = ExactRational.Zero;
        foreach (var t in _terms.Values)
        {
            var product = t.Coefficient;
            foreach (var (atom, power) in t.Factors) product *= ExactRational.Of(BigInteger.Pow(point[atom], power));
            sum += product;
        }
        return sum;
    }

    /// <summary>The least h for which the value is below 10^h in magnitude for every value of the atoms, from the monomials'
    /// coefficients and the atoms' digit bounds (<paramref name="digits"/>: |x| &lt; 10^digits, null when unknown), plus the carry of
    /// adding the monomials (n terms each below 10^m sum below 10^(m + e) for the least e with 10^e ≥ n); null when an atom's bound
    /// is unknown. The zero polynomial is 0.</summary>
    public int? DigitHigh(Func<int, int?> digits)
    {
        if (IsZero) return 0;
        int high = int.MinValue;
        foreach (var t in _terms.Values)
        {
            // A monomial with an atom is below |c| × 10^(Σ power × digits) strictly, so |c| ≤ 10^h serves; a constant needs |c| < 10^h.
            int h = t.Coefficient.DecimalExponent(strict: t.Factors.Length == 0);
            foreach (var (atom, power) in t.Factors)
            {
                if (digits(atom) is not { } d) return null;
                h += power * d;
            }
            high = Math.Max(high, h);
        }
        int carry = 0;
        while (BigInteger.Pow(10, carry) < _terms.Count) carry++;
        return high + carry;
    }
}
