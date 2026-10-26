using BlueprintMaker.Application;
using BlueprintMaker.Application.Commands;
using BlueprintMaker.Application.Persistence;
using BlueprintMaker.Core.Geometry;
using BlueprintMaker.Domain.Models;
using BlueprintMaker.Domain.Models.Materialisation;
using BlueprintMaker.Presentation.Avalonia.Helpers;
using ReactiveUI;
using System;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace BlueprintMaker.Presentation.Avalonia.ViewModels;

public partial class MainViewModel
{
    private string? _baseline;         // снимок <чистого> состояния (сохранённого или только что созданного)
    private string? _lastAutosaved;    // снимок, записанный автосохранением
 
    //  Состояние <есть несохранённые изменения>
  
    private string? Snapshot() =>
        _document.Root == null ? null : ProjectSerializer.Snapshot(_document);

    public bool HasUnsavedChanges => _document.Root != null && Snapshot() != _baseline;

    private void UpdateDirty() => IsDirty = HasUnsavedChanges;

    private void MarkClean()
    {
        _baseline = Snapshot();
        UpdateDirty();
    }

    private ViewStateDto CaptureView() => new()
    {
        Zoom = AppSettings.Instance.Zoom,
        PanX = AppSettings.Instance.PanX,
        PanY = AppSettings.Instance.PanY
    };

    //  Подтверждение потери изменений

    /// <summary>
    /// true — можно продолжать (изменений нет, сохранили или отказались от них),
    /// false — пользователь отменил действие.
    /// </summary>
    public async Task<bool> ConfirmCanDiscardAsync()
    {
        if (!HasUnsavedChanges) return true;

        var name = CurrentPath != null ? Path.GetFileName(CurrentPath) : "Без имени";
        var choice = await AskUnsaved.Handle($"Сохранить изменения в <{name}>?");

        return choice switch
        {
            ChoiceResult.Yes => await SaveAsync(false),
            ChoiceResult.No => true,
            _ => false
        };
    }

    // Создание окна
   
    private async Task CreateWindowAsync()
    {
        var contour = await AskNewWindow.Handle(Unit.Default);
        if (contour == null) return;
        if (!await ConfirmCanDiscardAsync()) return;

        _document = new Document();
        _history = new DocumentHistory();

        // Создание рамы выполняется вне истории: Undo не должен удалять само окно
        new CreateFrameCommand(_document, contour).Execute();

        ShowDocument();
        CurrentPath = null;
        _firstCentered = CanvasWidth > 0;

        UpdateHistoryState();
        CenterWindow();
        _modelChanged.OnNext(Unit.Default);
        MarkClean();                       // свежесозданное окно изменением не считается
        StatusMessage = "Окно создано";
    }

    /// <summary>Пересоздаёт ViewModel рамы для текущего документа.</summary>
    private void ShowDocument()
    {
        Frames.Clear();
        if (_document.Root == null) return;

        Frames.Add(new FrameViewModel(_document.Root, _history, SelectElementCommand, AddJambCmd));
        SelectedFrame = Frames[0];
        SelectedFrame.IsSelected = true;
        SelectedElement = null;
    }

    // Сохранение

    private async Task<bool> SaveAsync(bool forceDialog)
    {
        if (_document.Root == null) return false;

        var path = CurrentPath;
        if (forceDialog || path == null)
        {
            var suggested = Path.GetFileName(CurrentPath ?? "Окно" + ProjectFileService.Extension);
            path = await AskSavePath.Handle(suggested);
            if (path == null) return false;
            if (!Path.HasExtension(path)) path += ProjectFileService.Extension;
        }

        try
        {
            ProjectFileService.Save(path, ProjectSerializer.Capture(_document, CaptureView()));
        }
        catch (Exception ex)
        {
            StatusMessage = $"Не удалось сохранить: {ex.Message}";
            return false;
        }

        CurrentPath = path;
        MarkClean();
        _lastAutosaved = null;
        ProjectFileService.DeleteAutosave();
        StatusMessage = $"Сохранено: {Path.GetFileName(path)}";
        return true;
    }

    // Открытие
    
    private async Task OpenAsync()
    {
        var path = await AskOpenPath.Handle(Unit.Default);
        if (path == null) return;
        if (!await ConfirmCanDiscardAsync()) return;

        ProjectFileDto dto;
        try { dto = ProjectFileService.Load(path); }
        catch (Exception ex)
        {
            StatusMessage = $"Не удалось открыть файл: {ex.Message}";
            return;
        }

        if (LoadProject(dto, path, markDirty: false))
            ProjectFileService.DeleteAutosave();
    }

    /// <summary>Строит новый документ отдельно и подменяет текущий только при успехе.</summary>
    private bool LoadProject(ProjectFileDto dto, string? path, bool markDirty)
    {
        LoadResult result;
        try { result = ProjectSerializer.Restore(dto); }
        catch (Exception ex)
        {
            StatusMessage = $"Не удалось открыть файл: {ex.Message}";
            return false;
        }

        _document = result.Document;
        _history = new DocumentHistory();
        ShowDocument();

        if (dto.View != null)
        {
            AppSettings.Instance.Zoom = dto.View.Zoom;
            AppSettings.Instance.PanX = dto.View.PanX;
            AppSettings.Instance.PanY = dto.View.PanY;
        }

        _firstCentered = true;             // положение берётся из файла
        CurrentPath = path;
        UpdateHistoryState();
        _modelChanged.OnNext(Unit.Default);

        // Диагностика: восстановленная модель должна совпасть с сохранённой
        var warnings = result.Warnings.ToList();
        if (Snapshot() != ProjectSerializer.SnapshotOf(dto.Root))
            warnings.Add("Восстановленное окно отличается от сохранённого.");

        if (markDirty) { _baseline = null; UpdateDirty(); }
        else MarkClean();

        StatusMessage = warnings.Count == 0
            ? $"Открыто: {(path != null ? Path.GetFileName(path) : "восстановленная копия")}"
            : $"Открыто с предупреждениями ({warnings.Count}): {warnings[0]}";
        return true;
    }

    //  Автосохранение и восстановление
 
    private void StartAutosave() =>
        Observable.Interval(TimeSpan.FromSeconds(30))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => AutosaveIfNeeded());

    private void AutosaveIfNeeded()
    {
        if (_document.Root == null) return;

        var snap = Snapshot();
        if (snap == _baseline || snap == _lastAutosaved) return;

        try
        {
            ProjectFileService.Save(ProjectFileService.AutosavePath,
                ProjectSerializer.Capture(_document, CaptureView(), CurrentPath));
            _lastAutosaved = snap;
        }
        catch { /* автосохранение не должно мешать работе */ }
    }

    /// <summary>Вызывается при запуске: предлагает восстановить копию после аварийного закрытия.</summary>
    public async Task TryRecoverAutosaveAsync()
    {
        var path = ProjectFileService.AutosavePath;
        if (_document.Root != null || !File.Exists(path)) return;

        ProjectFileDto dto;
        try { dto = ProjectFileService.Load(path); }
        catch { ProjectFileService.DeleteAutosave(); return; }

        var name = dto.SourcePath != null ? Path.GetFileName(dto.SourcePath) : "без имени";
        var choice = await AskRecover.Handle(
            $"Найдена несохранённая копия <{name}> от {dto.SavedUtc.ToLocalTime():g}. Восстановить?");

        switch (choice)
        {
            case ChoiceResult.Yes:
                LoadProject(dto, dto.SourcePath, markDirty: true);   // останется <изменённой>
                break;
            case ChoiceResult.No:
                ProjectFileService.DeleteAutosave();
                break;
                // Cancel — оставить до следующего запуска
        }
    }

    public void DeleteAutosave() => ProjectFileService.DeleteAutosave();
}