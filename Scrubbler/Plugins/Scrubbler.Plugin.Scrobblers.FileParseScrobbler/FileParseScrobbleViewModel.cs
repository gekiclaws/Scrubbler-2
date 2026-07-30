using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Scrubbler.Plugin.Scrobbler.FileParseScrobbler.Parser.CSV;
using Scrubbler.Plugin.Scrobblers.FileParseScrobbler;
using Scrubbler.Plugin.Scrobblers.FileParseScrobbler.Parser;
using Scrubbler.Plugin.Scrobblers.FileParseScrobbler.Parser.CSV;
using Scrubbler.Plugin.Scrobblers.FileParseScrobbler.Parser.JSON;
using Scrubbler.PluginBase;
using Scrubbler.PluginBase.Plugin;
using Scrubbler.PluginBase.Services;

namespace Scrubbler.Plugin.Scrobbler.FileParseScrobbler;

internal enum ScrobbleMode
{
    Import,
    UseScrobbleTimestamp
}

internal sealed partial class FileParseScrobbleViewModel : ScrobbleMultipleTimeViewModelBase<ParsedScrobbleViewModel>
{
    #region Properties

    [ObservableProperty]
    private IFileParserViewModel _selectedParser;

    [ObservableProperty]
    private IEnumerable<IFileParserViewModel> _availableParsers;

    public ScrobbleMode[] AvailableScrobbleModes { get; } = Enum.GetValues<ScrobbleMode>();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsImportMode))]
    private ScrobbleMode _selectedScrobbleMode = ScrobbleMode.Import;

    public bool IsImportMode => SelectedScrobbleMode == ScrobbleMode.Import;

    public double ScrobbleGapSeconds
    {
        get => _scrobbleGapSeconds;
        set => SetProperty(ref _scrobbleGapSeconds, NormalizeScrobbleGapSeconds(value));
    }
    private double _scrobbleGapSeconds;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ParseCommand))]
    private string _selectedFilePath = string.Empty;

    private bool CanParse => File.Exists(SelectedFilePath);

    private readonly ILogService _logService;
    private readonly IDialogService _dialogService;
    private readonly IFilePickerService _filePicker;
    private readonly IFileStorageService _fileStorageService;
    private static readonly string[] _textFiles = [".txt"];
    private const double MinimumScrobbleGapSeconds = 1;
    private const double MaximumScrobbleGapSeconds = 86400;

    #endregion Properties

    #region Construction

    public FileParseScrobbleViewModel(ILogService logService, IDialogService dialogService, IFilePickerService filePicker, IFileStorageService fileStorageService,
                                      IFileParser<CsvFileParserConfiguration> csvParser, CsvFileParserConfiguration csvConfig,
                                      IFileParser<JsonFileParserConfiguration> jsonParser, JsonFileParserConfiguration jsonConfig,
                                      double scrobbleGapSeconds = PluginSettings.DefaultScrobbleGapSeconds)
    {
        _logService = logService;
        _dialogService = dialogService;
        _filePicker = filePicker;
        _fileStorageService = fileStorageService;
        _scrobbleGapSeconds = NormalizeScrobbleGapSeconds(scrobbleGapSeconds);

        var parsers = new List<IFileParserViewModel>
        {
            new CsvFileParserViewModel(dialogService, csvParser, csvConfig),
            new JsonFileParserViewModel(dialogService, jsonParser, jsonConfig)
        };

        AvailableParsers = parsers;
        SelectedParser = parsers[0];
    }

    #endregion Construction

    public override async Task<IEnumerable<ScrobbleData>> GetScrobblesAsync()
    {
        return await Task.Run(() =>
        {
            var scrobbles = Scrobbles.Where(s => s.ToScrobble);
            if (SelectedScrobbleMode == ScrobbleMode.Import)
                return CreateImportScrobbles(scrobbles, ScrobbleTimeVM.Timestamp, ScrobbleGapSeconds);
            else
                return scrobbles.Select(s => new ScrobbleData(s.TrackName, s.ArtistName, s.Timestamp) { Album = s.AlbumName, AlbumArtist = s.AlbumArtistName });
        });
    }

    internal static IEnumerable<ScrobbleData> CreateImportScrobbles(
        IEnumerable<ParsedScrobbleViewModel> scrobbles,
        DateTimeOffset masterTimestamp,
        double scrobbleGapSeconds)
    {
        return ScrobbleData.FromMasterTimestamp(
            scrobbles,
            masterTimestamp,
            reverse: false,
            secondsToSubtract: GetScrobbleGapSeconds(scrobbleGapSeconds));
    }

    internal static int GetScrobbleGapSeconds(double scrobbleGapSeconds)
    {
        return (int)Math.Round(NormalizeScrobbleGapSeconds(scrobbleGapSeconds), MidpointRounding.AwayFromZero);
    }

    private static double NormalizeScrobbleGapSeconds(double scrobbleGapSeconds)
    {
        if (!double.IsFinite(scrobbleGapSeconds))
            return PluginSettings.DefaultScrobbleGapSeconds;

        return Math.Clamp(scrobbleGapSeconds, MinimumScrobbleGapSeconds, MaximumScrobbleGapSeconds);
    }

    [RelayCommand]
    private async Task OpenFile()
    {
        var file = await _filePicker.PickFileAsync(SelectedParser.SupportedExtensions);
        if (file == null)
            return;

        SelectedFilePath = file.Path;
    }

    [RelayCommand(CanExecute = nameof(CanParse))]
    private async Task Parse()
    {
        IsBusy = true;
        try
        {
            FileParseResult result = null!;
            await Task.Run(() =>
            {
                result = SelectedParser.Parse(SelectedFilePath, SelectedScrobbleMode);
            });

            if (result.Errors.Any())
            {
                var errorCount = result.Errors.Count();
                var dialog = new ContentDialog
                {
                    Title = "Parsing Errors",
                    Content = $"Parsing completed with {errorCount} " +
                              $"error{(errorCount == 1 ? "" : "s")}. " +
                              "Do you want to save a file with the error details?",
                    PrimaryButtonText = "Yes",
                    SecondaryButtonText = "No",
                    DefaultButton = ContentDialogButton.Primary
                };

                var res = await _dialogService.ShowDialogAsync(dialog);
                if (res == ContentDialogResult.Primary)
                {
                    var file = await _filePicker.SaveFileAsync(
                                                "parse_errors",
                                                new Dictionary<string, IReadOnlyList<string>>
                                                {
                                                    { "Text file", _textFiles }
                                                });
                    if (file != null)
                        await _fileStorageService.WriteLinesAsync(file, result.Errors);
                }
            }

            Scrobbles = new ObservableCollection<ParsedScrobbleViewModel>(result.Scrobbles.Select(s => new ParsedScrobbleViewModel(s)));
        }
        catch (Exception ex)
        {
            _logService.Error("An error occurred while parsing the selected file.", ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnSelectedScrobbleModeChanged(ScrobbleMode value)
    {
        if (Scrobbles.Any())
            Scrobbles = [];
    }
}
