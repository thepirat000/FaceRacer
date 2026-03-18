using System.Web;

namespace FaceRacerLive;

public partial class BigLandscapeTextPage : ContentPage, IQueryAttributable
{
    private const double ManualStep = 4;
    private const double ManualMin = 16;
    private const double ManualMax = 500;

    private bool _manualFontEnabled;
    private double _manualFontSize;

    private const double MinFontSize = 24;
    private const double MaxFontSize = 300;
    private const double Step = 4;

    private const string PrefFontFamily = "BigLandscapeTextPage.FontFamily";
    private const string PrefTextColor = "BigLandscapeTextPage.TextColor";

    private bool _restoringPreferences;

    private string _text = "";

    private bool _isFontRepeatActive;
    private int _fontRepeatDirection; // +1 = plus, -1 = minus

    private bool _followBestTime;
    private Services.RaceMonitorState? _raceMonitorState;

    public BigLandscapeTextPage()
    {
        InitializeComponent();

        BigTextLabel.Text = _text;

        RestorePreferences();

        DeviceDisplay.MainDisplayInfoChanged += OnMainDisplayInfoChanged;

        SizeChanged += (_, _) =>
        {
            UpdateLandscapePresentation();
            if (!_manualFontEnabled)
            {
                AutoFit();
            }
        };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        TryStartFollowingBestTime();
        UpdateLandscapePresentation();
        if (!_manualFontEnabled)
        {
            AutoFit();
        }
    }

    protected override void OnDisappearing()
    {
        StopFollowingBestTime();
        DeviceDisplay.MainDisplayInfoChanged -= OnMainDisplayInfoChanged;

        StopFontRepeat();

        TextColorPicker?.Unfocus();
        FontFamilyPicker?.Unfocus();
        BigTextLabel?.Focus();
        base.OnDisappearing();
    }

    private void TryStartFollowingBestTime()
    {
        if (!_followBestTime)
        {
            return;
        }

        _raceMonitorState ??= Application.Current?.Handler?.MauiContext?.Services.GetService<Services.RaceMonitorState>();
        if (_raceMonitorState?.Tracked is not null)
        {
            _raceMonitorState.Tracked.PropertyChanged -= OnTrackedPropertyChanged;
            _raceMonitorState.Tracked.PropertyChanged += OnTrackedPropertyChanged;
            ApplyTrackedBestTimeToLabel();
        }
    }

    private void StopFollowingBestTime()
    {
        if (_raceMonitorState?.Tracked is not null)
        {
            _raceMonitorState.Tracked.PropertyChanged -= OnTrackedPropertyChanged;
        }
    }

    private void OnTrackedPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (string.Equals(e.PropertyName, nameof(ViewModels.TrackedRacerViewModel.BestTime), StringComparison.Ordinal))
        {
            ApplyTrackedBestTimeToLabel();
        }
    }

    private void ApplyTrackedBestTimeToLabel()
    {
        var raw = _raceMonitorState?.Tracked?.BestTime;
        if (string.IsNullOrWhiteSpace(raw) || string.Equals(raw, "-", StringComparison.Ordinal))
        {
            return;
        }

        var formatted = FormatTimeToOneDecimal(raw);
        if (formatted is null)
        {
            return;
        }

        MainThread.BeginInvokeOnMainThread(() =>
        {
            BigTextLabel.Text = formatted;
            _text = formatted;
            if (!_manualFontEnabled)
            {
                AutoFit();
            }
        });
    }

    private static string? FormatTimeToOneDecimal(string raw)
    {
        var trimmed = raw.Trim();
        if (double.TryParse(trimmed, System.Globalization.CultureInfo.InvariantCulture, out var seconds))
        {
            var truncated = AppSettings.TruncateForBigTextTime(seconds);
            return truncated.ToString(AppSettings.BigTextTimeFormat, System.Globalization.CultureInfo.InvariantCulture);
        }

        return null;
    }

    private void OnMainDisplayInfoChanged(object? sender, DisplayInfoChangedEventArgs e)
    {
        UpdateLandscapePresentation();
        if (!_manualFontEnabled)
        {
            AutoFit();
        }
    }

    private void UpdateLandscapePresentation()
    {
        // Force the label itself to be visually "landscape".
        // When the phone is portrait we rotate the label 90 degrees; when already landscape we do not rotate.
        var rotation = DeviceDisplay.MainDisplayInfo.Orientation == DisplayOrientation.Portrait ? 90 : 0;
        if (Math.Abs(BigTextLabel.Rotation - rotation) > 0.1)
        {
            BigTextLabel.Rotation = rotation;
            BigTextLabel.InvalidateMeasure();
        }
    }

    private void RestorePreferences()
    {
        _restoringPreferences = true;

        var savedFont = Preferences.Default.Get(PrefFontFamily, BigTextLabel.FontFamily ?? "Sans");
        var savedColor = Preferences.Default.Get(PrefTextColor, "White");

        SetPickerByString(FontFamilyPicker, savedFont);
        SetPickerByString(TextColorPicker, savedColor);

        BigTextLabel.FontFamily = savedFont;
        ApplyColorToLabel(savedColor);

        _restoringPreferences = false;
    }

    private static void SetPickerByString(Picker? picker, string value)
    {
        if (picker is null)
        {
            return;
        }

        for (var i = 0; i < picker.Items.Count; i++)
        {
            if (string.Equals(picker.Items[i], value, StringComparison.OrdinalIgnoreCase))
            {
                picker.SelectedIndex = i;
                return;
            }
        }
    }

    public void OnTextColorChanged(object? sender, EventArgs e)
    {
        var name = TextColorPicker.SelectedItem as string;
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        ApplyColorToLabel(name);

        if (!_restoringPreferences)
        {
            Preferences.Default.Set(PrefTextColor, name);
        }

        TextColorPicker.Unfocus();
    }

    private void ApplyColorToLabel(string name)
    {
        BigTextLabel.TextColor = name switch
        {
            "White" => Colors.White,
            "Yellow" => Colors.Yellow,
            "Green" => Colors.Lime,
            "Cyan" => Colors.Cyan,
            "Red" => Colors.Red,
            _ => Colors.White
        };
    }

    public void OnFontFamilyChanged(object? sender, EventArgs e)
    {
        var family = FontFamilyPicker.SelectedItem as string;
        if (string.IsNullOrWhiteSpace(family))
        {
            return;
        }

        BigTextLabel.FontFamily = family;

        if (!_restoringPreferences)
        {
            Preferences.Default.Set(PrefFontFamily, family);
        }

        // Reset manual zoom so font size goes back to auto-fit behavior.
        _manualFontEnabled = false;

        AutoFit();
        
        _manualFontSize = BigTextLabel.FontSize;

        FontFamilyPicker.Unfocus();
    }

    private void ApplyManualFontSize(double newSize)
    {
        _manualFontEnabled = true;
        _manualFontSize = Math.Clamp(newSize, ManualMin, ManualMax);

        BigTextLabel.FontSize = _manualFontSize;
        BigTextLabel.InvalidateMeasure();
    }

    public void OnFontPlusClicked(object? sender, EventArgs e)
    {
        var current = _manualFontEnabled ? _manualFontSize : BigTextLabel.FontSize;
        ApplyManualFontSize(current + ManualStep);
    }

    public void OnFontMinusClicked(object? sender, EventArgs e)
    {
        var current = _manualFontEnabled ? _manualFontSize : BigTextLabel.FontSize;
        ApplyManualFontSize(current - ManualStep);
    }

    public void OnFontPlusPressed(object? sender, EventArgs e)
    {
        StartFontRepeat(+1);
    }

    public void OnFontPlusReleased(object? sender, EventArgs e)
    {
        StopFontRepeat();
    }

    public void OnFontMinusPressed(object? sender, EventArgs e)
    {
        StartFontRepeat(-1);
    }

    public void OnFontMinusReleased(object? sender, EventArgs e)
    {
        StopFontRepeat();
    }

    private void StartFontRepeat(int direction)
    {
        _fontRepeatDirection = direction;
        _isFontRepeatActive = true;

        // Apply once immediately (so it feels responsive).
        ApplyFontRepeatTick();

        // Then keep repeating while pressed.
        Dispatcher.StartTimer(TimeSpan.FromMilliseconds(75), () =>
        {
            if (!_isFontRepeatActive)
            {
                return false; // stop timer
            }

            ApplyFontRepeatTick();
            return true; // keep running
        });
    }

    private void StopFontRepeat()
    {
        _isFontRepeatActive = false;
    }

    private void ApplyFontRepeatTick()
    {
        var current = _manualFontEnabled ? _manualFontSize : BigTextLabel.FontSize;
        ApplyManualFontSize(current + (ManualStep * _fontRepeatDirection));
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("followBestTime", out var followValue))
        {
            _followBestTime = IsTruthyQueryValue(followValue);
        }

        if (_followBestTime)
        {
            TryStartFollowingBestTime();
        }

        if (query.TryGetValue("text", out var value) && value is string s && !string.IsNullOrWhiteSpace(s))
        {
            _text = HttpUtility.UrlDecode(s);
            BigTextLabel.Text = _text;
            AutoFit();
        }
    }

    private static bool IsTruthyQueryValue(object value)
    {
        if (value is bool b)
        {
            return b;
        }

        if (value is string s)
        {
            return string.Equals(s, "1", StringComparison.Ordinal) ||
                   string.Equals(s, "true", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(s, "yes", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(s, "on", StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    private void AutoFit()
    {
        if (Width <= 0 || Height <= 0)
        {
            return;
        }

        var horizontalChrome = 25;

        var isRotated = Math.Abs(BigTextLabel.Rotation % 180) > 0.1;
        var maxLabelWidthUnrotated = Math.Max(0, (isRotated ? Height : Width) - horizontalChrome);
        var maxLabelHeightUnrotated = Math.Max(0, isRotated ? Width : Height);

        if (maxLabelWidthUnrotated <= 0 || maxLabelHeightUnrotated <= 0)
        {
            return;
        }

        var best = MinFontSize;

        for (var size = MinFontSize; size <= MaxFontSize; size += Step)
        {
            BigTextLabel.FontSize = size;
            BigTextLabel.InvalidateMeasure();

            var measure = BigTextLabel.Measure(double.PositiveInfinity, double.PositiveInfinity);

            if (measure.Width <= maxLabelWidthUnrotated && measure.Height <= maxLabelHeightUnrotated)
            {
                best = size;
                continue;
            }

            break;
        }

        BigTextLabel.FontSize = best;
        BigTextLabel.Margin = -1 * maxLabelWidthUnrotated;
    }
}