using System;
using System.Data;
using System.Windows.Forms;
using System.Data.SqlClient;
using TaskDB1.Data;

namespace TaskDB1.Forms
{
    public partial class FrmListadoTareas : Form
    {
        private bool cargandoEstados;
        private bool completando;
        public FrmListadoTareas()
        {
            InitializeComponent();
        }

        private void FrmListadoTareas_Load(object sender, EventArgs e)
        {
            CargarEstados();
            ConfigurarDataGridView();
            CargarTareas();
        }

        private void CargarEstados()
        {
            cargandoEstados = true;
            cmbEstado.Items.Clear();

            cmbEstado.Items.Add("Todas");
            cmbEstado.Items.Add("Pendiente");
            cmbEstado.Items.Add("Completada");

            cmbEstado.SelectedIndex = 0;
            cargandoEstados = false;
        }

        private void ConfigurarDataGridView()
        {
            dgvTareas.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvTareas.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvTareas.MultiSelect = false;
            dgvTareas.ReadOnly = true;
            dgvTareas.AllowUserToAddRows = false;
            dgvTareas.AllowUserToDeleteRows = false;
            dgvTareas.RowHeadersVisible = false;
        }

        private void CargarTareas()
        {
            try
            {
                using (var connection = DatabaseConnection.GetConnection())
                using (var command = new SqlCommand(
                    "SELECT Id, Titulo, Descripcion, Estado, FechaCreacion FROM dbo.Tareas " +
                    "WHERE (@Estado IS NULL OR Estado = @Estado) ORDER BY FechaCreacion DESC, Id DESC", connection))
                using (var adapter = new SqlDataAdapter(command))
                {
                    string estado = cmbEstado.SelectedItem as string;
                    command.Parameters.Add("@Estado", SqlDbType.NVarChar, 10).Value =
                        estado == "Pendiente" || estado == "Completada" ? (object)estado : DBNull.Value;
                    var table = new DataTable();
                    adapter.Fill(table);
                    var previousTable = dgvTareas.DataSource as DataTable;
                    dgvTareas.DataSource = table;
                    if (previousTable != null)
                        previousTable.Dispose();

                    dgvTareas.Columns["Id"].HeaderText = "ID";
                    dgvTareas.Columns["Id"].FillWeight = 8;
                    dgvTareas.Columns["Titulo"].HeaderText = "Título";
                    dgvTareas.Columns["Titulo"].FillWeight = 22;
                    dgvTareas.Columns["Descripcion"].HeaderText = "Descripción";
                    dgvTareas.Columns["Descripcion"].FillWeight = 30;
                    dgvTareas.Columns["Estado"].HeaderText = "Estado";
                    dgvTareas.Columns["Estado"].FillWeight = 15;
                    dgvTareas.Columns["FechaCreacion"].HeaderText = "Fecha de creación";
                    dgvTareas.Columns["FechaCreacion"].FillWeight = 25;
                    dgvTareas.Columns["FechaCreacion"].DefaultCellStyle.Format = "yyyy-MM-dd HH:mm";
                    dgvTareas.CurrentCell = null;
                    dgvTareas.ClearSelection();
                }
            }
            catch (Exception ex)
            {
                var previousTable = dgvTareas.DataSource as DataTable;
                dgvTareas.DataSource = null;
                if (previousTable != null)
                    previousTable.Dispose();
                MessageBox.Show(this, "No se pudieron cargar las tareas.\n\n" + ex.Message, "TaskDB",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnFiltrar_Click(object sender, EventArgs e)
        {
            CargarTareas();
        }

        private void cmbEstado_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!cargandoEstados && IsHandleCreated)
                CargarTareas();
        }

        private void btnNuevaTarea_Click(object sender, EventArgs e)
        {
            using (var form = new FrmAgregarTarea())
                form.ShowDialog(this);
            CargarTareas();
        }

        private void btnCompletar_Click(object sender, EventArgs e)
        {
            if (completando)
                return;
            if (dgvTareas.SelectedRows.Count == 0)
            {
                MessageBox.Show(this, "Selecciona una tarea para marcarla como completada.", "TaskDB",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int id = Convert.ToInt32(dgvTareas.SelectedRows[0].Cells["Id"].Value);
            completando = true;
            btnCompletar.Enabled = false;
            try
            {
                int updated;
                using (var connection = DatabaseConnection.GetConnection())
                using (var command = new SqlCommand(
                    "UPDATE dbo.Tareas SET Estado = @Estado WHERE Id = @Id AND Estado <> @Estado", connection))
                {
                    command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                    command.Parameters.Add("@Estado", SqlDbType.NVarChar, 10).Value = "Completada";
                    connection.Open();
                    updated = command.ExecuteNonQuery();
                }

                MessageBox.Show(this, updated == 1 ? "La tarea se marcó como completada." :
                    "La tarea ya está completada o ya no está disponible.", "TaskDB",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                CargarTareas();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "No se pudo completar la tarea.\n\n" + ex.Message, "TaskDB",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                completando = false;
                btnCompletar.Enabled = true;
            }
        }
    }
}
