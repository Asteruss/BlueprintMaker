using Avalonia.Media;
using BlueprintMaker.Core.Geometry;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive;

namespace BlueprintMaker.Presentation.Avalonia.ViewModels;

public abstract partial class SelectableElement : ReactiveObject
{
    [Reactive] private bool _isSelected;
    [Reactive] private IBrush _displayColor = Brushes.Black;
    public double StrokeThickness => GeometryConstants.DisplayWidth;
    public ReactiveCommand<SelectableElement, Unit> SelectCommand { get; }
    [Reactive] private string _displayName = "Элемент";
    [Reactive] private List<SelectableElement> _treeChildren = new();
    // Рекурсивная коллекция для дерева
    public ObservableCollection<SelectableElement> Children { get; } = new();

    protected SelectableElement(ReactiveCommand<SelectableElement, Unit> selectCommand)
    {
        SelectCommand = selectCommand;
        this.WhenAnyValue(x => x.IsSelected).Subscribe(_ => OnSelectedChanged());
        Children.CollectionChanged += OnChildrenChanged;

    }

    private void OnChildrenChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RefreshTreeChildren();
    }

    /// <summary>
    /// Базовая фильтрация: семантические + Board.
    /// Override в FrameViewModel/SashViewModel — добавить поднятые Jamb.
    /// Override в LightSpaceViewModel — убрать Jamb.
    /// </summary>
    public virtual void RefreshTreeChildren()
    {
        TreeChildren = Children
            .Where(c => c is LightSpaceViewModel or ImpostViewModel or
                            SashViewModel or BoardViewModel)
            .ToList();
    }

    /// <summary>
    /// Рекурсивно обновить TreeChildren на себе и всех потомках.
    /// Вызывается из Refresh() после BuildTree.
    /// </summary>
    public void RefreshTreeChildrenRecursive()
    {
        RefreshTreeChildren();
        foreach (var child in Children)
            if (child is SelectableElement se)
                se.RefreshTreeChildrenRecursive();
    }

    protected virtual void OnSelectedChanged()
    {
        DisplayColor = IsSelected ? Brushes.Blue : Brushes.Black;
    }
}