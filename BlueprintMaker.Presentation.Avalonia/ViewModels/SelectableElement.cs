using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System.Reactive;
using System;

namespace BlueprintMaker.Presentation.Avalonia.ViewModels;

public abstract partial class SelectableElement : ReactiveObject
{
    [Reactive] private bool _isSelected;

    public ReactiveCommand<SelectableElement, Unit> SelectCommand { get; }
    public string DisplayName { get; protected set; } = "Элемент";

    protected SelectableElement(ReactiveCommand<SelectableElement, Unit> selectCommand)
    {
        SelectCommand = selectCommand;

        // Подписываемся на изменение IsSelected
        this.WhenAnyValue(x => x.IsSelected)
            .Subscribe(_ => OnSelectedChanged());
    }

    protected virtual void OnSelectedChanged() { }
}