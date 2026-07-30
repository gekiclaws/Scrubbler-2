namespace Scrubbler.Plugin.Scrobblers.CloneHeroScrobbler;

internal interface ICloneHeroSongSource
{
  CloneHeroSong? GetCurrentSong(string dataFolderPath);
}

