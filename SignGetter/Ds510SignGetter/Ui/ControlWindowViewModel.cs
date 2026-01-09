using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using Ds510SignGetter.Manger;
using Ds510SignGetter.Utilities;

namespace Ds510SignGetter.Ui;

public enum Languages
{
    English = 0,
    Russian = 1,
}

public partial class ControlWindowViewModel : ViewModelBase
{
    public ControlWindowViewModel(ControlWindow window)
    {
        _acceptBtnLabel = SubmitLabel;
        _window = window;
    }
    
    #region Languages
    private Languages _selectedLanguage = Languages.English;

    public int SelectedLanguageIndex
    {
        get => (int)_selectedLanguage;
        set
        {
            if (value != SelectedLanguageIndex)
            {
                _selectedLanguage = (Languages)value;
                OnPropertyChanged(nameof(SelectedLanguageIndex));
                UpdateLanguages();
            }
        } 
    }

    public string ChooseLanguageLabel => _translationLabels[0 + (int)_selectedLanguage];
    public string TabletSettingsLabel => _translationLabels[2 + (int)_selectedLanguage];
    public string TurnScreenOnLabel => _translationLabels[4 + (int)_selectedLanguage];
    public string TurnScreenOffLabel => _translationLabels[6 + (int)_selectedLanguage];
    public string RebootLabel => _translationLabels[8 + (int)_selectedLanguage];
    public string SubmitLabel => _translationLabels[10 + (int)_selectedLanguage];
    public string CancelLabel => _translationLabels[12 + (int)_selectedLanguage];
    public string ReSignLabel => _translationLabels[14 + (int)_selectedLanguage];
    public string SaveLabel => _translationLabels[16 + (int)_selectedLanguage];
    public string WaitLabel => _translationLabels[18 + (int)_selectedLanguage];
    
    private void UpdateLanguages()
    {
        OnPropertyChanged(nameof(ChooseLanguageLabel));
        OnPropertyChanged(nameof(TabletSettingsLabel));
        OnPropertyChanged(nameof(TurnScreenOffLabel));
        OnPropertyChanged(nameof(TurnScreenOnLabel));
        OnPropertyChanged(nameof(RebootLabel));
        OnPropertyChanged(nameof(SubmitLabel));
        OnPropertyChanged(nameof(SaveLabel));
        OnPropertyChanged(nameof(WaitLabel));
        OnPropertyChanged(nameof(CancelLabel));
        OnPropertyChanged(nameof(ReSignLabel));
        UpdateAcceptBtnLanguage();
    }

    private void UpdateAcceptBtnLanguage()
    {
        _acceptBtnLabel = _acceptBtnMode switch
        {
            2 => WaitLabel,
            1 => SaveLabel,
            _ => SubmitLabel
        };

        OnPropertyChanged(nameof(AcceptBtnLabel));
    }

    private readonly ObservableCollection<string> _translationLabels = new()
    {
        "UI Language", "Язык Интерфейса", // Choose language label
        "Tablet Settings", "Настройки планшета", // Tablet settings label
        "Screen On", "Экран Вкл.", // Turn On (screen)
        "Screen Off", "Экран Выкл.", // Turn Off (screen)
        "Reboot", "Перезагрузить", // Reboot
        "Submit", "Применить", // Submit
        "Cancel", "Отменить", // Cancel
        "ReSign", "Повторная подпись", // ReSign
        "Save", "Сохранить", // Save
        "Wait...", "Подождите..." //Wait
    };

    public ObservableCollection<string> LanguagesLabel { get; } = new()
    {
        "English",
        "Русский"
    };
    #endregion

    private readonly ControlWindow _window;
    private string _acceptBtnLabel;
    private bool _acceptBtnIsEnabled = true;
    public string AcceptBtnLabel
    {
        get => _acceptBtnLabel;
        set
        {
            if (value != _acceptBtnLabel)
            {
                _acceptBtnLabel = value;
                OnPropertyChanged(nameof(AcceptBtnLabel));
            }
        }
    }

    public bool AcceptBtnIsEnabled
    {
        get => _acceptBtnIsEnabled;
        set
        {
            if (value != _acceptBtnIsEnabled)
            {
                _acceptBtnIsEnabled = value;
                OnPropertyChanged(nameof(AcceptBtnIsEnabled));
            }
        }
    }

    #region Commands
    private int _acceptBtnMode = 0; // 0 - basic; 1 - to save; 2 - wait

    [RelayCommand]
    private void ReSing()
    {
        GetterManager.ReSing();
        if (_acceptBtnMode == 1)
        {
            _window.HideImage();
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        if (MessageService.AskYesNoMessage("Do you want to exit without saving?"))
        {
            GetterManager.CancelOperation();
            _window.HideImage();
        }
    }

    [RelayCommand]
    private void Submit()
    {
        if (_acceptBtnMode == 1)
        {
            if (GetterManager.SaveResult()) AcceptBtnModeChange(0);
        }
        else if (_acceptBtnMode == 0)
        {
            if (GetterManager.GetImage()) AcceptBtnModeChange(2);
        }
    }

    public void AcceptBtnModeChange(int mode) // 0 - basic; 1 - save mode; 2- wait
    {
        switch (mode)
        {
            case 0:
                AcceptBtnLabel = SubmitLabel;
                _acceptBtnMode = 0;
                AcceptBtnIsEnabled = true;
                break;
            case 1:
                AcceptBtnLabel = SaveLabel;
                _acceptBtnMode = 1;
                AcceptBtnIsEnabled = true;
                break;
            case 2:
                AcceptBtnLabel = WaitLabel;
                _acceptBtnMode = 2;
                AcceptBtnIsEnabled = false;
                break;
            default:
                Console.WriteLine("[SignGetter > UI] Invalid AcceptBtnMode: {0}", mode);
                break;
        }
    }
    
    [RelayCommand]
    private void TurnScreenOn()
    {
        GetterManager.TurnScreenOn();
    }

    [RelayCommand]
    private void TurnScreenOff()
    {
        GetterManager.TurnScreenOff();
    }

    [RelayCommand]
    private void Reboot()
    {
        switch (MessageService.AskYesNoCancelMessage("Do you want to make full (\"yes\") or partial (\"no\") reboot?"))
        {
            case true:
                GetterManager.RestartGetter();
                break;
            case false:
                GetterManager.RebootTablet();
                break;
        }
    }
    #endregion
}