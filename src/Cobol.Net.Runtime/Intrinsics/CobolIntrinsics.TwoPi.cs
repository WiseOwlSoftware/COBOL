// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>
/// The exact reduction of an exact argument to a QUADRANT and a residue about the nearest multiple of π/2, for SIN /
/// COS / TAN (kb/Work PB999, PB1041; the periodic arm of the exact-intake bodies in <c>CobolIntrinsics.Float.cs</c>).
/// </summary>
/// <remarks>
/// <para>ISO §15.82.4 r1 / §15.20.4 r1 / §15.89.4 r1 return "the approximation of the sine [cosine / tangent] of
/// argument-1", and argument-1 is an exact decimal: a scaled item, or an SDIDI whose range reaches 9.999…E+6144
/// (§8.8.1.5.2). Narrowing it to binary64 FIRST answers for a different argument in two ways. (1) Past binary64's
/// range it answers for +∞, and even inside it the narrowing is an ABSOLUTE error of about |x|·2^−53, a whole period
/// once |x| passes ~10^16. (2) Near ANY multiple of π/2 the function is ill-conditioned: sin, cos and tan there
/// equal (plus or minus) the residue x − k·π/2 or its reciprocal, so an argument error of 10^−17 is the whole answer
/// — SIN(3.14159265358979323) is 8.46·10^−18, and sin of its binary64 is 1.22·10^−16. So the residue is formed on
/// the decimal itself, and only it is narrowed; the body then evaluates sin / cos / ±tan / −cot of a residue that
/// carries binary64's RELATIVE precision, which is the §15.4.1 approximation.</para>
/// <para>⛔ ON AN Int128 FIXED POINT, NOT A WIDER TYPE (COBOLNET_DESIGN §1.2 invariant 2 — no arbitrary-precision
/// integer in the runtime value model). With c = 1/(2π) = 0.c₁c₂c₃… and |x| = Sig·10^Exp, the turn count |x|·c
/// is congruent modulo 1 to Sig·g, where g = frac(10^Exp·c) is just c's digit string read from place Exp+1 (zeros
/// first when Exp &lt; 0): the digits before it contribute Sig times an INTEGER. g is read to
/// <see cref="ReductionPlaces"/> = 162 places as <see cref="ReductionLimbs"/> base-10^18 limbs and multiplied by
/// Sig's three limbs schoolbook — every partial product is below 10^36 and every column sum below 10^37, inside
/// Int128 — keeping only the fractional limbs. Truncating g costs under Sig·10^−162 &lt; 2·10^−124 turns, which is
/// the reduction's whole error. Four times that fraction is the quadrant (its integer part) and the residue (its
/// fraction, centred on (−½, ½]); the residue's two leading nonzero limbs become a double-double (FMA products)
/// scaled by π/2, so the body sees it to about 10^−18 relative, below binary64's 2^−53. The smallest residue a 34-digit decimal in the SDIDI range reaches is, by the
/// usual counting heuristic (~10^38 candidate significand-exponent pairs), about 10^−40 radians — some 80 digits
/// above the reduction's error, so the residue's relative precision is never the reduction's to lose. The only wide
/// datum is the digit string itself, a CONSTANT, and <c>InverseTwoPiDigitsDriftTests</c> (Unit) re-derives it
/// independently.</para>
/// </remarks>
public static partial class CobolIntrinsics
{
    /// <summary>Decimal digits per limb of the reduction's fixed point: 10^18 &lt; 2^63, and a limb product is below
    /// 10^36, so a column of three sums below 10^37 in <see cref="Int128"/> (max ≈ 1.7·10^38).</summary>
    internal const int LimbDigits = 18;

    /// <summary>Limbs of g = frac(10^Exp / 2π) the reduction reads.</summary>
    internal const int ReductionLimbs = 9;

    /// <summary>Places of 1/(2π) one reduction reads past the argument's lowest place: <see cref="ReductionLimbs"/> ·
    /// <see cref="LimbDigits"/> = 162.</summary>
    internal const int ReductionPlaces = ReductionLimbs * LimbDigits;

    private const long LimbBase = 1_000_000_000_000_000_000;

    /// <summary>10^18 as a double — EXACT (5^18 &lt; 2^53), so dividing by it is one correctly-rounded step.</summary>
    private const double LimbBaseDouble = 1e18;

    /// <summary>π/2 as a double-double: the binary64 nearest π/2 plus the binary64 nearest the remainder.</summary>
    private const double HalfPiHi = 1.5707963267948966, HalfPiLo = 6.123233995736766e-17;

    /// <summary>|x| reduced to <c>q·π/2 + r</c>, q ∈ {0, 1, 2, 3} and r ∈ [−π/4, π/4] as the double-double
    /// <c>Hi + Lo</c>, computed on the EXACT carrier (see the type's remarks). The caller applies x's sign by the
    /// function's parity.</summary>
    private static (int Quadrant, double Hi, double Lo) ReduceQuarterTurns(CobolDec x)
    {
        Int128 sig = Int128.Abs(x.Sig);
        Span<long> s = [(long)(sig % LimbBase), (long)(sig / LimbBase % LimbBase), (long)(sig / LimbBase / LimbBase)];
        Span<long> g = stackalloc long[ReductionLimbs];
        for (int k = 0; k < ReductionLimbs; k++) g[k] = InverseTwoPiLimb(x.Exp + k * LimbDigits);

        // a[m] is the coefficient of 10^(−18m); s[j]·10^(18j) × g[k]·10^(−18(k+1)) lands at m = k + 1 − j, and a
        // term with k < j is an INTEGER number of turns, which the reduction discards.
        Span<Int128> a = stackalloc Int128[ReductionLimbs + 1];
        for (int j = 0; j < s.Length; j++)
            for (int k = j; k < ReductionLimbs; k++)
                a[k + 1 - j] += (Int128)s[j] * g[k];
        for (int m = ReductionLimbs; m > 0; m--) { a[m - 1] += a[m] / LimbBase; a[m] %= LimbBase; }

        // Quarter turns: 4 × the turn fraction; what carries out of a[1] is the quadrant.
        Int128 carry = 0;
        for (int m = ReductionLimbs; m > 0; m--)
        {
            Int128 v = a[m] * 4 + carry;
            a[m] = v % LimbBase;
            carry = v / LimbBase;
        }
        int quadrant = (int)carry;

        // Centre the residue on (−½, ½] quarter-turns: past a half it is the NEXT quadrant's negative residue, 1 − ρ.
        bool negative = a[1] >= LimbBase / 2;
        if (negative)
        {
            quadrant = (quadrant + 1) & 3;
            Int128 borrow = 0;
            for (int m = ReductionLimbs; m > 0; m--)
            {
                Int128 v = -a[m] - borrow;
                (a[m], borrow) = v < 0 ? (v + LimbBase, 1) : (v, 0);
            }
        }

        int lead = 1;
        while (lead <= ReductionLimbs && a[lead] == 0) lead++;
        if (lead > ReductionLimbs) return (quadrant, 0, 0);

        // The residue's two leading limbs as one Int128 (below 10^36, so at least 10^18 when the first is nonzero:
        // relative precision 10^−18 from the limbs dropped), exactly as a double-double, times π/2, then scaled by
        // 10^(−18(lead + 1)) one exact 10^18 at a time.
        Int128 top = a[lead] * LimbBase + (lead < ReductionLimbs ? a[lead + 1] : 0);
        double hi = (double)top;
        double lo = (double)(top - (Int128)hi);
        (hi, lo) = MulDd(hi, lo, HalfPiHi, HalfPiLo);
        for (int m = 0; m <= lead; m++) (hi, lo) = DivDd(hi, lo, LimbBaseDouble);
        return negative ? (quadrant, -hi, -lo) : (quadrant, hi, lo);
    }

    /// <summary>The double-double product (Dekker / FMA two-product).</summary>
    private static (double Hi, double Lo) MulDd(double ah, double al, double bh, double bl)
    {
        double p = ah * bh;
        double e = Math.FusedMultiplyAdd(ah, bh, -p) + (ah * bl + al * bh);
        double h = p + e;
        return (h, e - (h - p));
    }

    /// <summary>A double-double divided by a double (one FMA-corrected long-division step).</summary>
    private static (double Hi, double Lo) DivDd(double ah, double al, double d)
    {
        double q1 = ah / d;
        double q2 = (Math.FusedMultiplyAdd(-q1, d, ah) + al) / d;
        double h = q1 + q2;
        return (h, q2 - (h - q1));
    }

    /// <summary>The <see cref="LimbDigits"/> digits of 1/(2π) at places <paramref name="n"/>+1 … n+18 as one limb,
    /// the places before the first (n &lt; 0) read as zeros.</summary>
    private static long InverseTwoPiLimb(int n)
    {
        long v = 0;
        for (int p = n + 1; p <= n + LimbDigits; p++)
        {
            if (p > InverseTwoPiDigits.Length)
                throw new InvalidOperationException(
                    $"quarter-turn reduction at 10^{n}: past the SDIDI range 9.999…E+6144 (ISO §8.8.1.5.2) that InverseTwoPiDigits covers");
            v = v * 10 + (p >= 1 ? InverseTwoPiDigits[p - 1] - '0' : 0);
        }
        return v;
    }

    /// <summary>The first 6320 fractional digits of 1/(2π) = 0.15915494309189533576…: enough for
    /// <see cref="ReductionPlaces"/> places past every place an SDIDI digit can occupy (the largest is 10^6144,
    /// §8.8.1.5.2). Re-derived independently by <c>InverseTwoPiDigitsDriftTests</c> (Unit).</summary>
    internal const string InverseTwoPiDigits =
        "1591549430918953357688837633725143620344596457404564487476673440588967976342265350901138027662530859" +
        "5607284272675795803689291184611457865287796741073169983922923996693740907757307774639692530768871739" +
        "2896217397661693362390241723629011832380114222699755715940461890086902673956120489410936937844085528" +
        "7230999464434002486723477394596108983230967830749061669864628046994486521878815747865669642410389958" +
        "7413934860998386809919996244287558517117885843111751876716054654753698800973946036475933376805930249" +
        "4496635305327156775503220324777816397166022946748119598165840606016803035998133911987498832786654435" +
        "2797550700162406775643888495713108801221993761476813777647378906330680464579784817613124273140699607" +
        "7502450029775985708905690279678513152521001631774602092481160624056145620314648408924845919143521157" +
        "5407556200871526606802217159140757474582722597746285399875155329390813981772409358254797073328719040" +
        "6999759076577078493470393589828087173425640366895116625457059433276312686500261227179711532112599504" +
        "3866794503762556083631711695259758128224941623334314510612353687856311363669216714206974696012925057" +
        "8336053119608594509839556718709954746510431623815517580839442979970999505254387566129445883306846050" +
        "7852915151410404892988506388160776196993073410389995786918905980937377720618754322271893013662552612" +
        "3878038753888110681406765434082827852693342679955607079038606035273899624512599574927629702359409558" +
        "4301164829641185577712405754449457021789769792409490327294770216649603565318153544003840689874717691" +
        "5887631909665069644047769706876836567781047797954503533957583018818386879377661248149530599655802190" +
        "8359875103512712904323158049871968687775946566346221034204440855497850379273869429353661937782928735" +
        "9378434703230237145837923557118636341929460183182291964165008783079331353497790997458649290267450609" +
        "8936890945883050337030538054731232158094319767603228313141898097498224383351743569898475010395006838" +
        "8003978672359960802400273901087495485478792356826113994890326899742708349611492082890377678474303550" +
        "4568456083671479308456723327035485392556202086839324099562211753318394020970793570774965498808686066" +
        "3609686619670374745421028312192518462248349911611495665560379696761399312829960776082779901007830360" +
        "0233827298790854023876155744543092601191005433799838904654921248295160707285300522721023601752331317" +
        "3179759311050328155109373913639645305792607180083617954876724645980473977292448109200937125786918332" +
        "8958862839904358686666397567344514095036373271917431138806638307259230275973450605482127780370653377" +
        "8303217098773496656849080032698850674179146468350828161685331433616073099514985311981973375844420984" +
        "1655954152250643394312864440383883561508797716450170647067518774560591608716857857939226234756331711" +
        "1329986559415968907198506887442300575191977056900382183925622033874235362568083541565172971088117217" +
        "9593683256488518749974870855311659830610139214454460161488452770251141107024852173974510386673640387" +
        "2860099674893173561812071174047889936888655692307848502305705714406363863202368520107410057485922811" +
        "1572196800397824759530016695852212303464187736504354676464565659719011230847670993097085912836466691" +
        "9177693879143331556650669813216415210089571172862384260706784517601113450800699476842235698962488051" +
        "5775980953397080854750597536265649034394454205817886435683042000315095594743439252544850674914290864" +
        "7514423033213324569511634945677539394240360905438335528292434220349484366151466322860247766666049531" +
        "4065734357553014090827988091478669343492273760263499782995701816196432123314047576289748408289117409" +
        "7478263789918169993948749771519898187266629460183053958327520923635068538892284682472599725283007668" +
        "5693758365972291982442974740616381831139583067443485169285973832373926624024345019978099404021896134" +
        "8342736136764499138271541660634248293637418506122610861321199863346284709941839942742955915628333990" +
        "4803821175011612116672051912579303552929241134403116134112495318385926958490443846807849097398280885" +
        "5297045153053991400988698840883654836652224668624087254014040091178742122045230753347397253814940388" +
        "4190586842311594632274433906612516239310628319532388339213153455638151175203510874595582011237543597" +
        "6815534018740739434036339780388172100453169182951948795917673954177879243527617407246059391602732282" +
        "8794681936491289497149534325527235916592980724799858061269007332188445267943350455801952492566306204" +
        "8766161343653399202875452085553441440990512982727454659118132223284051166615650709837557433729548631" +
        "2041121716380915606161165732000083306114606181280326258695951602463216613857661480471993270777131644" +
        "1201594960110632830520759583485030507909558498298218674028983855138323957020807639755042922598476470" +
        "7101642697438450430916586452836032493360435465723755791613663241204578099697156634022158805457943132" +
        "8278005524613208890187421210924489104100521549680971137207540057109634066431357454399159769435788920" +
        "7934256177830222370114864249252392487287131320217667360756645598272609574156602343787436291321097485" +
        "8971507130739104072643541417970572226547980381512759579124002534468048220261734229900102048306246303" +
        "3796474678190501811830375153802879523433419550213568977091290561431787879208620574499925789756901849" +
        "2103242064713851911388147564020976055489579378514140414530515158396428232654060206033118915865702720" +
        "8625026991639375152788736060811455694842103224077727274216513642343669927163403094053074806526850930" +
        "1658921369214143129371341061571537140620397847618426502978078606266969960809184223476335047746719017" +
        "4504514461663828462082408673595102371302904443779408535034454426334130626307459513830310229314693446" +
        "6832851766328241515210179422644395718121717021756492196444939653222218765848824451190940134050443213" +
        "9858628621083179393960844389801914787389772331028631013148695521262051827806349457118662778256598831" +
        "0053515523166598439409022180631445452121297897344714887412582682238602360271099811915205688234723983" +
        "5801336606837863288679286197323672536066852168563201194897807339584191906659583867852941241871821727" +
        "9875061039460648195857456200608921228416394373846549589932028481236433466119707324309545859073361878" +
        "6290631850165106267576851216357588696307451999220010776676830946981497562268243479367131084121021952" +
        "0899481912444048751171059184413990788945577518462161904153093454380280893862807323757861526779711433" +
        "2324196985780563763018088438664060717536832136262967122426094285401109632182627651201170225529292896" +
        "55594608204938409069";
}
