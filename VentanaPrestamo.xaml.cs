using System.Net.Mail;
using System.Windows;
using System.Windows.Controls;
using PrestamosGC.Datos;

namespace PrestamosGC;

public partial class VentanaPrestamo : Window
{
    // aquí dejamos el préstamo guardado para que la ventana principal lo pueda leer
    public Prestamo? PrestamoCreado { get; private set; }

    public VentanaPrestamo()
    {
        InitializeComponent();

        // rellenamos el combo con los portátiles que están libres
        cmbPortatil.ItemsSource = App.Prestamos.PortatilesDisponibles();

        // la fecha de préstamo es hoy y no se pueden elegir días anteriores
        dpPrestamo.DisplayDateStart = DateTime.Today;
        dpPrestamo.SelectedDate = DateTime.Today;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // el foco empieza en el nombre
        txtNombre.Focus();
    }

    private void FechaPrestamo_Changed(object? sender, SelectionChangedEventArgs e)
    {
        if (dpPrestamo.SelectedDate != null)
        {
            DateTime prestamo = dpPrestamo.SelectedDate.Value;
            DateTime minimo = prestamo.AddDays(1);
            DateTime maximo = prestamo.AddDays(14);

            // si la devolución que ya estaba elegida queda fuera, la borramos
            DateTime? devolucion = dpDevolucion.SelectedDate;
            if (devolucion != null && (devolucion.Value.Date < minimo || devolucion.Value.Date > maximo))
            {
                dpDevolucion.SelectedDate = null;
            }

            // la devolución va desde el día siguiente hasta 14 días después
            dpDevolucion.DisplayDateStart = minimo;
            dpDevolucion.DisplayDateEnd = maximo;
        }

        OcultarErroresCorregidos();
    }

    private void Observaciones_TextChanged(object sender, TextChangedEventArgs e)
    {
        // actualizamos el contador, por ejemplo 37/200
        txtContador.Text = txtObservaciones.Text.Length + "/200";

        OcultarErroresCorregidos();
    }

    private void Motivo_Checked(object sender, RoutedEventArgs e)
    {
        // si el motivo es Otro las observaciones son obligatorias
        if (rbOtro.IsChecked == true)
        {
            lblObservaciones.Text = "Observaciones (obligatorias)";
        }
        else
        {
            lblObservaciones.Text = "Observaciones";
        }

        OcultarErroresCorregidos();
    }

    // lo usan casi todos los campos (textos, combos, fecha y casilla)
    private void Campo_Cambiado(object sender, RoutedEventArgs e)
    {
        OcultarErroresCorregidos();
    }

    // vuelve a validar solo los campos que ahora mismo tienen el error a la vista
    // si ya están bien, el error desaparece. Así no salen errores antes de pulsar Registrar
    private void OcultarErroresCorregidos()
    {
        txtErrorGeneral.Visibility = Visibility.Collapsed;

        if (errNombre.Visibility == Visibility.Visible) ValidarNombre();
        if (errApellidos.Visibility == Visibility.Visible) ValidarApellidos();
        if (errGrupo.Visibility == Visibility.Visible) ValidarGrupo();
        if (errCorreo.Visibility == Visibility.Visible) ValidarCorreo();
        if (errPortatil.Visibility == Visibility.Visible) ValidarPortatil();
        if (errMotivo.Visibility == Visibility.Visible) ValidarMotivo();
        if (errFechaPrestamo.Visibility == Visibility.Visible) ValidarFechaPrestamo();
        if (errFechaDevolucion.Visibility == Visibility.Visible) ValidarFechaDevolucion();
        if (errObservaciones.Visibility == Visibility.Visible) ValidarObservaciones();
        if (errNormas.Visibility == Visibility.Visible) ValidarNormas();
    }

    private void Registrar_Click(object sender, RoutedEventArgs e)
    {
        txtErrorGeneral.Visibility = Visibility.Collapsed;

        // validamos todo y guardamos en una lista los campos que fallan
        List<Control> camposConError = new List<Control>();

        if (!ValidarNombre()) camposConError.Add(txtNombre);
        if (!ValidarApellidos()) camposConError.Add(txtApellidos);
        if (!ValidarGrupo()) camposConError.Add(cmbGrupo);
        if (!ValidarCorreo()) camposConError.Add(txtCorreo);
        if (!ValidarPortatil()) camposConError.Add(cmbPortatil);
        if (!ValidarMotivo()) camposConError.Add(rbProyecto);
        if (!ValidarFechaPrestamo()) camposConError.Add(dpPrestamo);
        if (!ValidarFechaDevolucion()) camposConError.Add(dpDevolucion);
        if (!ValidarObservaciones()) camposConError.Add(txtObservaciones);
        if (!ValidarNormas()) camposConError.Add(chkNormas);

        if (camposConError.Count > 0)
        {
            // hay errores, foco al primero y no guardamos
            camposConError[0].Focus();
            return;
        }

        // todo bien, creamos el préstamo con los datos del formulario
        Prestamo nuevo = new Prestamo();
        nuevo.Nombre = txtNombre.Text.Trim();
        nuevo.Apellidos = txtApellidos.Text.Trim();

        // el grupo son ComboBoxItem escritos en el XAML, así que hay que coger su Content
        ComboBoxItem grupoElegido = (ComboBoxItem)cmbGrupo.SelectedItem;
        nuevo.Grupo = grupoElegido.Content?.ToString() ?? "";

        nuevo.Correo = txtCorreo.Text.Trim();

        // el portátil se rellenó con cadenas, así que SelectedItem ya es el texto
        nuevo.Portatil = cmbPortatil.SelectedItem?.ToString() ?? "";

        nuevo.Motivo = ObtenerMotivo();
        nuevo.IncluyeCargador = chkCargador.IsChecked == true;
        nuevo.Observaciones = txtObservaciones.Text.Trim();
        nuevo.FechaPrestamo = dpPrestamo.SelectedDate!.Value;
        nuevo.FechaDevolucionPrevista = dpDevolucion.SelectedDate!.Value;

        // el gestor lanza InvalidOperationException si el portátil ya está prestado
        // o si el alumno ya tiene otro, así que lo controlamos
        try
        {
            App.Prestamos.InsertarPrestamo(nuevo);
            PrestamoCreado = nuevo;
            DialogResult = true;
        }
        catch (InvalidOperationException ex)
        {
            txtErrorGeneral.Text = ex.Message;
            txtErrorGeneral.Visibility = Visibility.Visible;
        }
    }

    // cada validación mira un campo, enseña u oculta su error y devuelve true si está bien

    private bool ValidarNombre()
    {
        bool correcto = txtNombre.Text.Trim() != "";
        MostrarError(errNombre, !correcto);
        return correcto;
    }

    private bool ValidarApellidos()
    {
        bool correcto = txtApellidos.Text.Trim() != "";
        MostrarError(errApellidos, !correcto);
        return correcto;
    }

    private bool ValidarGrupo()
    {
        // si no hay nada elegido SelectedItem es null
        bool correcto = cmbGrupo.SelectedItem != null;
        MostrarError(errGrupo, !correcto);
        return correcto;
    }

    private bool ValidarCorreo()
    {
        string correo = txtCorreo.Text.Trim();

        // formato válido y acabado en @g.educaand.es
        bool correcto = MailAddress.TryCreate(correo, out _)
                        && correo.EndsWith("@g.educaand.es", StringComparison.OrdinalIgnoreCase);

        MostrarError(errCorreo, !correcto);
        return correcto;
    }

    private bool ValidarPortatil()
    {
        bool correcto = cmbPortatil.SelectedItem != null;
        MostrarError(errPortatil, !correcto);
        return correcto;
    }

    private bool ValidarMotivo()
    {
        bool correcto = ObtenerMotivo() != "";
        MostrarError(errMotivo, !correcto);
        return correcto;
    }

    private bool ValidarFechaPrestamo()
    {
        DateTime? fecha = dpPrestamo.SelectedDate;

        // tiene que haber fecha y no puede ser anterior a hoy
        bool correcto = fecha != null && fecha.Value.Date >= DateTime.Today;

        MostrarError(errFechaPrestamo, !correcto);
        return correcto;
    }

    private bool ValidarFechaDevolucion()
    {
        DateTime? prestamo = dpPrestamo.SelectedDate;
        DateTime? devolucion = dpDevolucion.SelectedDate;

        bool correcto = false;
        if (prestamo != null && devolucion != null)
        {
            // los días del préstamo tienen que estar entre 1 y 14
            int dias = (devolucion.Value.Date - prestamo.Value.Date).Days;
            correcto = dias >= 1 && dias <= 14;
        }

        MostrarError(errFechaDevolucion, !correcto);
        return correcto;
    }

    private bool ValidarObservaciones()
    {
        // solo son obligatorias si el motivo es Otro
        bool correcto = true;
        if (ObtenerMotivo() == "Otro" && txtObservaciones.Text.Trim() == "")
        {
            correcto = false;
        }

        MostrarError(errObservaciones, !correcto);
        return correcto;
    }

    private bool ValidarNormas()
    {
        bool correcto = chkNormas.IsChecked == true;
        MostrarError(errNormas, !correcto);
        return correcto;
    }

    // enseña u oculta el TextBlock de error que le pasamos
    private void MostrarError(TextBlock error, bool hayError)
    {
        error.Visibility = hayError ? Visibility.Visible : Visibility.Collapsed;
    }

    // devuelve el motivo marcado o "" si no hay ninguno
    private string ObtenerMotivo()
    {
        if (rbProyecto.IsChecked == true) return "Proyecto";
        if (rbPracticas.IsChecked == true) return "Prácticas";
        if (rbOtro.IsChecked == true) return "Otro";
        return "";
    }
}
