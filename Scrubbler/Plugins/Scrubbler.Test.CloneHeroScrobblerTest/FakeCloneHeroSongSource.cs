using Scrubbler.Plugin.Scrobblers.CloneHeroScrobbler;

namespace Scrubbler.Test.CloneHeroScrobblerTest;

internal sealed class FakeCloneHeroSongSource : ICloneHeroSongSource
{
  public CloneHeroSong? CurrentSong { get; set; }

  public string? LastDataFolderPath { get; private set; }

  public CloneHeroSong? GetCurrentSong(string dataFolderPath)
  {
    LastDataFolderPath = dataFolderPath;
    return CurrentSong;
  }
}

