using Scrubbler.Plugin.Scrobblers.CloneHeroScrobbler;

namespace Scrubbler.Test.CloneHeroScrobblerTest;

[TestFixture]
internal sealed class CloneHeroSongParserTests
{
  [Test]
  public void Parse_reads_default_clone_hero_export()
  {
    var song = CloneHeroSongParser.Parse("Track\nArtist\nCharter", null);

    Assert.That(song, Is.EqualTo(new CloneHeroSong("Artist", "Track", string.Empty)));
  }

  [Test]
  public void Parse_reads_album_from_custom_export()
  {
    var song = CloneHeroSongParser.Parse("Track\nArtist\nCharter\nAlbum", "%s%n%a%n%c%n%b");

    Assert.That(song, Is.EqualTo(new CloneHeroSong("Artist", "Track", "Album")));
  }

  [Test]
  public void Parse_supports_custom_field_order()
  {
    var song = CloneHeroSongParser.Parse("Artist | Album | Track", "%a | %b | %s");

    Assert.That(song, Is.EqualTo(new CloneHeroSong("Artist", "Track", "Album")));
  }

  [Test]
  public void Parse_removes_clone_hero_speed_modifier_from_title()
  {
    var song = CloneHeroSongParser.Parse("Track (125%)\nArtist\nCharter", null);

    Assert.That(song?.Track, Is.EqualTo("Track"));
  }

  [Test]
  public void Parse_returns_null_when_export_format_has_no_artist()
  {
    var song = CloneHeroSongParser.Parse("Track\nCharter", "%s%n%c");

    Assert.That(song, Is.Null);
  }
}

