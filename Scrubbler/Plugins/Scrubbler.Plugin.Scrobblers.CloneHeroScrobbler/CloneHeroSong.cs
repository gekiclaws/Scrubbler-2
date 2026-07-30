namespace Scrubbler.Plugin.Scrobblers.CloneHeroScrobbler;

internal sealed record CloneHeroSong(string Artist, string Track, string Album)
{
  public bool SameScrobbleIdentity(CloneHeroSong other)
  {
    return string.Equals(Artist, other.Artist, StringComparison.OrdinalIgnoreCase) &&
           string.Equals(Track, other.Track, StringComparison.OrdinalIgnoreCase) &&
           string.Equals(Album, other.Album, StringComparison.OrdinalIgnoreCase);
  }
}

