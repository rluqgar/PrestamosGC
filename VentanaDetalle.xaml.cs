using System.Windows;
using PrestamosGC.Datos;

namespace PrestamosGC;

public partial class VentanaDetalle : Window
{
    public VentanaDetalle(Prestamo prestamo)
    {
        InitializeComponent();

        // así los Binding del XAML leen los datos de este préstamo
        DataContext = prestamo;
    }

    private void RegistrarDevolucion_Click(object sender, RoutedEventArgs e)
    {
        // el préstamo es el DataContext que hemos puesto antes
        Prestamo prestamo = (Prestamo)DataContext;

        // primero pedimos confirmación
        MessageBoxResult respuesta = MessageBox.Show(
            "¿Registrar la devolución del portátil " + prestamo.Portatil + "?",
            "Confirmar devolución",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (respuesta == MessageBoxResult.Yes)
        {
            try
            {
                App.Prestamos.RegistrarDevolucion(prestamo.Id);

                // cerramos con true para que la ventana principal recargue la lista
                DialogResult = true;
            }
            catch (InvalidOperationException ex)
            {
                // si el gestor da error lo enseñamos para que no se cierre el programa
                MessageBox.Show(ex.Message, "No se pudo registrar la devolución",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
