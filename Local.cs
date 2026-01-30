using UnityEngine;
using TMPro;
using Unity.Localization;
using System;

public static class LocalizationManager
{
    public static LocalizedTextHandle Localize(
        string tableReference,
        Func<string> keyProvider,
        TMP_Text textComponent)
    {
        if (textComponent == null)
        {
            Debug.LogWarning("TMP_Text is null!");
            return null;
        }

        var localizedString = new LocalizedString
        {
            TableReference = tableReference
        };

        UpdateText();

        void OnLocaleChanged()
        {
            UpdateText();
        }

        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;

        void UpdateText()
        {
            try
            {
                string key = keyProvider?.Invoke() ?? "";
                localizedString.TableEntryReference = new Unity.Localization.Tables.TableEntryReference(key);
                localizedString.StringChanged += s => textComponent.text = s;
                localizedString.RefreshString();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to localize with key from provider: {ex.Message}");
            }
        }

        return new LocalizedTextHandle(localizedString, OnLocaleChanged, UpdateText, () =>
        {
            localizedString.StringChanged -= s => textComponent.text = s;
            LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
        });
    }
}

public class LocalizedTextHandle
{
    private readonly LocalizedString _localizedString;
    private readonly Action _onLocaleChanged;
    private readonly Action _updateText;
    private readonly Action _onDestroy;
    private bool _isDestroyed = false;

    internal LocalizedTextHandle(LocalizedString localizedString, Action onLocaleChanged, Action updateText, Action onDestroy)
    {
        _localizedString = localizedString;
        _onLocaleChanged = onLocaleChanged;
        _updateText = updateText;
        _onDestroy = onDestroy;
    }

    public void RefreshString()
    {
        if (_isDestroyed) return;
        _updateText?.Invoke();
    }

    public void Destroy()
    {
        if (_isDestroyed) return;
        _onDestroy?.Invoke();
        _isDestroyed = true;
    }
}

using UnityEngine;
using TMPro;
using Unity.Localization;
using System;

public static class LocalizationManager
{
    public static LocalizedTextHandle Localize(
        string tableReference,
        Func<string> keyProvider,
        TMP_Text textComponent)
    {
        if (textComponent == null)
        {
            Debug.LogWarning("TMP_Text is null!");
            return null;
        }

        return new LocalizedTextHandle(tableReference, keyProvider, textComponent);
    }
}

public class LocalizedTextHandle : IDisposable
{
    private readonly string _tableReference;
    private readonly Func<string> _keyProvider;
    private readonly TMP_Text _textComponent;
    private readonly LocalizedString _localizedString;
    private readonly Action<string> _onStringChanged;

    private bool _isDestroyed = false;

    public LocalizedTextHandle(string tableReference, Func<string> keyProvider, TMP_Text textComponent)
    {
        _tableReference = tableReference;
        _keyProvider = keyProvider;
        _textComponent = textComponent;

        _localizedString = new LocalizedString
        {
            TableReference = _tableReference
        };

        _onStringChanged = OnLocalizedStringChanged;
        _localizedString.StringChanged += _onStringChanged;

        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;

        RefreshString();
    }

    private void OnLocalizedStringChanged(string value)
    {
        if (_textComponent != null && !_isDestroyed)
        {
            _textComponent.text = value;
        }
    }

    private void OnLocaleChanged()
    {
        RefreshString();
    }

    public void RefreshString()
    {
        if (_isDestroyed || _keyProvider == null || _textComponent == null) return;

        try
        {
            string key = _keyProvider.Invoke();
            _localizedString.TableEntryReference = new Unity.Localization.Tables.TableEntryReference(key);
            _localizedString.RefreshString();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Localization refresh failed: {ex.Message}");
        }
    }

    public void Destroy()
    {
        Dispose();
    }

    public void Dispose()
    {
        if (_isDestroyed) return;

        if (_localizedString != null)
        {
            _localizedString.StringChanged -= _onStringChanged;
        }

        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;

        _isDestroyed = true;
    }
}