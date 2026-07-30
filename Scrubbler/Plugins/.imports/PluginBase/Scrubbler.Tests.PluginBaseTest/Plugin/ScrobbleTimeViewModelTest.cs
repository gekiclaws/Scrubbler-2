using Scrubbler.PluginBase.Plugin;

namespace Scrubbler.Tests.PluginBaseTest.Plugin;

[TestFixture]
internal class ScrobbleTimeViewModelTest
{
    [Test]
    public void UsesCurrentTime_ByDefault()
    {
        var time = new FakeTimeProvider(DateTimeOffset.Parse("2025-01-01T10:00:00"));
        using var vm = new ScrobbleTimeViewModel(time);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(vm.UseCurrentTime, Is.True);
            Assert.That(vm.Time, Is.EqualTo(TimeSpan.FromHours(10)));
        }
    }

    [Test]
    public void DisablingUseCurrentTime_FreezesTime()
    {
        var now = DateTimeOffset.Parse("2025-01-01T10:00:42.500");
        var time = new FakeTimeProvider(now);
        using var vm = new ScrobbleTimeViewModel(time);

        vm.UseCurrentTime = false;

        time.Advance(TimeSpan.FromHours(1));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(vm.Date, Is.EqualTo(new DateTimeOffset(now.Date, now.Offset)));
            Assert.That(vm.Time, Is.EqualTo(new TimeSpan(10, 0, 42)));
        }
    }

    [Test]
    public void HoursAndMinutesChange_PreservesSelectedSeconds()
    {
        using var vm = new ScrobbleTimeViewModel
        {
            UseCurrentTime = false,
            Time = new TimeSpan(10, 15, 37)
        };

        vm.HoursAndMinutes = new TimeSpan(12, 30, 0);

        Assert.That(vm.Time, Is.EqualTo(new TimeSpan(12, 30, 37)));
    }

    [Test]
    public void SecondsChange_UpdatesTimestampToSelectedSecond()
    {
        using var vm = new ScrobbleTimeViewModel
        {
            UseCurrentTime = false,
            Date = new DateTimeOffset(2024, 01, 01, 0, 0, 0, TimeSpan.Zero),
            Time = new TimeSpan(12, 30, 0)
        };

        vm.Seconds = 37;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(vm.Time, Is.EqualTo(new TimeSpan(12, 30, 37)));
            Assert.That(
                vm.Timestamp,
                Is.EqualTo(new DateTimeOffset(2024, 01, 01, 12, 30, 37, TimeSpan.Zero)));
        }
    }

    [Test]
    public void Timestamp_IsDatePlusTime()
    {
        var time = new FakeTimeProvider(DateTimeOffset.Now);
        using var vm = new ScrobbleTimeViewModel(time)
        {
            UseCurrentTime = false
        };

        vm.Date = new DateTimeOffset(2024, 01, 01, 0, 0, 0, TimeSpan.Zero);
        vm.Time = new TimeSpan(12, 30, 0);

        Assert.That(
            vm.Timestamp,
            Is.EqualTo(new DateTimeOffset(2024, 01, 01, 12, 30, 0, TimeSpan.Zero)));
    }

    [Test]
    public void IsTimeValid_True_ForRecentTime()
    {
        var now = DateTimeOffset.Parse("2025-01-15T10:00:00");
        var time = new FakeTimeProvider(now);

        using var vm = new ScrobbleTimeViewModel(time)
        {
            UseCurrentTime = false
        };

        var valid = now.AddDays(-1);
        vm.Date = valid.Date;
        vm.Time = valid.TimeOfDay;

        Assert.That(vm.IsTimeValid, Is.True);
    }

    [Test]
    public void IsTimeValid_False_ForTooOldTime()
    {
        var now = DateTimeOffset.Parse("2025-01-15T10:00:00");
        var time = new FakeTimeProvider(now);

        using var vm = new ScrobbleTimeViewModel(time)
        {
            UseCurrentTime = false
        };

        var invalid = now.AddDays(-15);
        vm.Date = invalid.Date;
        vm.Time = invalid.TimeOfDay;

        Assert.That(vm.IsTimeValid, Is.False);
    }
}
