using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

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
        }

        private void CargarEstados()
        {
            cboEstado.Items.Clear();

            cboEstado.Items.Add("Todas");
            cboEstado.Items.Add("Pendiente");
            cboEstado.Items.Add("Completada");

            cboEstado.SelectedIndex = 0;
        }

        private void ConfigurarDataGridView()
        {
            dgvTareas.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvTareas.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvTareas.MultiSelect = false;
            dgvTareas.ReadOnly = true;
            dgvTareas.AllowUserToAddRows = false;
            dgvTareas.AllowUserToDeleteRows = false;
        }
    }
}
