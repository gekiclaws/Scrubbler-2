using CommunityToolkit.Mvvm.ComponentModel;

namespace Scrubbler.PluginBase.Plugin;

public partial class ScrobbleTimeViewModel : ObservableObject, IDisposable
{
    private readonly TimeProvider _timeProvider;
    private readonly CancellationTokenSource _cts = new();

    private DateTimeOffset _date;
    private TimeSpan _time;
    private bool _useCurrentTime;

    public DateTimeOffset Date
    {
        get
        {
            if (!UseCurrentTime)
                return _date;

            var now = _timeProvider.GetLocalNow();
            return new DateTimeOffset(now.Date, now.Offset);
        }
        set
        {
            if (_date != value)
            {
                _date = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Timestamp));
                OnPropertyChanged(nameof(IsTimeValid));
            }
        }
    }

    public TimeSpan Time
    {
        get => UseCurrentTime ? _timeProvider.GetLocalNow().TimeOfDay : _time;
        set
        {
            if (_time != value)
            {
                _time = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HoursAndMinutes));
                OnPropertyChanged(nameof(Seconds));
                OnPropertyChanged(nameof(Timestamp));
                OnPropertyChanged(nameof(IsTimeValid));
            }
        }
    }

    public TimeSpan HoursAndMinutes
    {
        get
        {
            var time = Time;
            return new TimeSpan(time.Hours, time.Minutes, 0);
        }
        set
        {
            var time = Time;
            Time = new TimeSpan(value.Hours, value.Minutes, time.Seconds);
        }
    }

    public double Seconds
    {
        get => Time.Seconds;
        set
        {
            if (!double.IsFinite(value))
                return;

            var seconds = (int)Math.Clamp(
                Math.Round(value, MidpointRounding.AwayFromZero),
                0,
                59);
            var time = Time;
            Time = new TimeSpan(time.Hours, time.Minutes, seconds);
        }
    }

    public bool UseCurrentTime
    {
        get => _useCurrentTime;
        set
        {
            if (_useCurrentTime != value)
            {
                if (!value)
                {
                    var now = _timeProvider.GetLocalNow();
                    _date = new DateTimeOffset(now.Date, now.Offset);
                    _time = TimeSpan.FromSeconds(Math.Floor(now.TimeOfDay.TotalSeconds));
                }

                _useCurrentTime = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Time));
                OnPropertyChanged(nameof(HoursAndMinutes));
                OnPropertyChanged(nameof(Seconds));
                OnPropertyChanged(nameof(Date));
                OnPropertyChanged(nameof(Timestamp));
                OnPropertyChanged(nameof(IsTimeValid));
            }
        }
    }

    public DateTimeOffset Timestamp => UseCurrentTime ? _timeProvider.GetLocalNow() : Date + Time;

    public bool IsTimeValid
    {
        get
        {
            var now = _timeProvider.GetLocalNow();
            return Timestamp >= now.AddDays(-14) && Timestamp < now.AddDays(1);
        }
    }

    public ScrobbleTimeViewModel(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
        UseCurrentTime = true;

        _ = UpdateLoopAsync(_cts.Token);
    }

    private async Task UpdateLoopAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                if (UseCurrentTime)
                {
                    OnPropertyChanged(nameof(Time));
                    OnPropertyChanged(nameof(HoursAndMinutes));
                    OnPropertyChanged(nameof(Seconds));
                    OnPropertyChanged(nameof(Date));
                    OnPropertyChanged(nameof(Timestamp));
                }

                OnPropertyChanged(nameof(IsTimeValid));
                await Task.Delay(1000, ct);
            }
        }
        catch (OperationCanceledException)
        {
            // expected
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}
