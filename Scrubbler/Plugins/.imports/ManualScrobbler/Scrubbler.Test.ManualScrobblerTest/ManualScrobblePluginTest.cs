using Scrubbler.Plugin.Scrobblers.ManualScrobbler;

namespace Scrubbler.Test.ManualScrobblerTest;

public class Tests
{
    [Test]
    public async Task CreateScrobbles_UsesSelectedTimestampIncludingSeconds()
    {
        var now = DateTimeOffset.Now;
        var playedAt = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, now.Minute, 37, now.Offset);
        var vm = new ManualScrobbleViewModel
        {
            ArtistName = "Test Artist",
            TrackName = "Test Track",
            AlbumName = "Test Album",
            AlbumArtistName = "Test Album Artist"
        };
        vm.ScrobbleTimeVM.UseCurrentTime = false;
        vm.ScrobbleTimeVM.Date = playedAt.Date;
        vm.ScrobbleTimeVM.Time = playedAt.TimeOfDay;

        var scrobbles = await vm.GetScrobblesAsync();

        Assert.That(scrobbles.Count, Is.EqualTo(1));
        var scrobble = scrobbles.First();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(scrobble.Artist, Is.EqualTo("Test Artist"));
            Assert.That(scrobble.Track, Is.EqualTo("Test Track"));
            Assert.That(scrobble.Album, Is.EqualTo("Test Album"));
            Assert.That(scrobble.AlbumArtist, Is.EqualTo("Test Album Artist"));
            Assert.That(scrobble.Timestamp, Is.EqualTo(playedAt));
        }
    }

    [Test]
    public void InvalidDataTest()
    {
        var vm = new ManualScrobbleViewModel
        {
            ArtistName = "",
            TrackName = "Test Track"
        };
        Assert.ThrowsAsync<InvalidOperationException>(vm.GetScrobblesAsync);
        vm.ArtistName = "Test Artist";
        vm.TrackName = "";
        Assert.ThrowsAsync<InvalidOperationException>(vm.GetScrobblesAsync);
    }

    [Test]
    public void CanScrobbleTest()
    {
        var vm = new ManualScrobbleViewModel();
        Assert.That(vm.CanScrobble, Is.False);
        vm.ArtistName = "Test Artist";
        Assert.That(vm.CanScrobble, Is.False);
        vm.TrackName = "Test Track";
        Assert.That(vm.CanScrobble, Is.True);
        vm.ArtistName = "";
        Assert.That(vm.CanScrobble, Is.False);
    }

    [Test]
    public async Task IsBusyTest()
    {
        var vm = new ManualScrobbleViewModel
        {
            ArtistName = "Test Artist",
            TrackName = "Test Track",
            AlbumName = "Test Album",
            AlbumArtistName = "Test Album Artist"
        };

        Assert.That(vm.IsBusy, Is.False);
        int changes = 0;
        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ManualScrobbleViewModel.IsBusy))
            {
                changes++;
            }
        };

        var scrobbles = await vm.GetScrobblesAsync();
        Assert.That(changes, Is.EqualTo(2));
    }
}
