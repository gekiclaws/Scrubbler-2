using System.Text;
using System.Text.RegularExpressions;

namespace Scrubbler.Plugin.Scrobblers.CloneHeroScrobbler;

internal static class CloneHeroSongParser
{
  private const string DefaultExportFormat = "%s%n%a%n%c";
  private static readonly Regex SpeedModifierRegex = new(@"\(\d+%\)", RegexOptions.Compiled | RegexOptions.CultureInvariant);

  public static CloneHeroSong? Parse(string currentSong, string? customSongExport)
  {
    if (string.IsNullOrWhiteSpace(currentSong))
      return null;

    var exportFormat = string.IsNullOrWhiteSpace(customSongExport)
      ? DefaultExportFormat
      : customSongExport.Trim();

    if (!ContainsToken(exportFormat, 's') || !ContainsToken(exportFormat, 'a'))
      return null;

    return exportFormat == DefaultExportFormat
      ? ParseDefault(currentSong)
      : ParseCustom(currentSong, exportFormat);
  }

  private static CloneHeroSong? ParseDefault(string currentSong)
  {
    var lines = currentSong
      .Split(["\r\n", "\n", "\r"], StringSplitOptions.None)
      .Select(line => line.Trim())
      .ToArray();

    if (lines.Length < 2)
      return null;

    return CreateSong(lines[1], lines[0], string.Empty);
  }

  private static CloneHeroSong? ParseCustom(string currentSong, string exportFormat)
  {
    var regex = CreateExportRegex(exportFormat);
    var match = regex.Match(currentSong.TrimEnd('\r', '\n'));

    if (!match.Success)
      return null;

    var artist = GetGroupValue(match, "a");
    var track = GetGroupValue(match, "s");
    var album = ContainsToken(exportFormat, 'b') ? GetGroupValue(match, "b") : string.Empty;

    return CreateSong(artist, track, album);
  }

  private static CloneHeroSong? CreateSong(string artist, string track, string album)
  {
    artist = artist.Trim();
    track = SpeedModifierRegex.Replace(track, string.Empty).Trim();
    album = album.Trim();

    if (string.IsNullOrWhiteSpace(artist) || string.IsNullOrWhiteSpace(track))
      return null;

    return new CloneHeroSong(artist, track, album);
  }

  private static Regex CreateExportRegex(string exportFormat)
  {
    var usedGroups = new HashSet<char>();
    var regex = new StringBuilder(@"\A");

    for (var i = 0; i < exportFormat.Length; i++)
    {
      if (exportFormat[i] == '%' && i + 1 < exportFormat.Length)
      {
        var token = exportFormat[++i];
        if (token == 'n')
        {
          regex.Append(@"(?:\r\n|\n|\r)");
        }
        else if (char.IsLetterOrDigit(token))
        {
          if (usedGroups.Add(token))
            regex.Append("(?<").Append(token).Append(@">.*?)");
          else
            regex.Append(".*?");
        }
        else
        {
          regex.Append(Regex.Escape("%" + token));
        }
      }
      else
      {
        regex.Append(Regex.Escape(exportFormat[i].ToString()));
      }
    }

    regex.Append(@"\z");
    return new Regex(regex.ToString(), RegexOptions.CultureInvariant);
  }

  private static bool ContainsToken(string exportFormat, char token)
  {
    return exportFormat.Contains($"%{token}", StringComparison.Ordinal);
  }

  private static string GetGroupValue(Match match, string groupName)
  {
    var group = match.Groups[groupName];
    return group.Success ? group.Value : string.Empty;
  }
}
