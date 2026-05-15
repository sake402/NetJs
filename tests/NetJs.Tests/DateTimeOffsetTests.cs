using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace NetJs.Tests
{
    public static class DateTimeOffsetTests
    {
        public static void Run()
        {
            ConstructorTests();
            PropertyTests();
            ArithmeticTests();
            ComparisonTests();
            FormattingTests();
            ParsingTests();
            ConversionTests();
            UnixTimeTests();
            OffsetTests();
            TicksTests();
            SerializationTests();
            EqualityTests();
            MiscellaneousTests();

            Console.WriteLine("✅ DateTimeOffset tests passed.");
        }

        private static void ConstructorTests()
        {
            var dto1 = new DateTimeOffset(2025, 1, 1, 12, 0, 0, TimeSpan.Zero);
            Debug.Assert(dto1.Year == 2025);
            Debug.Assert(dto1.Offset == TimeSpan.Zero);

            var dto2 = new DateTimeOffset(new DateTime(2025, 1, 1));
            Debug.Assert(dto2.Year == 2025);

            var dto3 = new DateTimeOffset(new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc));
            Debug.Assert(dto3.Offset == TimeSpan.Zero);

            var dto4 = new DateTimeOffset(637765920000000000L, TimeSpan.Zero);
            Debug.Assert(dto4.Ticks == 637765920000000000L);

            var offset = TimeSpan.FromHours(2);
            var dto5 = new DateTimeOffset(2025, 1, 1, 12, 0, 0, offset);
            Debug.Assert(dto5.Offset == offset);
        }

        private static void PropertyTests()
        {
            var dto = new DateTimeOffset(2025, 5, 10, 14, 15, 16, 500, TimeSpan.FromHours(1));

            Debug.Assert(dto.Year == 2025);
            Debug.Assert(dto.Month == 5);
            Debug.Assert(dto.Day == 10);
            Debug.Assert(dto.Hour == 14);
            Debug.Assert(dto.Minute == 15);
            Debug.Assert(dto.Second == 16);
            Debug.Assert(dto.Millisecond == 500);
            Debug.Assert(dto.DayOfWeek == DayOfWeek.Saturday);
            Debug.Assert(dto.DayOfYear == 130);
            Debug.Assert(dto.Offset == TimeSpan.FromHours(1));
            Debug.Assert(dto.UtcDateTime.Kind == DateTimeKind.Utc);
            Debug.Assert(dto.LocalDateTime.Kind == DateTimeKind.Local);
            Debug.Assert(dto.Date == new DateTime(2025, 5, 10));
            Debug.Assert(dto.TimeOfDay == new TimeSpan(14, 15, 16) + TimeSpan.FromMilliseconds(500));
        }

        private static void ArithmeticTests()
        {
            var dto = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);

            Debug.Assert(dto.AddDays(1) == new DateTimeOffset(2025, 1, 2, 0, 0, 0, TimeSpan.Zero));
            Debug.Assert(dto.AddMonths(1) == new DateTimeOffset(2025, 2, 1, 0, 0, 0, TimeSpan.Zero));
            Debug.Assert(dto.AddYears(1) == new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
            Debug.Assert(dto.AddHours(1).Hour == 1);
            Debug.Assert(dto.AddMinutes(30).Minute == 30);
            Debug.Assert(dto.AddSeconds(45).Second == 45);
            Debug.Assert(dto.AddMilliseconds(500).Millisecond == 500);
            Debug.Assert(dto.AddTicks(10).Ticks == dto.Ticks + 10);

            var span = new TimeSpan(1, 2, 3);
            Debug.Assert(dto.Add(span) == dto + span);
            Debug.Assert((dto + span) - dto == span);
            Debug.Assert(dto.Subtract(span) == dto - span);
        }

        private static void ComparisonTests()
        {
            var a = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var b = new DateTimeOffset(2025, 1, 2, 0, 0, 0, TimeSpan.Zero);

            Debug.Assert(a < b);
            Debug.Assert(b > a);
            Debug.Assert(a != b);
            Debug.Assert(a == new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero));

            Debug.Assert(DateTimeOffset.Compare(a, b) < 0);
            Debug.Assert(DateTimeOffset.Compare(b, a) > 0);
            Debug.Assert(DateTimeOffset.Compare(a, a) == 0);

            Debug.Assert(a.CompareTo(b) < 0);
            Debug.Assert(b.CompareTo(a) > 0);
            Debug.Assert(a.Equals(new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero)));
        }

        private static void FormattingTests()
        {
            var dto = new DateTimeOffset(2025, 1, 1, 15, 30, 45, TimeSpan.FromHours(1));

            Debug.Assert(dto.ToString("yyyy-MM-dd") == "2025-01-01");
            Debug.Assert(dto.ToString("HH:mm:ss") == "15:30:45");
            Debug.Assert(dto.ToString("zzz") == "+01:00");

            string roundTrip = dto.ToString("o");
            var parsed = DateTimeOffset.Parse(roundTrip, null, DateTimeStyles.RoundtripKind);

            Debug.Assert(parsed == dto);
        }

        private static void ParsingTests()
        {
            var dto = DateTimeOffset.Parse("2025-01-01T12:00:00+00:00");

            Debug.Assert(dto.Year == 2025);
            Debug.Assert(dto.Offset == TimeSpan.Zero);

            bool success = DateTimeOffset.TryParse(
                "2025-01-01T12:00:00+00:00",
                out var parsed);

            Debug.Assert(success);
            Debug.Assert(parsed.Year == 2025);

            var exact = DateTimeOffset.ParseExact(
                "2025-01-01",
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture);

            Debug.Assert(exact.Year == 2025);

            bool exactSuccess = DateTimeOffset.TryParseExact(
                "2025-01-01",
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var exactParsed);

            Debug.Assert(exactSuccess);
            Debug.Assert(exactParsed.Year == 2025);
        }

        private static void ConversionTests()
        {
            var dto = new DateTimeOffset(2025, 1, 1, 12, 0, 0, TimeSpan.Zero);

            DateTime utc = dto.UtcDateTime;
            DateTime local = dto.LocalDateTime;

            Debug.Assert(utc.Kind == DateTimeKind.Utc);
            Debug.Assert(local.Kind == DateTimeKind.Local);

            var converted = dto.ToOffset(TimeSpan.FromHours(2));

            Debug.Assert(converted.Offset == TimeSpan.FromHours(2));
            Debug.Assert(converted.UtcDateTime == dto.UtcDateTime);

            Debug.Assert(dto.ToUniversalTime().Offset == TimeSpan.Zero);
        }

        private static void UnixTimeTests()
        {
            var now = DateTimeOffset.UtcNow;

            long unixSeconds = now.ToUnixTimeSeconds();
            long unixMilliseconds = now.ToUnixTimeMilliseconds();

            var reconstructedSeconds = DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
            var reconstructedMilliseconds = DateTimeOffset.FromUnixTimeMilliseconds(unixMilliseconds);

            Debug.Assert(Math.Abs((reconstructedSeconds - now).TotalSeconds) < 1);
            Debug.Assert(Math.Abs((reconstructedMilliseconds - now).TotalMilliseconds) < 1000);
        }

        private static void OffsetTests()
        {
            var dto = new DateTimeOffset(2025, 1, 1, 12, 0, 0, TimeSpan.FromHours(3));

            Debug.Assert(dto.Offset == TimeSpan.FromHours(3));

            var utc = dto.ToOffset(TimeSpan.Zero);

            Debug.Assert(utc.Offset == TimeSpan.Zero);
            Debug.Assert(utc.UtcDateTime == dto.UtcDateTime);

            var negativeOffset = dto.ToOffset(TimeSpan.FromHours(-5));

            Debug.Assert(negativeOffset.Offset == TimeSpan.FromHours(-5));
            Debug.Assert(negativeOffset.UtcDateTime == dto.UtcDateTime);
        }

        private static void TicksTests()
        {
            var dto = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);

            Debug.Assert(dto.Ticks > 0);
            Debug.Assert(dto.UtcTicks > 0);

            var recreated = new DateTimeOffset(dto.Ticks, TimeSpan.Zero);

            Debug.Assert(recreated.Ticks == dto.Ticks);

            long ticksPerDay = TimeSpan.TicksPerDay;
            Debug.Assert(dto.AddTicks(ticksPerDay) == dto.AddDays(1));
        }

        private static void SerializationTests()
        {
            var dto = new DateTimeOffset(2025, 1, 1, 12, 0, 0, TimeSpan.FromHours(2));

            string serialized = dto.ToString("o");
            var deserialized = DateTimeOffset.Parse(serialized, null, DateTimeStyles.RoundtripKind);

            Debug.Assert(deserialized == dto);
            Debug.Assert(deserialized.Offset == dto.Offset);
        }

        private static void EqualityTests()
        {
            var a = new DateTimeOffset(2025, 1, 1, 12, 0, 0, TimeSpan.Zero);
            var b = new DateTimeOffset(2025, 1, 1, 14, 0, 0, TimeSpan.FromHours(2));

            Debug.Assert(a.Equals(b));
            Debug.Assert(a.UtcDateTime == b.UtcDateTime);
            Debug.Assert(DateTimeOffset.Equals(a, b));
            Debug.Assert(a.GetHashCode() == b.GetHashCode());
        }

        private static void MiscellaneousTests()
        {
            Debug.Assert(DateTimeOffset.MinValue < DateTimeOffset.MaxValue);

            var now = DateTimeOffset.Now;
            var utcNow = DateTimeOffset.UtcNow;

            Debug.Assert(now.Offset != null);
            Debug.Assert(utcNow.Offset == TimeSpan.Zero);

            var list = new List<DateTimeOffset>
            {
                new DateTimeOffset(2025, 1, 3, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2025, 1, 2, 0, 0, 0, TimeSpan.Zero)
            };

            //var sorted = list.OrderBy(x => x).ToList();

            //Debug.Assert(sorted[0].Day == 1);
            //Debug.Assert(sorted[1].Day == 2);
            //Debug.Assert(sorted[2].Day == 3);

            Debug.Assert(now == now);
            Debug.Assert(now.GetHashCode() == now.GetHashCode());
        }
    }
}
