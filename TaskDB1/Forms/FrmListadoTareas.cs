using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SqlClient;
using TaskDB1.Data;

namespace TaskDB1.Forms
{
    public partial class FrmListadoTareas : Form
    {
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
            cmbEstado.Items.Clear();

            cmbEstado.Items.Add("Todas");
            cmbEstado.Items.Add("Pendiente");
            cmbEstado.Items.Add("Completada");

            cmbEstado.SelectedIndex = 0;
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
                    "SELECT Id, Titulo, Descripcion, Estado, FechaCreacion FROM dbo.Tareas ORDER BY FechaCreacion DESC, Id DESC", connection))
                using (var adapter = new SqlDataAdapter(command))
                {
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
    }
}
