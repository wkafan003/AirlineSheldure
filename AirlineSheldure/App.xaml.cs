using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace AirlineSheldure
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
        private void ApplicationStart(object sender, StartupEventArgs e)
        {
            Login login = new Login();
            MainWindow window = new MainWindow();
            login.ShowDialog();
            if (login.Sucsess)
                window.Show();
            else
                window.Close();
        }
    }
}
