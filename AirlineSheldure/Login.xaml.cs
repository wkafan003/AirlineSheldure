using Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace AirlineSheldure
{
	/// <summary>
	/// Логика взаимодействия для Login.xaml
	/// </summary>
	public partial class Login : Window
	{
		public bool Sucsess;
        private Dictionary<string,string> _auth;
        private string _pkey = "YsODwo7CsMOWRTDDiRZX0LPStdGO0ZvSktKP";
        public Login()
		{
			InitializeComponent();
            _auth = new Dictionary<string, string>();
            
			try
			{
                string[] s = File.ReadAllLines("auth.txt").Where(s => s.Length != 0).ToArray();
                _auth["host"] = s[0];
                _auth["port"] = s[1];
                _auth["username"] = s[2];
                _auth["password"] = Decrypt(s[3], _pkey);
            }
			catch (Exception)
			{
                _auth["host"] = "127.0.0.1";
                _auth["port"] = "5432";
                _auth["username"] = "postgres";
                _auth["password"] = "";
            }
		}

		private void ButtonLogin_Click(object sender, RoutedEventArgs e)
		{
            _auth["host"] = TextBoxHost.Text;
            _auth["port"] = TextBoxPort.Text;
            _auth["username"] = TextBoxUser.Text;
            _auth["password"] = SecureStringToString(TextBoxPassword.SecurePassword);
            try
            {
                new ApplicationContext(_auth["host"], _auth["port"], _auth["username"], _auth["password"]);
            }
            catch (Exception ee)
            {
                MessageBox.Show("Ошибка подключения к базе данных. Проверьте доступность базы данных и правильность введенных данных!", "Ошибка.");
                return;
            }

            string s = "";
            s+= _auth["host"]+"\n"+ _auth["port"] + "\n"+_auth["username"] + "\n" + Encrypt(_auth["password"],_pkey);
            File.WriteAllText("auth.txt", s);
            Sucsess = true;
			Close();
		}
		private void ButtonExit_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}

        private static String SecureStringToString(SecureString value)
        {
            IntPtr valuePtr = IntPtr.Zero;
            try
            {
                valuePtr = Marshal.SecureStringToGlobalAllocUnicode(value);
                return Marshal.PtrToStringUni(valuePtr);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(valuePtr);
            }
        }
        public static string Encrypt(string clearText,string privateKey)
        {
            string EncryptionKey = privateKey;
            byte[] clearBytes = Encoding.Unicode.GetBytes(clearText);
            using (Aes encryptor = Aes.Create())
            {
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(clearBytes, 0, clearBytes.Length);
                        cs.Close();
                    }
                    clearText = Convert.ToBase64String(ms.ToArray());
                }
            }
            return clearText;
        }
        public static string Decrypt(string cipherText, string privateKey)
        {
            string EncryptionKey = privateKey;
            cipherText = cipherText.Replace(" ", "+");
            byte[] cipherBytes = Convert.FromBase64String(cipherText);
            using (Aes encryptor = Aes.Create())
            {
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(cipherBytes, 0, cipherBytes.Length);
                        cs.Close();
                    }
                    cipherText = Encoding.Unicode.GetString(ms.ToArray());
                }
            }
            return cipherText;
        }

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
            try
            {
                new ApplicationContext(_auth["host"], _auth["port"], _auth["username"], _auth["password"]);
                Sucsess = true;
                Close();
            }
			catch (Exception)
			{

			}
        }
	}
}
