using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using TaskDB1.Data;
using TaskDB1.Forms;

// Usa los eventos reales de los formularios y una base temporal exclusiva de esta prueba.
internal static class VerificarTaskDB
{
    private delegate bool EnumWindowProc(IntPtr window, IntPtr parameter);
    [DllImport("user32.dll")] private static extern bool EnumThreadWindows(uint threadId, EnumWindowProc callback, IntPtr parameter);
    [DllImport("user32.dll")] private static extern bool EnumChildWindows(IntPtr parent, EnumWindowProc callback, IntPtr parameter);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(IntPtr window, StringBuilder name, int maxCount);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(IntPtr window, StringBuilder text, int maxCount);
    [DllImport("user32.dll")] private static extern bool PostMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();

    private static readonly List<string> Messages = new List<string>();
    private static int checks;
    private static string databaseName;
    private static string masterConnection;

    [STAThread]
    private static int Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        string dataDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Datos");
        AppDomain.CurrentDomain.SetData("DataDirectory", dataDirectory);
        using (var messages = new Timer())
        {
            messages.Interval = 50;
            messages.Tick += delegate { CloseMessages(); };
            messages.Start();
            try
            {
                DatabaseConnection.Initialize();
                using (var connection = DatabaseConnection.GetConnection())
                {
                    connection.Open();
                    databaseName = connection.Database;
                    var builder = new SqlConnectionStringBuilder(connection.ConnectionString);
                    builder.InitialCatalog = "master";
                    builder.AttachDBFilename = string.Empty;
                    masterConnection = builder.ConnectionString;
                }
                DatabaseConnection.Initialize();
                Check(CountTasks() == 0, "Inicialización repetida y base vacía");
                Check(File.Exists(Path.Combine(dataDirectory, "TaskDB.mdf")), "Base creada por la instancia instalada");

                using (var listado = new FrmListadoTareas())
                {
                    listado.Show();
                    Application.DoEvents();
                    var grid = Find<DataGridView>(listado, "dgvTareas");
                    var estado = Find<ComboBox>(listado, "cmbEstado");
                    Check(Rows(grid) == 0 && grid.Columns.Count == 5, "Listado vacío con las cinco columnas");
                    Check(grid.ReadOnly && !grid.AllowUserToAddRows && !grid.AllowUserToDeleteRows && !grid.MultiSelect,
                        "Grilla de solo lectura y selección única");
                    Check(grid.SelectionMode == DataGridViewSelectionMode.FullRowSelect &&
                        grid.AutoSizeColumnsMode == DataGridViewAutoSizeColumnsMode.Fill, "Configuración de filas y columnas");
                    Check(estado.Items.Count == 3 && estado.DropDownStyle == ComboBoxStyle.DropDownList, "Opciones de estado cerradas");

                    using (var registro = new FrmAgregarTarea())
                    {
                        registro.Show(listado);
                        Find<TextBox>(registro, "txtTitulo").Text = "   ";
                        Messages.Clear();
                        Find<Button>(registro, "btnGuardar").PerformClick();
                        Check(CountTasks() == 0 && MessagesContain("obligatorio"), "Título con espacios rechazado");
                        Find<TextBox>(registro, "txtTitulo").Text = new string('a', 201);
                        Messages.Clear();
                        Find<Button>(registro, "btnGuardar").PerformClick();
                        Check(CountTasks() == 0 && MessagesContain("200 caracteres"), "Título demasiado largo rechazado");
                        Find<TextBox>(registro, "txtTitulo").Text = "Tarea válida";
                        Find<TextBox>(registro, "txtDescripcion").Text = new string('b', 2001);
                        Messages.Clear();
                        Find<Button>(registro, "btnGuardar").PerformClick();
                        Check(CountTasks() == 0 && MessagesContain("2000 caracteres"), "Descripción demasiado larga rechazada");
                        Find<Button>(registro, "btnCancelar").PerformClick();
                        Check(registro.DialogResult == DialogResult.Cancel, "Cancelar cierra sin guardar");
                    }

                    Messages.Clear();
                    grid.ClearSelection();
                    Find<Button>(listado, "btnCompletar").PerformClick();
                    Check(MessagesContain("Selecciona") && CountTasks() == 0, "Completar sin selección muestra validación");

                    estado.SelectedItem = "Completada";
                    string title = "Revisar O'Brien '; DROP TABLE dbo.Tareas;--";
                    OpenRegistration(listado, title, "Descripción con acentos y 'comillas'", true);
                    Check(CountTasks() == 1, "Guardar desde el listado inserta una sola tarea");
                    Check((string)estado.SelectedItem == "Completada" && Rows(grid) == 0, "Guardar conserva el filtro sin resultados");
                    estado.SelectedItem = "Pendiente";
                    Check(Rows(grid) == 1 && (string)grid.Rows[0].Cells["Titulo"].Value == title,
                        "Título con contenido SQL guardado como texto");
                    Check((string)grid.Rows[0].Cells["Estado"].Value == "Pendiente", "Estado inicial Pendiente");
                    DateTime creation = (DateTime)grid.Rows[0].Cells["FechaCreacion"].Value;
                    Check((int)Scalar("SELECT COUNT(*) FROM dbo.Tareas WHERE DATEDIFF(second, FechaCreacion, SYSDATETIME()) BETWEEN 0 AND 60") == 1,
                        "Fecha de creación generada por SQL Server");

                    DatabaseConnection.Initialize();
                    Check(CountTasks() == 1, "Reinicializar conserva las tareas");
                    OpenRegistration(listado, "Tarea cancelada", "", false);
                    Check(CountTasks() == 1 && Rows(grid) == 1 && (string)estado.SelectedItem == "Pendiente",
                        "Cancelar desde el listado conserva los datos y el filtro");

                    grid.Rows[0].Selected = true;
                    Find<Button>(listado, "btnCompletar").PerformClick();
                    Check(Rows(grid) == 0 && CountTasks() == 1, "Completar actualiza el registro y recarga el filtro Pendiente");
                    estado.SelectedItem = "Completada";
                    Check(Rows(grid) == 1 && (string)grid.Rows[0].Cells["Estado"].Value == "Completada", "Filtro Completada actualizado");
                    Check((DateTime)grid.Rows[0].Cells["FechaCreacion"].Value == creation, "Completar conserva la fecha original");
                    grid.Rows[0].Selected = true;
                    Messages.Clear();
                    Find<Button>(listado, "btnCompletar").PerformClick();
                    Check(MessagesContain("ya está completada") && CountTasks() == 1, "Completar de nuevo no duplica registros");
                    estado.SelectedItem = "Todas";
                    Find<Button>(listado, "btnFiltrar").PerformClick();
                    Check(Rows(grid) == 1, "Todas y botón Filtrar muestran los datos recientes");

                    CheckConstraint("INSERT INTO dbo.Tareas (Titulo, Estado) VALUES (@Titulo, @Estado)", "Válida", "Otro",
                        "La base rechaza estados distintos a Pendiente y Completada");
                    CheckConstraint("INSERT INTO dbo.Tareas (Titulo, Estado) VALUES (@Titulo, @Estado)", "   ", "Pendiente",
                        "La base rechaza títulos en blanco");

                    Execute("EXEC sp_rename N'dbo.Tareas', N'TareasPruebaLectura'");
                    try
                    {
                        Messages.Clear();
                        Find<Button>(listado, "btnFiltrar").PerformClick();
                        Check(MessagesContain("No se pudieron cargar") && grid.DataSource == null, "Error de lectura controlado sin datos obsoletos");
                    }
                    finally { Execute("EXEC sp_rename N'dbo.TareasPruebaLectura', N'Tareas'"); }
                    Find<Button>(listado, "btnFiltrar").PerformClick();
                    Check(Rows(grid) == 1, "Listado recuperado después de un error");

                    SaveImage(listado, "listado.png");
                    using (var registro = new FrmAgregarTarea())
                    {
                        registro.Show(listado);
                        SaveImage(registro, "registro.png");
                        registro.Close();
                    }
                    listado.Close();
                }

                SqlConnection.ClearAllPools();
                using (var connection = new SqlConnection(masterConnection))
                using (var command = connection.CreateCommand())
                {
                    connection.Open();
                    command.CommandText = "EXEC sp_detach_db @dbname";
                    command.Parameters.Add("@dbname", SqlDbType.NVarChar, 128).Value = databaseName;
                    command.ExecuteNonQuery();
                }
                DatabaseConnection.Initialize();
                Check(CountTasks() == 1, "La base existente se vuelve a adjuntar sin perder registros");
                Console.WriteLine("Correcto: " + checks + " comprobaciones con LocalDB y los eventos de los formularios.");
                return 0;
            }
            catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
            finally
            {
                messages.Stop();
                if (databaseName != null && masterConnection != null)
                {
                    SqlConnection.ClearAllPools();
                    using (var connection = new SqlConnection(masterConnection))
                    using (var command = connection.CreateCommand())
                    {
                        connection.Open();
                        string identifier = "[" + databaseName.Replace("]", "]]") + "]";
                        command.CommandText = "ALTER DATABASE " + identifier + " SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE " + identifier;
                        command.ExecuteNonQuery();
                    }
                }
            }
        }
    }

    private static T Find<T>(Form form, string name) where T : Control
    {
        return (T)form.Controls.Find(name, true)[0];
    }

    private static int Rows(DataGridView grid) { return ((DataTable)grid.DataSource).Rows.Count; }
    private static int CountTasks() { return (int)Scalar("SELECT COUNT(*) FROM dbo.Tareas"); }
    private static object Scalar(string sql)
    {
        using (var connection = DatabaseConnection.GetConnection())
        using (var command = new SqlCommand(sql, connection)) { connection.Open(); return command.ExecuteScalar(); }
    }
    private static void Execute(string sql)
    {
        using (var connection = DatabaseConnection.GetConnection())
        using (var command = new SqlCommand(sql, connection)) { connection.Open(); command.ExecuteNonQuery(); }
    }
    private static void Check(bool valid, string name)
    {
        if (!valid) throw new InvalidOperationException("Falló: " + name);
        checks++;
        Console.WriteLine("OK: " + name);
    }
    private static bool MessagesContain(string value) { return Messages.Exists(delegate(string text) { return text.Contains(value); }); }

    private static void CheckConstraint(string sql, string title, string status, string name)
    {
        bool rejected = false;
        using (var connection = DatabaseConnection.GetConnection())
        using (var command = new SqlCommand(sql, connection))
        {
            command.Parameters.Add("@Titulo", SqlDbType.NVarChar, 200).Value = title;
            command.Parameters.Add("@Estado", SqlDbType.NVarChar, 10).Value = status;
            connection.Open();
            try { command.ExecuteNonQuery(); }
            catch (SqlException ex) { if (ex.Number != 547) throw; rejected = true; }
        }
        Check(rejected, name);
    }

    private static void OpenRegistration(FrmListadoTareas listado, string title, string description, bool save)
    {
        bool acted = false;
        using (var timer = new Timer())
        {
            timer.Interval = 50;
            timer.Tick += delegate
            {
                if (acted) return;
                foreach (Form form in Application.OpenForms)
                {
                    var registro = form as FrmAgregarTarea;
                    if (registro == null) continue;
                    acted = true;
                    Find<TextBox>(registro, "txtTitulo").Text = title;
                    Find<TextBox>(registro, "txtDescripcion").Text = description;
                    Find<Button>(registro, save ? "btnGuardar" : "btnCancelar").PerformClick();
                    break;
                }
            };
            timer.Start();
            Find<Button>(listado, "btnNuevaTarea").PerformClick();
            timer.Stop();
        }
        Check(acted, save ? "Navegación a registro y guardado" : "Navegación a registro y cancelación");
    }

    private static void CloseMessages()
    {
        EnumThreadWindows(GetCurrentThreadId(), delegate(IntPtr window, IntPtr parameter)
        {
            var name = new StringBuilder(100);
            GetClassName(window, name, name.Capacity);
            if (name.ToString() == "#32770")
            {
                EnumChildWindows(window, delegate(IntPtr child, IntPtr ignored)
                {
                    var text = new StringBuilder(4000);
                    GetWindowText(child, text, text.Capacity);
                    if (text.Length > 0) Messages.Add(text.ToString());
                    return true;
                }, IntPtr.Zero);
                PostMessage(window, 0x0111, new IntPtr(1), IntPtr.Zero);
            }
            return true;
        }, IntPtr.Zero);
    }

    private static void SaveImage(Form form, string name)
    {
        Application.DoEvents();
        using (var bitmap = new Bitmap(form.Width, form.Height))
        {
            form.DrawToBitmap(bitmap, new Rectangle(0, 0, bitmap.Width, bitmap.Height));
            bitmap.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, name), ImageFormat.Png);
        }
    }
}
