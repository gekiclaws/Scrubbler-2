using CommunityToolkit.Mvvm.ComponentModel;
using Scrubbler.PluginBase;
using Scrubbler.PluginBase.Plugin;

namespace Scrubbler.Plugin.Scrobblers.ManualScrobbler;

public partial class ManualScrobbleViewModel : ScrobblePluginViewModelBase
{
    #region Properties

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanScrobble))]
    private string _artistName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanScrobble))]
    private string _trackName = string.Empty;

    [ObservableProperty]
    private string _albumName = string.Empty;

    [ObservableProperty]
    private string _albumArtistName = string.Empty;

    public override bool CanScrobble => !string.IsNullOrEmpty(ArtistName) && !string.IsNullOrEmpty(TrackName) && ScrobbleTimeVM.IsTimeValid;

    public ScrobbleTimeViewModel ScrobbleTimeVM { get; }

    [Obsolete("Use ScrobbleTimeVM.Date instead.")]
    public DateTimeOffset PlayedAt
    {
        get => ScrobbleTimeVM.Date;
        set
        {
            ScrobbleTimeVM.UseCurrentTime = false;
            ScrobbleTimeVM.Date = value.Date;
        }
    }

    [Obsolete("Use ScrobbleTimeVM.Time instead.")]
    public TimeSpan PlayedAtTime
    {
        get => ScrobbleTimeVM.Time;
        set
        {
            ScrobbleTimeVM.UseCurrentTime = false;
            ScrobbleTimeVM.Time = value;
        }
    }

    #endregion Properties

    #region Construction

    public ManualScrobbleViewModel()
    {
        ScrobbleTimeVM = new ScrobbleTimeViewModel();
        ScrobbleTimeVM.PropertyChanged += ScrobbleTimeVM_PropertyChanged;
    }

    #endregion Construction

    public override async Task<IEnumerable<ScrobbleData>> GetScrobblesAsync()
    {
        if (!CanScrobble)
            throw new InvalidOperationException("Invalid data for scrobble creation");

        IsBusy = true;

        try
        {
            return await Task.Run(() =>
            {
                return new[] { new ScrobbleData(TrackName, ArtistName, ScrobbleTimeVM.Timestamp) { Album = AlbumName, AlbumArtist = AlbumArtistName } };
            });
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ScrobbleTimeVM_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(CanScrobble));
    }
}
