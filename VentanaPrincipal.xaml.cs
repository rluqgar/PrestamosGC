using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PrestamosGC.Datos;

namespace PrestamosGC;

public partial class VentanaPrincipal : Window
{
    public VentanaPrincipal()
    {
        InitializeComponent();

        // al abrir la ventana cargamos la lista
        CargarLista();
    }

    private void CargarLista()
    {
        // cogemos todos los préstamos
        List<Prestamo> todos = App.Prestamos.ObtenerPrestamos();

        // nos quedamos con los del filtro marcado
        // "En préstamo" son los activos que no están vencidos
        List<Prestamo> mostrados;
        if (rbActivos.IsChecked == true)
        {
            mostrados = todos.Where(p => p.EstaActivo && !p.EstaVencido).ToList();
        }
        else if (rbVencidos.IsChecked == true)
        {
            mostrados = todos.Where(p => p.EstaVencido).ToList();
        }
        else if (rbDevueltos.IsChecked == true)
        {
            mostrados = todos.Where(p => !p.EstaActivo).ToList();
        }
        else
        {
            mostrados = todos;
        }

        lstPrestamos.ItemsSource = mostrados;

        // la barra de estado cuenta siempre sobre todos los préstamos
        int enPrestamo = todos.Count(p => p.EstaActivo && !p.EstaVencido);
        int vencidos = todos.Count(p => p.EstaVencido);
        int disponibles = App.Prestamos.PortatilesDisponibles().Count;

        txtEstado.Text = enPrestamo + " en préstamo · "
                         + vencidos + (vencidos == 1 ? " vencido" : " vencidos") + " · "
                         + disponibles + " portátiles disponibles de " + App.Prestamos.TotalPortatiles;
    }

    // este evento lo usan los cuatro RadioButton
    private void Filtro_Checked(object sender, RoutedEventArgs e)
    {
        // al crear la ventana rbTodos se marca solo y el evento salta antes de que exista la lista
        // sin esto da NullReferenceException
        if (!IsLoaded)
        {
            return;
        }

        CargarLista();
    }

    private void lstPrestamos_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // el botón solo se activa si hay algo seleccionado
        btnDetalle.IsEnabled = lstPrestamos.SelectedItem != null;
    }

    private void btnNuevo_Click(object sender, RoutedEventArgs e)
    {
        // abrimos el formulario como ventana modal
        VentanaPrestamo ventana = new VentanaPrestamo();
        ventana.Owner = this;

        if (ventana.ShowDialog() == true)
        {
            // se ha guardado, así que marcamos "Todos" y recargamos
            rbTodos.IsChecked = true;
            CargarLista();

            // dejamos seleccionado el préstamo nuevo
            Prestamo? nuevo = ventana.PrestamoCreado;
            if (nuevo != null)
            {
                lstPrestamos.SelectedItem = nuevo;
                lstPrestamos.ScrollIntoView(nuevo);
            }
        }
    }

    private void btnDetalle_Click(object sender, RoutedEventArgs e)
    {
        AbrirDetalle();
    }

    private void lstPrestamos_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        // el doble clic hace lo mismo que el botón
        AbrirDetalle();
    }

    private void AbrirDetalle()
    {
        // si hacemos doble clic en una zona vacía no hay nada seleccionado
        Prestamo? seleccionado = lstPrestamos.SelectedItem as Prestamo;
        if (seleccionado == null)
        {
            return;
        }

        VentanaDetalle detalle = new VentanaDetalle(seleccionado);
        detalle.Owner = this;

        // si se ha registrado la devolución recargamos la lista
        if (detalle.ShowDialog() == true)
        {
            CargarLista();
        }
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        // miramos cuántos préstamos vencidos hay antes de cerrar
        int vencidos = App.Prestamos.ObtenerPrestamos().Count(p => p.EstaVencido);

        if (vencidos > 0)
        {
            string mensaje = vencidos == 1
                ? "Hay 1 préstamo vencido. ¿Quieres salir de todos modos?"
                : "Hay " + vencidos + " préstamos vencidos. ¿Quieres salir de todos modos?";

            MessageBoxResult respuesta = MessageBox.Show(mensaje, "Préstamos vencidos",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            // si dice que no, cancelamos el cierre
            if (respuesta == MessageBoxResult.No)
            {
                e.Cancel = true;
            }
        }
    }
}
