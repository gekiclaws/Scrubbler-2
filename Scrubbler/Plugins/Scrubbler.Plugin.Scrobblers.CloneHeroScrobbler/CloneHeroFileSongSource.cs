using System.Text;
using System.Text.RegularExpressions;

namespace Scrubbler.Plugin.Scrobblers.CloneHeroScrobbler;

internal sealed class CloneHeroFileSongSource : ICloneHeroSongSource
{
  private static readonly Regex CustomSongExportRegex = new(@"^\s*custom_song_export\s*=\s*(.*)$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

  public CloneHeroSong? GetCurrentSong(string dataFolderPath)
  {
    if (string.IsNullOrWhiteSpace(dataFolderPath))
      return null;

    var currentSongPath = Path.Combine(dataFolderPath, PluginDefaults.CurrentSongFileName);
    if (!File.Exists(currentSongPath))
      return null;

    var currentSong = File.ReadAllText(currentSongPath, Encoding.UTF8);
    var customSongExport = ReadCustomSongExport(Path.Combine(dataFolderPath, PluginDefaults.SettingsFileName));

    return CloneHeroSongParser.Parse(currentSong, customSongExport);
  }

  public static string GetDefaultDataFolderPath()
  {
    var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    if (OperatingSystem.IsWindows())
    {
      var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
      return Path.Combine(string.IsNullOrWhiteSpace(documents) ? userProfile : documents, "Clone Hero");
    }

    if (OperatingSystem.IsLinux())
      return Path.Combine(userProfile, ".clonehero");

    return Path.Combine(userProfile, "Clone Hero");
  }

  private static string? ReadCustomSongExport(string settingsPath)
  {
    if (!File.Exists(settingsPath))
      return null;

    foreach (var line in File.ReadLines(settingsPath, Encoding.UTF8))
    {
      var match = CustomSongExportRegex.Match(line);
      if (match.Success)
        return match.Groups[1].Value.Trim();
    }

    return null;
  }
}

