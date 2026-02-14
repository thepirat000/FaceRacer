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

    public BigLandscapeTextPage()
    {
        InitializeComponent();

        BigTextLabel.Text = _text;

        RestorePreferences();

        SizeChanged += (_, _) =>
        {
            if (!_manualFontEnabled)
            {
                AutoFit();
            }
        };
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

    protected override void OnDisappearing()
    {
        StopFontRepeat();

        TextColorPicker?.Unfocus();
        FontFamilyPicker?.Unfocus();
        BigTextLabel?.Focus();
        base.OnDisappearing();
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
        if (query.TryGetValue("text", out var value) && value is string s && !string.IsNullOrWhiteSpace(s))
        {
            _text = HttpUtility.UrlDecode(s);
            BigTextLabel.Text = _text;
            AutoFit();
        }
    }

    private void AutoFit()
    {
        if (Width <= 0 || Height <= 0)
        {
            return;
        }

        var horizontalChrome = 25;

        var maxLabelWidthUnrotated = Math.Max(0, Height - horizontalChrome);
        var maxLabelHeightUnrotated = Math.Max(0, Width);

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