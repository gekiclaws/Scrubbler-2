using MediaPlayerScrobblerBase;
using Scrubbler.Plugin.Scrobbler.AppleMusicScrobbler;
using Scrubbler.PluginBase;
using Scrubbler.PluginBase.Discord;
using Scrubbler.PluginBase.Plugin;
using Scrubbler.PluginBase.Plugin.Account;
using Scrubbler.PluginBase.Services;
using Scrubbler.PluginBase.Settings;
using Shoegaze.LastFM;

namespace Scrubbler.Plugin.Scrobblers.AppleMusicScrobbler;

[PluginMetadata(
    Name = "Apple Music Scrobbler",
    Description = "Automatically scrobble tracks playing in the Apple Music desktop app",
    SupportedPlatforms = PlatformSupport.Windows)]
public class AppleMusicScrobblePlugin : PluginBase.Plugin.PluginBase, IAutoScrobblePlugin, IPersistentPlugin, IAcceptAccountFunctions
{
  #region Properties

  private readonly ApiKeyStorage _apiKeyStorage;
  private readonly AppleMusicScrobbleViewModel _vm;
  private readonly JsonSettingsStore _settingsStore;
  private PluginSettings _settings = new();

  #endregion Properties

  /// <summary>
  /// Initializes a new instance of the <see cref="ManualScrobblePlugin"/> class.
  /// </summary>
  public AppleMusicScrobblePlugin(IModuleLogServiceFactory logFactory, IDiscordRichPresence discordRichPresence)
      : base(logFactory)
  {
    var pluginDir = Path.GetDirectoryName(GetType().Assembly.Location)!;
    _apiKeyStorage = new ApiKeyStorage(PluginDefaults.ApiKey, PluginDefaults.ApiSecret, Path.Combine(pluginDir, "environment.env"));
    var settingsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Scrubbler", "Plugins", Name);
    Directory.CreateDirectory(settingsDir);
    _settingsStore = new JsonSettingsStore(Path.Combine(settingsDir, "settings.json"));
    _vm = new AppleMusicScrobbleViewModel(new LastfmClient(_apiKeyStorage.ApiKey, _apiKeyStorage.ApiSecret), _logService,
                                          new FlaUiAppleMusicAutomation(), new TimerTickSource(1000), new TimerTickSource(1000),
                                          discordRichPresence);
  }

  /// <summary>
  /// Gets the view model instance for this plugin's UI.
  /// </summary>
  /// <returns>The <see cref="IPluginViewModel"/> instance for this plugin.</returns>
  public override IPluginViewModel GetViewModel()
  {
    return _vm;
  }

  public async Task LoadAsync()
  {
    _logService.Debug("Loading settings...");

    _settings = await _settingsStore.GetOrCreateAsync<PluginSettings>(Name);
    _vm.SetInitialAutoConnectState(_settings.AutoConnect);
    _vm.SetInitialDiscordRichPresenceState(_settings.EnableDiscordRichPresence);
  }

  public async Task SaveAsync()
  {
    _logService.Debug("Saving settings...");

    _settings.AutoConnect = _vm.AutoConnect;
    _settings.EnableDiscordRichPresence = _vm.EnableDiscordRichPresence;
    await _settingsStore.SetAsync(Name, _settings);
  }

  public void SetAccountFunctionsContainer(AccountFunctionContainer container)
  {
    _vm.FunctionContainer = container;
  }
}
