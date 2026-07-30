using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediaPlayerScrobblerBase;
using Scrubbler.MediaPlayerScrobblerBase;
using Scrubbler.PluginBase;
using Scrubbler.PluginBase.Discord;
using Scrubbler.PluginBase.Services;
using Scrubbler.Plugins.Scrobblers.MediaPlayerScrobbleBase;
using Shoegaze.LastFM;

namespace Scrubbler.Plugin.Scrobblers.CloneHeroScrobbler;

internal partial class CloneHeroScrobbleViewModel : MediaPlayerScrobblePluginViewModelBase
{
  public override string CurrentTrackName => _currentSong?.Track ?? string.Empty;

  public override string CurrentArtistName => _currentSong?.Artist ?? string.Empty;

  public override string CurrentAlbumName => _currentSong?.Album ?? string.Empty;

  public override int CurrentTrackLength => Math.Max(1, NormalizedScrobbleThresholdSeconds * 2);

  [ObservableProperty]
  private string _dataFolderPath = CloneHeroFileSongSource.GetDefaultDataFolderPath();

  [ObservableProperty]
  private double _scrobbleThresholdSeconds = PluginDefaults.DefaultScrobbleThresholdSeconds;

  [ObservableProperty]
  private string _statusMessage = "Not connected";

  private readonly ICloneHeroSongSource _songSource;
  private readonly ITickSource _pollTicks;
  private readonly IFilePickerService _filePicker;
  private CloneHeroSong? _currentSong;
  private DateTimeOffset _currentSongStartedAt;

  internal int NormalizedScrobbleThresholdSeconds
  {
    get
    {
      if (!double.IsFinite(ScrobbleThresholdSeconds))
        return PluginDefaults.DefaultScrobbleThresholdSeconds;

      return Math.Clamp((int)Math.Round(ScrobbleThresholdSeconds), PluginDefaults.DefaultScrobbleThresholdSeconds, PluginDefaults.MaximumScrobbleThresholdSeconds);
    }
  }

  public CloneHeroScrobbleViewModel(
    ILastfmClient lastfmClient,
    ILogService logger,
    ICloneHeroSongSource songSource,
    ITickSource pollTicks,
    IFilePickerService filePicker,
    IDiscordRichPresence richPresence)
    : base(lastfmClient, richPresence, new DiscordRichPresenceData("scrubbler", "Scrubbler", "scrubbler", "Scrubbler"), logger)
  {
    _songSource = songSource;
    _pollTicks = pollTicks;
    _filePicker = filePicker;

    _pollTicks.Tick += OnPollTick;
  }

  protected override Task Connect()
  {
    try
    {
      IsBusy = true;

      if (string.IsNullOrWhiteSpace(DataFolderPath))
        DataFolderPath = CloneHeroFileSongSource.GetDefaultDataFolderPath();

      _currentSong = null;
      ClearState();
      StatusMessage = $"Watching {DataFolderPath}";
      IsConnected = true;

      PollCurrentSong();
      _pollTicks.Start();
    }
    catch (Exception ex)
    {
      _logger.Error("Error connecting to Clone Hero", ex);
      IsConnected = false;
      StatusMessage = "Connection failed";
    }
    finally
    {
      IsBusy = false;
    }

    return Task.CompletedTask;
  }

  protected override Task Disconnect()
  {
    _pollTicks.Stop();
    _currentSong = null;
    ClearState();
    IsConnected = false;
    StatusMessage = "Not connected";

    return Task.CompletedTask;
  }

  [RelayCommand]
  private async Task SelectCurrentSongFile()
  {
    var file = await _filePicker.PickFileAsync([".txt"]);
    if (file is null || string.IsNullOrWhiteSpace(file.Path))
      return;

    var folder = Path.GetDirectoryName(file.Path);
    if (!string.IsNullOrWhiteSpace(folder))
      DataFolderPath = folder;
  }

  private void OnPollTick(object? sender, EventArgs e)
  {
    PollCurrentSong();
  }

  private void PollCurrentSong()
  {
    if (!IsConnected)
      return;

    try
    {
      if (!Directory.Exists(DataFolderPath))
      {
        ClearCurrentSong($"Clone Hero data folder not found: {DataFolderPath}");
        return;
      }

      var song = _songSource.GetCurrentSong(DataFolderPath);
      if (song is null)
      {
        ClearCurrentSong($"Waiting for {PluginDefaults.CurrentSongFileName}");
        return;
      }

      if (_currentSong is null || !_currentSong.SameScrobbleIdentity(song))
      {
        StartCurrentSong(song);
        return;
      }

      CountCurrentSongSecond();
    }
    catch (Exception ex)
    {
      _logger.Error("Error while reading Clone Hero current song data", ex);
      ClearCurrentSong("Could not read Clone Hero current song data");
    }
  }

  private void StartCurrentSong(CloneHeroSong song)
  {
    _currentSong = song;
    _currentSongStartedAt = DateTimeOffset.UtcNow;

    var albumText = string.IsNullOrWhiteSpace(song.Album) ? string.Empty : $", from the album \"{song.Album}\"";
    _logger.Info($"Now playing \"{song.Track}\" by {song.Artist}{albumText}.");

    StatusMessage = "Clone Hero playback detected";
    ClearState();
  }

  private void CountCurrentSongSecond()
  {
    if (_currentSong is null || CurrentTrackScrobbled)
      return;

    CountedSeconds++;

    if (CountedSeconds < CurrentTrackLengthToScrobble)
      return;

    var scrobble = new ScrobbleData(_currentSong.Track, _currentSong.Artist, _currentSongStartedAt);
    if (!string.IsNullOrWhiteSpace(_currentSong.Album))
    {
      scrobble.Album = _currentSong.Album;
      scrobble.AlbumArtist = _currentSong.Artist;
    }

    OnScrobblesDetected([scrobble]);
    CurrentTrackScrobbled = true;
    CountedSeconds = CurrentTrackLengthToScrobble;
    StatusMessage = "Current track scrobbled";
  }

  private void ClearCurrentSong(string statusMessage)
  {
    StatusMessage = statusMessage;

    if (_currentSong is null)
    {
      CountedSeconds = 0;
      CurrentTrackScrobbled = false;
      return;
    }

    _logger.Info("Currently not playing anything.");
    _currentSong = null;
    _currentSongStartedAt = default;
    ClearState();
  }

  partial void OnScrobbleThresholdSecondsChanged(double value)
  {
    var normalized = NormalizedScrobbleThresholdSeconds;
    if (!double.IsFinite(value) || Math.Abs(value - normalized) > 0.001d)
    {
      ScrobbleThresholdSeconds = normalized;
      return;
    }

    OnPropertyChanged(nameof(CurrentTrackLength));
    OnPropertyChanged(nameof(CurrentTrackLengthToScrobble));
  }
}
