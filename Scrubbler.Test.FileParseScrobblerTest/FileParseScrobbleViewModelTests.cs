using System.Text.Json;
using Scrubbler.Plugin.Scrobbler.FileParseScrobbler;
using Scrubbler.Plugin.Scrobblers.FileParseScrobbler;
using Scrubbler.PluginBase;

namespace Scrubbler.Test.FileParseScrobblerTest;

[TestFixture]
internal sealed class FileParseScrobbleViewModelTests
{
    [Test]
    public void PluginSettings_missingGap_usesThreeSecondDefault()
    {
        var settings = JsonSerializer.Deserialize<PluginSettings>("{}");

        Assert.That(settings, Is.Not.Null);
        Assert.That(settings!.ScrobbleGapSeconds, Is.EqualTo(3));
    }

    [Test]
    public void CreateImportScrobbles_customGap_spacesGeneratedTimestamps()
    {
        var masterTimestamp = new DateTimeOffset(2026, 7, 30, 12, 0, 0, TimeSpan.Zero);
        ParsedScrobbleViewModel[] parsedScrobbles =
        [
            CreateParsedScrobble("Track 1"),
            CreateParsedScrobble("Track 2"),
            CreateParsedScrobble("Track 3")
        ];

        var scrobbles = FileParseScrobbleViewModel
            .CreateImportScrobbles(parsedScrobbles, masterTimestamp, scrobbleGapSeconds: 90)
            .ToArray();

        Assert.That(scrobbles.Select(scrobble => scrobble.Timestamp), Is.EqualTo(new[]
        {
            masterTimestamp,
            masterTimestamp.AddSeconds(-90),
            masterTimestamp.AddSeconds(-180)
        }));
    }

    [TestCase(0, 1)]
    [TestCase(double.NaN, 3)]
    [TestCase(double.PositiveInfinity, 3)]
    [TestCase(100000, 86400)]
    public void GetScrobbleGapSeconds_invalidOrOutOfRangeValue_isNormalized(double seconds, int expectedSeconds)
    {
        Assert.That(FileParseScrobbleViewModel.GetScrobbleGapSeconds(seconds), Is.EqualTo(expectedSeconds));
    }

    private static ParsedScrobbleViewModel CreateParsedScrobble(string track)
    {
        return new ParsedScrobbleViewModel(new ScrobbleData(track, "Artist", DateTimeOffset.Now));
    }
}
