using Scrubbler.PluginBase.Settings;

namespace Scrubbler.Plugin.Scrobblers.CloneHeroScrobbler;

internal sealed class PluginSettings : IPluginSettings
{
  public bool AutoConnect { get; set; }

  public bool EnableDiscordRichPresence { get; set; }

  public string DataFolderPath { get; set; } = CloneHeroFileSongSource.GetDefaultDataFolderPath();

  public int ScrobbleThresholdSeconds { get; set; } = PluginDefaults.DefaultScrobbleThresholdSeconds;
}
