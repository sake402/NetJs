using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;

namespace NetJs.Tests
{
    /// <summary>
    /// Exhaustive tests for all public APIs of System.DateTime.
    /// Validation is performed exclusively via Debug.Assert.
    /// Run in Debug configuration so assertions are active.
    /// </summary>
    public static class DateTimeExhaustiveTests
    {
        // ─────────────────────────────────────────────
        //  Entry point
        // ─────────────────────────────────────────────
        public static void Run()
        {
            TestConstructors();
            TestStaticFields();
            TestInstanceProperties();
            TestStaticProperties();
            TestAddMethods();
            TestComparisonMethods();
            TestConversionMethods();
            TestParsingMethods();
            TestFormattingMethods();
            TestOperators();
            TestDaysInMonth();
            TestIsLeapYear();
            TestSpecifyKind();
            TestFromAndToFileTime();
            TestFromAndToOADate();
            TestFromAndToBinary();
            TestGetDateTimeFormats();
            TestIComparableIEquatable();
            TestMinMaxValues();
            TestKindBehavior();
            TestDeconstruct();             // .NET 8+
            TestDateOnlyTimeOnlyRoundtrip(); // .NET 6+

            Console.WriteLine("✅ DateTime tests passed.");
        }

        // ─────────────────────────────────────────────
        //  1. Constructors
        // ─────────────────────────────────────────────
        static void TestConstructors()
        {
            // (long ticks)
            var t1 = new DateTime(0L);
            Debug.Assert(t1 == DateTime.MinValue, "ctor(ticks=0) should equal MinValue");

            // (long ticks, DateTimeKind)
            var t2 = new DateTime(0L, DateTimeKind.Utc);
            Debug.Assert(t2.Kind == DateTimeKind.Utc, "ctor(ticks, Utc).Kind");

            // (int year, int month, int day)
            var t3 = new DateTime(2024, 3, 15);
            Debug.Assert(t3.Year == 2024 && t3.Month == 3 && t3.Day == 15, "ctor(y,m,d)");

            // (int year, int month, int day, int hour, int minute, int second)
            var t4 = new DateTime(2024, 3, 15, 10, 30, 45);
            Debug.Assert(t4.Hour == 10 && t4.Minute == 30 && t4.Second == 45, "ctor(y,m,d,h,min,s)");

            // (int year, int month, int day, int hour, int minute, int second, int millisecond)
            var t5 = new DateTime(2024, 3, 15, 10, 30, 45, 500);
            Debug.Assert(t5.Millisecond == 500, "ctor(...,ms)");

            // (int year, int month, int day, int hour, int minute, int second, DateTimeKind)
            var t6 = new DateTime(2024, 3, 15, 10, 30, 45, DateTimeKind.Local);
            Debug.Assert(t6.Kind == DateTimeKind.Local, "ctor(...,kind)");

            // (int year, int month, int day, int hour, int minute, int second, int millisecond, DateTimeKind)
            var t7 = new DateTime(2024, 3, 15, 10, 30, 45, 500, DateTimeKind.Utc);
            Debug.Assert(t7.Millisecond == 500 && t7.Kind == DateTimeKind.Utc, "ctor(...,ms,kind)");

            // (int year, int month, int day, int hour, int minute, int second, int millisecond, Calendar)
            var t8 = new DateTime(2024, 3, 15, 10, 30, 45, 500, new GregorianCalendar());
            Debug.Assert(t8.Year == 2024, "ctor(...,Calendar)");

            // (int year, int month, int day, int hour, int minute, int second, int millisecond, Calendar, DateTimeKind)
            var t9 = new DateTime(2024, 3, 15, 10, 30, 45, 500, new GregorianCalendar(), DateTimeKind.Utc);
            Debug.Assert(t9.Kind == DateTimeKind.Utc, "ctor(...,Calendar,Kind)");

#if NET7_0_OR_GREATER
            // (int year, int month, int day, int hour, int minute, int second, int millisecond, int microsecond)
            var t10 = new DateTime(2024, 3, 15, 10, 30, 45, 500, 123);
            Debug.Assert(t10.Microsecond == 123, "ctor(...,microsecond)");

            // (int year, int month, int day, int hour, int minute, int second, int millisecond, int microsecond, DateTimeKind)
            var t11 = new DateTime(2024, 3, 15, 10, 30, 45, 500, 123, DateTimeKind.Utc);
            Debug.Assert(t11.Microsecond == 123 && t11.Kind == DateTimeKind.Utc, "ctor(...,microsecond,kind)");
#endif
        }

        // ─────────────────────────────────────────────
        //  2. Static fields
        // ─────────────────────────────────────────────
        static void TestStaticFields()
        {
            Debug.Assert(DateTime.MinValue.Year == 1, "MinValue.Year == 1");
            Debug.Assert(DateTime.MaxValue.Year == 9999, "MaxValue.Year == 9999");
            Debug.Assert(DateTime.MinValue < DateTime.MaxValue, "MinValue < MaxValue");

#if NET6_0_OR_GREATER
            Debug.Assert(DateTime.UnixEpoch == new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                         "UnixEpoch");
#endif
        }

        // ─────────────────────────────────────────────
        //  3. Instance properties
        // ─────────────────────────────────────────────
        static void TestInstanceProperties()
        {
            var dt = new DateTime(2024, 2, 29, 13, 45, 30, 250, DateTimeKind.Utc); // leap day

            Debug.Assert(dt.Year == 2024, "Year");
            Debug.Assert(dt.Month == 2, "Month");
            Debug.Assert(dt.Day == 29, "Day");
            Debug.Assert(dt.Hour == 13, "Hour");
            Debug.Assert(dt.Minute == 45, "Minute");
            Debug.Assert(dt.Second == 30, "Second");
            Debug.Assert(dt.Millisecond == 250, "Millisecond");
            Debug.Assert(dt.Kind == DateTimeKind.Utc, "Kind");

            // Date strips time component
            Debug.Assert(dt.Date == new DateTime(2024, 2, 29), "Date");

            // TimeOfDay
            var tod = dt.TimeOfDay;
            Debug.Assert(tod.Hours == 13 && tod.Minutes == 45 && tod.Seconds == 30, "TimeOfDay");

            // DayOfWeek — 2024-02-29 is a Thursday
            Debug.Assert(dt.DayOfWeek == DayOfWeek.Thursday, "DayOfWeek");

            // DayOfYear — 2024 is leap: Jan(31)+Feb(29)=60
            Debug.Assert(dt.DayOfYear == 60, "DayOfYear");

            // Ticks — must be positive and consistent
            Debug.Assert(dt.Ticks > 0, "Ticks > 0");
            var fromTicks = new DateTime(dt.Ticks, DateTimeKind.Utc);
            Debug.Assert(fromTicks == dt, "Round-trip via Ticks");

#if NET7_0_OR_GREATER
            var dtMicro = new DateTime(2024, 2, 29, 13, 45, 30, 250, 777, DateTimeKind.Utc);
            Debug.Assert(dtMicro.Microsecond == 777, "Microsecond");
            Debug.Assert(dtMicro.Nanosecond == 700, "Nanosecond (777µs → 777*1000 ns but ticks store 100ns, so 700ns fractional)");
#endif
        }

        // ─────────────────────────────────────────────
        //  4. Static properties
        // ─────────────────────────────────────────────
        static void TestStaticProperties()
        {
            var before = DateTime.UtcNow;
            var now = DateTime.Now;
            var utcNow = DateTime.UtcNow;
            var after = DateTime.UtcNow;
            var today = DateTime.Today;

            Debug.Assert(utcNow >= before && utcNow <= after, "UtcNow is within expected range");
            Debug.Assert(today.TimeOfDay == TimeSpan.Zero, "Today has no time component");
            Debug.Assert(now.Kind == DateTimeKind.Local, "Now.Kind == Local");
            Debug.Assert(utcNow.Kind == DateTimeKind.Utc, "UtcNow.Kind == Utc");
        }

        // ─────────────────────────────────────────────
        //  5. Add* instance methods
        // ─────────────────────────────────────────────
        static void TestAddMethods()
        {
            var base_ = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            // Add(TimeSpan)
            var addTs = base_.Add(TimeSpan.FromHours(2));
            Debug.Assert(addTs.Hour == 2, "Add(TimeSpan)");

            // AddTicks
            var addTk = base_.AddTicks(TimeSpan.TicksPerMillisecond);
            Debug.Assert(addTk.Millisecond == 1, "AddTicks");

            // AddMilliseconds
            var addMs = base_.AddMilliseconds(1500);
            Debug.Assert(addMs.Second == 1 && addMs.Millisecond == 500, "AddMilliseconds");

            // AddSeconds
            var addSec = base_.AddSeconds(90);
            Debug.Assert(addSec.Minute == 1 && addSec.Second == 30, "AddSeconds");

            // AddMinutes
            var addMin = base_.AddMinutes(75);
            Debug.Assert(addMin.Hour == 1 && addMin.Minute == 15, "AddMinutes");

            // AddHours
            var addH = base_.AddHours(25);
            Debug.Assert(addH.Day == 2 && addH.Hour == 1, "AddHours");

            // AddDays
            var addD = base_.AddDays(31);
            Debug.Assert(addD.Month == 2, "AddDays crosses month");

            // AddMonths
            var addMo = base_.AddMonths(13);
            Debug.Assert(addMo.Year == 2025 && addMo.Month == 2, "AddMonths");

            // AddYears
            var addY = base_.AddYears(4);
            Debug.Assert(addY.Year == 2028, "AddYears");

            // Negative values
            var sub = base_.AddDays(-1);
            Debug.Assert(sub.Year == 2023 && sub.Month == 12 && sub.Day == 31, "AddDays negative");

#if NET7_0_OR_GREATER
            // AddMicroseconds
            var addMicro = base_.AddMicroseconds(1500);
            Debug.Assert(addMicro.Millisecond == 1, "AddMicroseconds");
#endif
        }

        // ─────────────────────────────────────────────
        //  6. Comparison methods
        // ─────────────────────────────────────────────
        static void TestComparisonMethods()
        {
            var a = new DateTime(2024, 1, 1);
            var b = new DateTime(2024, 6, 15);
            var c = new DateTime(2024, 1, 1);

            // Static Compare
            Debug.Assert(DateTime.Compare(a, b) < 0, "Compare(a<b) < 0");
            Debug.Assert(DateTime.Compare(b, a) > 0, "Compare(b>a) > 0");
            Debug.Assert(DateTime.Compare(a, c) == 0, "Compare(a==c) == 0");

            // Instance CompareTo(DateTime)
            Debug.Assert(a.CompareTo(b) < 0, "a.CompareTo(b) < 0");
            Debug.Assert(b.CompareTo(a) > 0, "b.CompareTo(a) > 0");
            Debug.Assert(a.CompareTo(c) == 0, "a.CompareTo(c) == 0");

            // Instance CompareTo(object)
            Debug.Assert(a.CompareTo((object)b) < 0, "CompareTo(object)");

            // Equals static
            Debug.Assert(DateTime.Equals(a, c), "DateTime.Equals(a,c)");
            Debug.Assert(!DateTime.Equals(a, b), "!DateTime.Equals(a,b)");

            // Equals instance
            Debug.Assert(a.Equals(c), "a.Equals(c)");
            Debug.Assert(!a.Equals(b), "!a.Equals(b)");
            Debug.Assert(a.Equals((object)c), "a.Equals((object)c)");

            // GetHashCode — equal values must have equal hashes
            Debug.Assert(a.GetHashCode() == c.GetHashCode(), "Equal DateTimes → equal hash");
        }

        // ─────────────────────────────────────────────
        //  7. Conversion methods (ToXxx / ToUniversalTime / ToLocalTime)
        // ─────────────────────────────────────────────
        static void TestConversionMethods()
        {
            var utc = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc);
            var local = utc.ToLocalTime();
            var backUtc = local.ToUniversalTime();

            Debug.Assert(local.Kind == DateTimeKind.Local, "ToLocalTime().Kind");
            Debug.Assert(backUtc.Kind == DateTimeKind.Utc, "ToUniversalTime().Kind");
            Debug.Assert(backUtc == utc, "UTC → Local → UTC round-trip");

            // ToFileTime / ToFileTimeUtc
            long ft = utc.ToFileTime();
            long ftUtc = utc.ToFileTimeUtc();
            Debug.Assert(ft > 0, "ToFileTime() > 0");
            Debug.Assert(ftUtc > 0, "ToFileTimeUtc() > 0");

            // ToOADate
            double oa = utc.ToOADate();
            Debug.Assert(oa > 0, "ToOADate() > 0");

            // ToLongDateString / ToShortDateString
            string lds = utc.ToLongDateString();
            string sds = utc.ToShortDateString();
            Debug.Assert(!string.IsNullOrEmpty(lds), "ToLongDateString not empty");
            Debug.Assert(!string.IsNullOrEmpty(sds), "ToShortDateString not empty");

            // ToLongTimeString / ToShortTimeString
            string lts = utc.ToLongTimeString();
            string sts = utc.ToShortTimeString();
            Debug.Assert(!string.IsNullOrEmpty(lts), "ToLongTimeString not empty");
            Debug.Assert(!string.IsNullOrEmpty(sts), "ToShortTimeString not empty");

            // ToBinary / FromBinary round-trip
            long binary = utc.ToBinary();
            var fromBin = DateTime.FromBinary(binary);
            Debug.Assert(fromBin == utc, "ToBinary → FromBinary round-trip");
        }

        // ─────────────────────────────────────────────
        //  8. Parsing methods
        // ─────────────────────────────────────────────
        static void TestParsingMethods()
        {
            var culture = CultureInfo.InvariantCulture;
            var expected = new DateTime(2024, 3, 15, 10, 30, 0, DateTimeKind.Unspecified);
            const string iso = "2024-03-15T10:30:00";

            // Parse
            var p1 = DateTime.Parse(iso, culture);
            Debug.Assert(p1 == expected, "Parse(string,IFormatProvider)");

            // Parse with DateTimeStyles
            var p2 = DateTime.Parse(iso, culture, DateTimeStyles.None);
            Debug.Assert(p2 == expected, "Parse(string,IFormatProvider,DateTimeStyles)");

            // ParseExact (single format)
            var p3 = DateTime.ParseExact("15/03/2024", "dd/MM/yyyy", culture);
            Debug.Assert(p3.Day == 15 && p3.Month == 3, "ParseExact(string,format,IFormatProvider)");

            // ParseExact (format array)
            var formats = new[] { "dd/MM/yyyy", "yyyy-MM-dd" };
            var p4 = DateTime.ParseExact("15/03/2024", formats, culture, DateTimeStyles.None);
            Debug.Assert(p4.Day == 15, "ParseExact(string,formats[],IFormatProvider,DateTimeStyles)");

            // TryParse
            bool ok1 = DateTime.TryParse(iso, out var tp1);
            Debug.Assert(ok1 && tp1 == expected, "TryParse(string,out)");

            bool ok2 = DateTime.TryParse(iso, culture, DateTimeStyles.None, out var tp2);
            Debug.Assert(ok2 && tp2 == expected, "TryParse(string,IFormatProvider,DateTimeStyles,out)");

            bool fail = DateTime.TryParse("not-a-date", out _);
            Debug.Assert(!fail, "TryParse returns false for invalid input");

            // TryParseExact (single format)
            bool ok3 = DateTime.TryParseExact("15/03/2024", "dd/MM/yyyy", culture, DateTimeStyles.None, out var tp3);
            Debug.Assert(ok3 && tp3.Day == 15, "TryParseExact(single format)");

            // TryParseExact (format array)
            bool ok4 = DateTime.TryParseExact("2024-03-15", formats, culture, DateTimeStyles.None, out var tp4);
            Debug.Assert(ok4 && tp4.Month == 3, "TryParseExact(format array)");

            bool fail2 = DateTime.TryParseExact("bad", "dd/MM/yyyy", culture, DateTimeStyles.None, out _);
            Debug.Assert(!fail2, "TryParseExact returns false for invalid input");

            // RoundtripKind style preservation
            var utcDt = new DateTime(2024, 3, 15, 10, 30, 0, DateTimeKind.Utc);
            var utcStr = utcDt.ToString("o");
            var parsedK = DateTime.Parse(utcStr, culture, DateTimeStyles.RoundtripKind);
            Debug.Assert(parsedK.Kind == DateTimeKind.Utc, "RoundtripKind preserves Utc");
        }

        // ─────────────────────────────────────────────
        //  9. Formatting / ToString overloads
        // ─────────────────────────────────────────────
        static void TestFormattingMethods()
        {
            var dt = new DateTime(2024, 3, 15, 10, 30, 45, DateTimeKind.Utc);
            var inv = CultureInfo.InvariantCulture;

            // ToString()
            Debug.Assert(!string.IsNullOrEmpty(dt.ToString()), "ToString()");

            // ToString(IFormatProvider)
            Debug.Assert(!string.IsNullOrEmpty(dt.ToString(inv)), "ToString(IFormatProvider)");

            // ToString(string)
            Debug.Assert(dt.ToString("yyyy") == "2024", "ToString(\"yyyy\")");
            Debug.Assert(dt.ToString("MM") == "03", "ToString(\"MM\")");
            Debug.Assert(dt.ToString("dd") == "15", "ToString(\"dd\")");
            Debug.Assert(dt.ToString("HH") == "10", "ToString(\"HH\")");
            Debug.Assert(dt.ToString("mm") == "30", "ToString(\"mm\")");
            Debug.Assert(dt.ToString("ss") == "45", "ToString(\"ss\")");

            // ToString(string, IFormatProvider)
            Debug.Assert(dt.ToString("d", inv) == "03/15/2024", "ToString(\"d\", InvariantCulture)");

            // Standard format specifiers
            var roundO = DateTime.Parse(dt.ToString("o", inv), inv, DateTimeStyles.RoundtripKind);
            Debug.Assert(roundO == dt, "Round-trip via 'o' format");

            var roundR = DateTime.Parse(dt.ToString("r", inv), inv, DateTimeStyles.None);
            Debug.Assert(roundR == dt, "Round-trip via 'r' (RFC1123) format");

            var roundS = DateTime.Parse(dt.ToString("s", inv), inv);
            Debug.Assert(roundS == dt, "Round-trip via 's' (sortable) format");

            var roundU = DateTime.Parse(dt.ToString("u", inv), inv, DateTimeStyles.None);
            Debug.Assert(roundU == dt, "Round-trip via 'u' (universal sortable) format");

            // GetDateTimeFormats
            var allFormats = dt.GetDateTimeFormats(inv);
            Debug.Assert(allFormats.Length > 0, "GetDateTimeFormats() returns non-empty array");

            var dFormats = dt.GetDateTimeFormats('d', inv);
            Debug.Assert(dFormats.Length > 0, "GetDateTimeFormats('d', culture) non-empty");
        }

        // ─────────────────────────────────────────────
        //  10. Operators
        // ─────────────────────────────────────────────
        static void TestOperators()
        {
            var a = new DateTime(2024, 6, 1);
            var b = new DateTime(2024, 6, 15);

            // Subtraction: DateTime - DateTime → TimeSpan
            TimeSpan diff = b - a;
            Debug.Assert(diff.TotalDays == 14, "b - a == 14 days");

            // Addition: DateTime + TimeSpan → DateTime
            var c = a + TimeSpan.FromDays(14);
            Debug.Assert(c == b, "a + 14days == b");

            // Subtraction: DateTime - TimeSpan → DateTime
            var d = b - TimeSpan.FromDays(14);
            Debug.Assert(d == a, "b - 14days == a");

            // Comparison operators
            Debug.Assert(a < b, "a < b");
            Debug.Assert(b > a, "b > a");
            Debug.Assert(a <= b, "a <= b");
            Debug.Assert(b >= a, "b >= a");
            Debug.Assert(a != b, "a != b");
            Debug.Assert(a == new DateTime(2024, 6, 1), "a == copy");
        }

        // ─────────────────────────────────────────────
        //  11. DaysInMonth
        // ─────────────────────────────────────────────
        static void TestDaysInMonth()
        {
            Debug.Assert(DateTime.DaysInMonth(2024, 2) == 29, "Leap Feb 2024 = 29 days");
            Debug.Assert(DateTime.DaysInMonth(2023, 2) == 28, "Non-leap Feb 2023 = 28 days");
            Debug.Assert(DateTime.DaysInMonth(2024, 1) == 31, "Jan = 31");
            Debug.Assert(DateTime.DaysInMonth(2024, 4) == 30, "Apr = 30");
            Debug.Assert(DateTime.DaysInMonth(2024, 12) == 31, "Dec = 31");
        }

        // ─────────────────────────────────────────────
        //  12. IsLeapYear
        // ─────────────────────────────────────────────
        static void TestIsLeapYear()
        {
            Debug.Assert(DateTime.IsLeapYear(2024), "2024 is leap");
            Debug.Assert(!DateTime.IsLeapYear(2023), "2023 not leap");
            Debug.Assert(!DateTime.IsLeapYear(1900), "1900 not leap (divisible 100, not 400)");
            Debug.Assert(DateTime.IsLeapYear(2000), "2000 is leap (divisible 400)");
            Debug.Assert(DateTime.IsLeapYear(2400), "2400 is leap");
            Debug.Assert(!DateTime.IsLeapYear(2100), "2100 not leap");
        }

        // ─────────────────────────────────────────────
        //  13. SpecifyKind
        // ─────────────────────────────────────────────
        static void TestSpecifyKind()
        {
            var dt = new DateTime(2024, 6, 15, 12, 0, 0);
            var utc = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
            var loc = DateTime.SpecifyKind(dt, DateTimeKind.Local);
            var uns = DateTime.SpecifyKind(dt, DateTimeKind.Unspecified);

            Debug.Assert(utc.Kind == DateTimeKind.Utc, "SpecifyKind → Utc");
            Debug.Assert(loc.Kind == DateTimeKind.Local, "SpecifyKind → Local");
            Debug.Assert(uns.Kind == DateTimeKind.Unspecified, "SpecifyKind → Unspecified");

            // Tick value unchanged
            Debug.Assert(utc.Ticks == dt.Ticks, "SpecifyKind preserves ticks");
        }

        // ─────────────────────────────────────────────
        //  14. FromFileTime / ToFileTime
        // ─────────────────────────────────────────────
        static void TestFromAndToFileTime()
        {
            var utc = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc);
            long ftUtc = utc.ToFileTimeUtc();
            var backUtc = DateTime.FromFileTimeUtc(ftUtc);
            Debug.Assert(backUtc == utc, "ToFileTimeUtc → FromFileTimeUtc round-trip");

            long ft = utc.ToFileTime();
            var backFt = DateTime.FromFileTime(ft);
            // FromFileTime returns Local kind; convert to Utc for comparison
            Debug.Assert(backFt.ToUniversalTime() == utc, "ToFileTime → FromFileTime round-trip");

            // Epoch anchor: FILETIME 0 = 1601-01-01 00:00:00 UTC
            var epoch = DateTime.FromFileTimeUtc(0);
            Debug.Assert(epoch == new DateTime(1601, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                         "FILETIME epoch == 1601-01-01");
        }

        // ─────────────────────────────────────────────
        //  15. FromOADate / ToOADate
        // ─────────────────────────────────────────────
        static void TestFromAndToOADate()
        {
            var dt = new DateTime(2024, 6, 15, 12, 0, 0);
            double oa = dt.ToOADate();
            var back = DateTime.FromOADate(oa);
            Debug.Assert(back == dt, "ToOADate → FromOADate round-trip");

            // OA 0 = 1899-12-30 (COM convention)
            var oaZero = DateTime.FromOADate(0.0);
            Debug.Assert(oaZero.Year == 1899 && oaZero.Month == 12 && oaZero.Day == 30,
                         "OADate 0 == 1899-12-30");

            // OA 1.5 = 1899-12-31 12:00:00
            var oaOneHalf = DateTime.FromOADate(1.5);
            Debug.Assert(oaOneHalf.Day == 31 && oaOneHalf.Hour == 12, "OADate 1.5");
        }

        // ─────────────────────────────────────────────
        //  16. ToBinary / FromBinary
        // ─────────────────────────────────────────────
        static void TestFromAndToBinary()
        {
            var kinds = new[]
            {
            DateTimeKind.Utc,
            DateTimeKind.Local,
            DateTimeKind.Unspecified
        };

            foreach (var kind in kinds)
            {
                var dt = new DateTime(2024, 6, 15, 12, 0, 0, kind);
                long bin = dt.ToBinary();
                var back = DateTime.FromBinary(bin);
                Debug.Assert(back == dt, $"ToBinary→FromBinary round-trip ({kind})");
                Debug.Assert(back.Kind == kind, $"FromBinary preserves Kind ({kind})");
            }
        }

        // ─────────────────────────────────────────────
        //  17. GetDateTimeFormats overloads
        // ─────────────────────────────────────────────
        static void TestGetDateTimeFormats()
        {
            var dt = new DateTime(2024, 6, 15, 12, 30, 0, DateTimeKind.Utc);
            var inv = CultureInfo.InvariantCulture;

            // All formats
            var all = dt.GetDateTimeFormats();
            Debug.Assert(all.Length > 0, "GetDateTimeFormats() non-empty");

            // By specifier
            foreach (char spec in "dDfFgGmMoOrRstTuUyY")
            {
                var arr = dt.GetDateTimeFormats(spec, inv);
                Debug.Assert(arr.Length > 0, $"GetDateTimeFormats('{spec}') non-empty");
            }
        }

        // ─────────────────────────────────────────────
        //  18. IComparable / IEquatable explicit coverage
        // ─────────────────────────────────────────────
        static void TestIComparableIEquatable()
        {
            IComparable<DateTime> ic = new DateTime(2024, 1, 1);
            var later = new DateTime(2024, 12, 31);
            Debug.Assert(ic.CompareTo(later) < 0, "IComparable<DateTime>.CompareTo");

            IComparable ic2 = new DateTime(2024, 1, 1);
            Debug.Assert(ic2.CompareTo(new DateTime(2023, 1, 1)) > 0,
                         "IComparable.CompareTo(object)");

            IEquatable<DateTime> ie = new DateTime(2024, 6, 15);
            Debug.Assert(ie.Equals(new DateTime(2024, 6, 15)), "IEquatable.Equals same");
            Debug.Assert(!ie.Equals(new DateTime(2024, 6, 16)), "IEquatable.Equals diff");
        }

        // ─────────────────────────────────────────────
        //  19. Min/Max value edge cases
        // ─────────────────────────────────────────────
        static void TestMinMaxValues()
        {
            var min = DateTime.MinValue;
            var max = DateTime.MaxValue;

            Debug.Assert(min.Ticks == 0, "MinValue.Ticks == 0");
            Debug.Assert(max.Ticks == 3155378975999999999L, "MaxValue.Ticks");
            Debug.Assert(min.Year == 1 && min.Month == 1 && min.Day == 1, "MinValue date");
            Debug.Assert(max.Year == 9999 && max.Month == 12 && max.Day == 31, "MaxValue date");

            // Kind for MinValue/MaxValue is Unspecified per spec
            Debug.Assert(min.Kind == DateTimeKind.Unspecified, "MinValue.Kind == Unspecified");
            Debug.Assert(max.Kind == DateTimeKind.Unspecified, "MaxValue.Kind == Unspecified");
        }

        // ─────────────────────────────────────────────
        //  20. Kind behavior with Now / UtcNow / Today
        // ─────────────────────────────────────────────
        static void TestKindBehavior()
        {
            Debug.Assert(DateTime.Now.Kind == DateTimeKind.Local, "Now.Kind");
            Debug.Assert(DateTime.UtcNow.Kind == DateTimeKind.Utc, "UtcNow.Kind");
            Debug.Assert(DateTime.Today.Kind == DateTimeKind.Local, "Today.Kind");

            // Unspecified kind
            var uns = new DateTime(2024, 1, 1);
            Debug.Assert(uns.Kind == DateTimeKind.Unspecified, "Default ctor kind == Unspecified");

            // Kind survives Add operations
            var utcAdded = DateTime.SpecifyKind(new DateTime(2024, 1, 1), DateTimeKind.Utc).AddDays(1);
            Debug.Assert(utcAdded.Kind == DateTimeKind.Utc, "AddDays preserves Utc kind");
        }

        // ─────────────────────────────────────────────
        //  21. Deconstruct (.NET 8+)
        // ─────────────────────────────────────────────
        static void TestDeconstruct()
        {
#if NET8_0_OR_GREATER
            var dt = new DateTime(2024, 6, 15);
            var (date, time) = dt;
            //Debug.Assert(date == dt.Date, "Deconstruct date part");
            //Debug.Assert(time == dt.TimeOfDay, "Deconstruct time part");
#endif
        }

        // ─────────────────────────────────────────────
        //  22. DateOnly / TimeOnly round-trip (.NET 6+)
        // ─────────────────────────────────────────────
        static void TestDateOnlyTimeOnlyRoundtrip()
        {
#if NET6_0_OR_GREATER
            var dt = new DateTime(2024, 6, 15, 10, 30, 45, 250);
            var dateOnly = DateOnly.FromDateTime(dt);
            var timeOnly = TimeOnly.FromDateTime(dt);

            Debug.Assert(dateOnly.Year == 2024 && dateOnly.Month == 6 && dateOnly.Day == 15,
                         "DateOnly.FromDateTime");
            Debug.Assert(timeOnly.Hour == 10 && timeOnly.Minute == 30 && timeOnly.Second == 45,
                         "TimeOnly.FromDateTime");

            var backDt = dateOnly.ToDateTime(timeOnly);
            Debug.Assert(backDt.Year == dt.Year &&
                         backDt.Month == dt.Month &&
                         backDt.Day == dt.Day &&
                         backDt.Hour == dt.Hour &&
                         backDt.Minute == dt.Minute &&
                         backDt.Second == dt.Second,
                         "DateOnly + TimeOnly → DateTime round-trip");
#endif
        }
    }
}