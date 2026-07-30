using Moq;
using Scrubbler.Plugin.Scrobblers.CloneHeroScrobbler;
using Scrubbler.PluginBase;
using Scrubbler.PluginBase.Discord;
using Scrubbler.PluginBase.Services;
using Shoegaze.LastFM;

namespace Scrubbler.Test.CloneHeroScrobblerTest;

[TestFixture]
internal sealed class CloneHeroScrobbleViewModelTests
{
  private static readonly string ExistingFolder = AppContext.BaseDirectory;

  private Mock<ILastfmClient> _lastfmClient = null!;
  private Mock<ILogService> _logger = null!;
  private Mock<IDiscordRichPresence> _discordRichPresence = null!;
  private Mock<IFilePickerService> _filePicker = null!;
  private FakeCloneHeroSongSource _songSource = null!;
  private ManualTickSource _ticks = null!;
  private CloneHeroScrobbleViewModel _vm = null!;

  [SetUp]
  public void Setup()
  {
    _lastfmClient = new Mock<ILastfmClient>();
    _logger = new Mock<ILogService>();
    _discordRichPresence = new Mock<IDiscordRichPresence>();
    _filePicker = new Mock<IFilePickerService>();
    _songSource = new FakeCloneHeroSongSource();
    _ticks = new ManualTickSource();

    _vm = new CloneHeroScrobbleViewModel(
      _lastfmClient.Object,
      _logger.Object,
      _songSource,
      _ticks,
      _filePicker.Object,
      _discordRichPresence.Object)
    {
      DataFolderPath = ExistingFolder
    };
  }

  [TearDown]
  public void TearDown()
  {
    _ticks.Dispose();
  }

  [Test]
  public async Task Connect_reads_current_song()
  {
    _songSource.CurrentSong = new CloneHeroSong("Artist", "Track", string.Empty);

    await _vm.ToggleConnectionCommand.ExecuteAsync(null);

    using (Assert.EnterMultipleScope())
    {
      Assert.That(_vm.IsConnected, Is.True);
      Assert.That(_vm.CurrentTrackName, Is.EqualTo("Track"));
      Assert.That(_vm.CurrentArtistName, Is.EqualTo("Artist"));
      Assert.That(_songSource.LastDataFolderPath, Is.EqualTo(ExistingFolder));
    }
  }

  [Test]
  public async Task Stable_song_scrobbles_after_threshold()
  {
    var scrobbles = new List<ScrobbleData>();
    _songSource.CurrentSong = new CloneHeroSong("Artist", "Track", string.Empty);
    _vm.ScrobblesDetected += (_, data) => scrobbles.AddRange(data);

    await _vm.ToggleConnectionCommand.ExecuteAsync(null);

    for (var i = 0; i < PluginDefaults.DefaultScrobbleThresholdSeconds; i++)
      _ticks.Fire();

    using (Assert.EnterMultipleScope())
    {
      Assert.That(scrobbles, Has.Count.EqualTo(1));
      Assert.That(scrobbles[0].Track, Is.EqualTo("Track"));
      Assert.That(scrobbles[0].Artist, Is.EqualTo("Artist"));
      Assert.That(_vm.CurrentTrackScrobbled, Is.True);
      Assert.That(_vm.CountedSeconds, Is.EqualTo(_vm.CurrentTrackLengthToScrobble));
    }
  }

  [Test]
  public async Task Stable_song_scrobbles_only_once()
  {
    var scrobbles = new List<ScrobbleData>();
    _songSource.CurrentSong = new CloneHeroSong("Artist", "Track", string.Empty);
    _vm.ScrobblesDetected += (_, data) => scrobbles.AddRange(data);

    await _vm.ToggleConnectionCommand.ExecuteAsync(null);

    for (var i = 0; i < PluginDefaults.DefaultScrobbleThresholdSeconds + 10; i++)
      _ticks.Fire();

    Assert.That(scrobbles, Has.Count.EqualTo(1));
  }

  [Test]
  public async Task Song_change_resets_counter()
  {
    _songSource.CurrentSong = new CloneHeroSong("Artist", "Track", string.Empty);

    await _vm.ToggleConnectionCommand.ExecuteAsync(null);

    for (var i = 0; i < 10; i++)
      _ticks.Fire();

    _songSource.CurrentSong = new CloneHeroSong("Artist", "Other Track", string.Empty);
    _ticks.Fire();

    using (Assert.EnterMultipleScope())
    {
      Assert.That(_vm.CurrentTrackName, Is.EqualTo("Other Track"));
      Assert.That(_vm.CountedSeconds, Is.Zero);
      Assert.That(_vm.CurrentTrackScrobbled, Is.False);
    }
  }

  [Test]
  public async Task Missing_song_clears_current_state()
  {
    _songSource.CurrentSong = new CloneHeroSong("Artist", "Track", string.Empty);

    await _vm.ToggleConnectionCommand.ExecuteAsync(null);

    _songSource.CurrentSong = null;
    _ticks.Fire();

    using (Assert.EnterMultipleScope())
    {
      Assert.That(_vm.CurrentTrackName, Is.Empty);
      Assert.That(_vm.CurrentArtistName, Is.Empty);
      Assert.That(_vm.CountedSeconds, Is.Zero);
    }
  }

  [Test]
  public void Invalid_threshold_normalizes_to_default()
  {
    _vm.ScrobbleThresholdSeconds = double.NaN;

    using (Assert.EnterMultipleScope())
    {
      Assert.That(_vm.ScrobbleThresholdSeconds, Is.EqualTo(PluginDefaults.DefaultScrobbleThresholdSeconds));
      Assert.That(_vm.CurrentTrackLengthToScrobble, Is.EqualTo(PluginDefaults.DefaultScrobbleThresholdSeconds));
    }
  }
}
