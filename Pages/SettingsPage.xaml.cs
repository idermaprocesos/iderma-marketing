using System.Collections.ObjectModel;
using BitacoraGrabacion.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Windows.System;

namespace BitacoraGrabacion.Pages;

public sealed partial class SettingsPage : Page
{
    private readonly ObservableCollection<CatalogItem> _roles = [];
    private readonly ObservableCollection<Persona> _personas = [];
    private readonly ObservableCollection<CatalogItem> _camaras = [];
    private readonly ObservableCollection<CatalogItem> _equipos = [];

    public SettingsPage()
    {
        InitializeComponent();
        RolesList.ItemsSource = _roles;
        PersonasList.ItemsSource = _personas;
        CamarasList.ItemsSource = _camaras;
        EquiposList.ItemsSource = _equipos;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        RefrescarListas();
    }

    private void RefrescarListas()
    {
        Reemplazar(_roles, App.Data.Store.Roles);
        ReemplazarPersonas();
        Reemplazar(_camaras, App.Data.Store.Camaras);
        Reemplazar(_equipos, App.Data.Store.Equipos);
    }

    private void AgregarRol_Click(object sender, RoutedEventArgs e)
    {
        AgregarItem(RolBox, App.Data.Store.Roles, _roles);
    }

    private void AgregarPersona_Click(object sender, RoutedEventArgs e)
    {
        var nombre = PersonaBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(nombre))
        {
            return;
        }

        if (App.Data.Store.Personas.Any(p => string.Equals(p.Nombre, nombre, StringComparison.CurrentCultureIgnoreCase)))
        {
            PersonaBox.Text = string.Empty;
            return;
        }

        var persona = App.Data.AgregarPersona(nombre);
        _personas.Add(persona);
        PersonaBox.Text = string.Empty;
        PersonaBox.Focus(FocusState.Programmatic);
    }

    private void AgregarCamara_Click(object sender, RoutedEventArgs e) =>
        AgregarItem(CamaraBox, App.Data.Store.Camaras, _camaras);

    private void AgregarEquipo_Click(object sender, RoutedEventArgs e) =>
        AgregarItem(EquipoBox, App.Data.Store.Equipos, _equipos);

    private void RolBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter)
        {
            AgregarRol_Click(sender, e);
        }
    }

    private void PersonaBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter)
        {
            AgregarPersona_Click(sender, e);
        }
    }

    private void CamaraBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter)
        {
            AgregarCamara_Click(sender, e);
        }
    }

    private void EquipoBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter)
        {
            AgregarEquipo_Click(sender, e);
        }
    }

    private void EliminarRol_Click(object sender, RoutedEventArgs e)
    {
        EliminarItem(sender, App.Data.Store.Roles, _roles);
    }

    private void EliminarPersona_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || !Guid.TryParse(button.Tag?.ToString(), out var id))
        {
            return;
        }

        App.Data.EliminarPersona(id);
        var item = _personas.FirstOrDefault(p => p.Id == id);
        if (item is not null)
        {
            _personas.Remove(item);
        }
    }

    private void EliminarCamara_Click(object sender, RoutedEventArgs e) =>
        EliminarItem(sender, App.Data.Store.Camaras, _camaras);

    private void EliminarEquipo_Click(object sender, RoutedEventArgs e) =>
        EliminarItem(sender, App.Data.Store.Equipos, _equipos);

    private void AgregarItem(TextBox caja, List<CatalogItem> origen, ObservableCollection<CatalogItem> vista)
    {
        var nombre = caja.Text.Trim();
        if (string.IsNullOrWhiteSpace(nombre))
        {
            return;
        }

        if (origen.Any(i => string.Equals(i.Nombre, nombre, StringComparison.CurrentCultureIgnoreCase)))
        {
            caja.Text = string.Empty;
            return;
        }

        var item = App.Data.AgregarCatalogo(origen, nombre);
        vista.Add(item);
        caja.Text = string.Empty;
        caja.Focus(FocusState.Programmatic);
    }

    private void EliminarItem(object sender, List<CatalogItem> origen, ObservableCollection<CatalogItem> vista)
    {
        if (sender is not Button button || !Guid.TryParse(button.Tag?.ToString(), out var id))
        {
            return;
        }

        App.Data.EliminarCatalogo(origen, id);
        var item = vista.FirstOrDefault(i => i.Id == id);
        if (item is not null)
        {
            vista.Remove(item);
        }
    }

    private void ReemplazarPersonas()
    {
        _personas.Clear();
        foreach (var persona in App.Data.Store.Personas.OrderBy(p => p.Nombre))
        {
            _personas.Add(persona);
        }
    }

    private static void Reemplazar(ObservableCollection<CatalogItem> destino, IEnumerable<CatalogItem> origen)
    {
        destino.Clear();
        foreach (var item in origen.OrderBy(i => i.Nombre))
        {
            destino.Add(item);
        }
    }
}
