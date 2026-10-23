using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace BlueprintMaker.Presentation.Avalonia;

/// <summary>
/// Глобальные настройки приложения. Singleton — доступ через AppSettings.Instance
/// </summary>
public partial class AppSettings : ReactiveObject
{
    public static AppSettings Instance { get; } = new();

    private AppSettings() { } 

    [Reactive] private bool _isNodeVisible = true;
    [Reactive] private double _zoom = 1.0;
    [Reactive] private bool _isGridVisible = false;
    [Reactive] private double _gridSize = 50.0;
    [Reactive] private bool _isDimensionsVisible = false;
}