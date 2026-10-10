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
    public partial class FrmAgregarTarea : Form
    {
        private bool guardando;
        public FrmAgregarTarea()
        {
            InitializeComponent();
        }

        private void FrmAgregarTarea_Load(object sender, EventArgs e)
        {
            txtTitulo.Focus();
        }
        private bool ValidarTitulo()
        {
            if (string.IsNullOrWhiteSpace(txtTitulo.Text))
            {
                MessageBox.Show(
                    "El título de la tarea es obligatorio.",
                    "Validación",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );

                txtTitulo.Focus();
                return false;
            }

            return true;
        }

        private void btnGuardar_Click(object sender, EventArgs e)
        {
            if (guardando || !ValidarTitulo())
                return;

            guardando = true;
            btnGuardar.Enabled = false;
            try
            {
                using (var connection = DatabaseConnection.GetConnection())
                using (var command = new SqlCommand(
                    "INSERT INTO dbo.Tareas (Titulo, Descripcion, Estado) VALUES (@Titulo, @Descripcion, @Estado)", connection))
                {
                    command.Parameters.Add("@Titulo", SqlDbType.NVarChar, 200).Value = txtTitulo.Text.Trim();
                    command.Parameters.Add("@Descripcion", SqlDbType.NVarChar, 2000).Value = txtDescripcion.Text.Trim();
                    command.Parameters.Add("@Estado", SqlDbType.NVarChar, 10).Value = "Pendiente";
                    connection.Open();
                    command.ExecuteNonQuery();
                }

                MessageBox.Show(this, "La tarea se guardó correctamente.", "TaskDB",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "No se pudo guardar la tarea.\n\n" + ex.Message, "TaskDB",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                guardando = false;
                btnGuardar.Enabled = true;
            }
        }

        private void btnCancelar_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
