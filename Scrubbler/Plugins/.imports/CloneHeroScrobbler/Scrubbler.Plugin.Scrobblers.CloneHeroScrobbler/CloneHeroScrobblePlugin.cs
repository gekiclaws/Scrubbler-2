using MediaPlayerScrobblerBase;
using Scrubbler.PluginBase;
using Scrubbler.PluginBase.Discord;
using Scrubbler.PluginBase.Plugin;
using Scrubbler.PluginBase.Plugin.Account;
using Scrubbler.PluginBase.Services;
using Scrubbler.PluginBase.Settings;
using Shoegaze.LastFM;

namespace Scrubbler.Plugin.Scrobblers.CloneHeroScrobbler;

[PluginMetadata(
  Name = "Clone Hero Scrobbler",
  Description = "Automatically scrobble songs played in Clone Hero",
  SupportedPlatforms = PlatformSupport.All)]
public sealed class CloneHeroScrobblePlugin : PluginBase.Plugin.PluginBase, IAutoScrobblePlugin, IPersistentPlugin, IAcceptAccountFunctions
{
  private readonly JsonSettingsStore _settingsStore;
  private readonly CloneHeroScrobbleViewModel _vm;
  private PluginSettings _settings = new();

  public CloneHeroScrobblePlugin(
    IModuleLogServiceFactory logFactory,
    IDiscordRichPresence discordRichPresence,
    IFilePickerService filePickerService)
    : base(logFactory)
  {
    var pluginDir = Path.GetDirectoryName(GetType().Assembly.Location)!;
    var apiKeyStorage = new ApiKeyStorage(PluginDefaults.ApiKey, PluginDefaults.ApiSecret, Path.Combine(pluginDir, "environment.env"));
    var settingsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Scrubbler", "Plugins", Name);
    Directory.CreateDirectory(settingsDir);

    _settingsStore = new JsonSettingsStore(Path.Combine(settingsDir, "settings.json"));
    _vm = new CloneHeroScrobbleViewModel(
      new LastfmClient(apiKeyStorage.ApiKey, apiKeyStorage.ApiSecret),
      _logService,
      new CloneHeroFileSongSource(),
      new TimerTickSource(1000),
      filePickerService,
      discordRichPresence);
  }

  public override IPluginViewModel GetViewModel()
  {
    return _vm;
  }

  public async Task LoadAsync()
  {
    _logService.Debug("Loading settings...");

    _settings = await _settingsStore.GetOrCreateAsync<PluginSettings>(Name);
    _vm.DataFolderPath = string.IsNullOrWhiteSpace(_settings.DataFolderPath)
      ? CloneHeroFileSongSource.GetDefaultDataFolderPath()
      : _settings.DataFolderPath;
    _vm.ScrobbleThresholdSeconds = _settings.ScrobbleThresholdSeconds;
    _vm.SetInitialDiscordRichPresenceState(_settings.EnableDiscordRichPresence);
    _vm.SetInitialAutoConnectState(_settings.AutoConnect);
  }

  public async Task SaveAsync()
  {
    _logService.Debug("Saving settings...");

    _settings.AutoConnect = _vm.AutoConnect;
    _settings.EnableDiscordRichPresence = _vm.EnableDiscordRichPresence;
    _settings.DataFolderPath = _vm.DataFolderPath;
    _settings.ScrobbleThresholdSeconds = _vm.NormalizedScrobbleThresholdSeconds;
    await _settingsStore.SetAsync(Name, _settings);
  }

  public void SetAccountFunctionsContainer(AccountFunctionContainer container)
  {
    _vm.FunctionContainer = container;
    _vm.UpdateNowPlayingObject = container.UpdateNowPlayingObject;
  }
}
