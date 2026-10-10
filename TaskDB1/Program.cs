using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using TaskDB1.Forms;
using TaskDB1.Data;

namespace TaskDB1
{
    internal static class Program
    {
        /// <summary>
        /// Punto de entrada principal para la aplicación.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            try
            {
                DatabaseConnection.Initialize();
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo preparar la base de datos. Verifica que SQL Server LocalDB esté instalado.\n\n" + ex.Message,
                    "TaskDB", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            Application.Run(new FrmAgregarTarea());
        }
    }
}
