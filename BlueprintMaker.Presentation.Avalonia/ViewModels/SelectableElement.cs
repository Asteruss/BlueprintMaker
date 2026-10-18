using Avalonia.Media;
using BlueprintMaker.Core.Geometry;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System;
using System.Collections.ObjectModel;
using System.Reactive;

namespace BlueprintMaker.Presentation.Avalonia.ViewModels;

public abstract partial class SelectableElement : ReactiveObject
{
    [Reactive] private bool _isSelected;
    [Reactive] private IBrush _displayColor = Brushes.Black;
    public double StrokeThickness => GeometryConstants.DisplayWidth;
    public ReactiveCommand<SelectableElement, Unit> SelectCommand { get; }
    [Reactive] private string _displayName = "Элемент";

    // Рекурсивная коллекция для дерева
    public ObservableCollection<SelectableElement> Children { get; } = new();

    protected SelectableElement(ReactiveCommand<SelectableElement, Unit> selectCommand)
    {
        SelectCommand = selectCommand;
        this.WhenAnyValue(x => x.IsSelected).Subscribe(_ => OnSelectedChanged());
    }

    protected virtual void OnSelectedChanged()
    {
        DisplayColor = IsSelected ? Brushes.Blue : Brushes.Black;
    }
}