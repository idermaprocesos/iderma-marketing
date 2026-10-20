using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
using WinRT.Interop;

namespace BitacoraGrabacion.Services;

public static class InformeExport
{
    public static async Task<bool> GuardarAsync(Window window, string nombreSugerido, string html)
    {
        var picker = new FileSavePicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            SuggestedFileName = NombreArchivo(nombreSugerido)
        };
        picker.FileTypeChoices.Add("Informe HTML", [".html"]);
        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(window));

        var archivo = await picker.PickSaveFileAsync();
        if (archivo is null)
        {
            return false;
        }

        await FileIO.WriteTextAsync(archivo, html);
        await PreguntarAbrirAsync(window, archivo);
        return true;
    }

    private static async Task PreguntarAbrirAsync(Window window, StorageFile archivo)
    {
        var raiz = (window.Content as FrameworkElement)?.XamlRoot;
        if (raiz is null)
        {
            await Launcher.LaunchFileAsync(archivo);
            return;
        }

        var dialogo = new ContentDialog
        {
            Title = "Informe generado",
            Content = "El reporte se guardó correctamente. ¿Quieres abrirlo ahora?",
            PrimaryButtonText = "Abrir",
            CloseButtonText = "Cerrar",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = raiz
        };

        if (await dialogo.ShowAsync() == ContentDialogResult.Primary)
        {
            await Launcher.LaunchFileAsync(archivo);
        }
    }

    public static string NombreArchivo(string? titulo)
    {
        var limpio = string.Join(" ", (titulo ?? string.Empty).Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries)).Trim();
        if (string.IsNullOrWhiteSpace(limpio))
        {
            limpio = "grabacion";
        }

        if (limpio.Length > 60)
        {
            limpio = limpio[..60].Trim();
        }

        return limpio;
    }
}
