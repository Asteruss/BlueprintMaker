using ReactiveUI;
using System;
using System.Reactive;

namespace BlueprintMaker.Presentation.Avalonia.ViewModels;

public sealed class JambSlotViewModel
{
    private const double Half = 12;
    private const double Offset = 22;

    public JambSlotViewModel(JambSlot slot, ReactiveCommand<JambSlot, Unit> addCommand)
    {
        Slot = slot;
        AddCommand = addCommand;

        double x = slot.Position.X + slot.Direction.X * Offset;
        double y = slot.Position.Y + slot.Direction.Y * Offset;

        Left = x - Half;
        Top = y - Half;
        Angle = Math.Atan2(slot.Direction.Y, slot.Direction.X) * 180 / Math.PI;
    }

    public JambSlot Slot { get; }
    public ReactiveCommand<JambSlot, Unit> AddCommand { get; }
    public double Left { get; }
    public double Top { get; }
    public double Angle { get; }
}