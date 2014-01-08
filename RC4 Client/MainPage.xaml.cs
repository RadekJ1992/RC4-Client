using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using RC4_Client.Resources;

namespace RC4_Client
{
    /// <summary>
    /// Klasa głównego okna aplikacji
    /// </summary>
    public partial class MainPage : PhoneApplicationPage
    {
        const int REMOTE_PORT = 10100;
        int ckey;
        public static SocketClient client;
        bool connected;
        /// <summary>
        /// Obsługa wciśnięcia przycisku wysyłania wiadomości
        /// </summary>
        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            // Sprawdzenie czy pola "Key" i "PlainText" są wypełnione
            if (ValidateInput())
            {
                if (connected)
                {
                    string result = client.Send("run\n");
                    Log(result, false);
                    // Zaszyfruj klucz
                    string encryptedKey = Cshift(keyInput.Text, ckey);
                    Log(String.Format("Sending key " + keyInput.Text + " (" +encryptedKey + ") and text " + txtInput.Text + " to server"), true);
                    result = client.Send(encryptedKey + "\n");
                    Log(result, false);
                    client.Send(txtInput.Text + "\n");
                    Log("Sent!", true);
                }
                else
                {
                    Log("Not connected!", false);
                }
            }

        }
        /// <summary>
        /// Obsługa wciśnięcia przycisku logowania się na serwerze
        /// </summary>
        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            if (!connected)
            {
                ClearLog();
                client = new SocketClient();
                Log(String.Format("Connecting to server encryptserver.cloudapp.net over port 10100 ..."), true);
                client.Connect("encryptserver.cloudapp.net", REMOTE_PORT);
                string result = client.Send("client\n");
                Log(result, false);
                if (result == "Success")
                {
                    connected = true;
                    btnLogin.Content = "Log out!";
                    String caesarkey = client.Receive();
                    Log("Caesar Cipher key received: " + caesarkey, false);
                    ckey = int.Parse(caesarkey);
                }
                else
                {
                    connected = false;
                    btnLogin.Content = "Log in!";
                }
            }
            else
            {
                client.Send("quit\n");
                ClearLog();
                Log("Logged out!", false);
                connected = false;
                btnLogin.Content = "Log in!";
                client = null;
            }
        }
        /// <summary>
        /// Klasa szyfrująca tekst szyfrem Cezara
        /// </summary>
        /// <param name="str">tekst do zaszyfrowania</param>
        /// <param name="shift">klucz</param>
        /// <returns>zaszyfrowany tekst</returns>
        public string Cshift(string str, int shift)
        {
            string output = null;
            char[] text = null;
            text = str.ToCharArray();
            int temp;

            for (int i = 0; i < str.Length; i++)
            {
                temp = (int)(text[i] + shift);
                output += (char)temp;
            }
            return output;
        }

        /// <summary>
        /// Sprawdzenie zawartości pól "Key" i "PlainText"
        /// </summary>
        /// <returns>True gdy są wypeełnione, false gdy nie są 
        ///</returns>
        private bool ValidateInput()
        {
            if (String.IsNullOrWhiteSpace(txtInput.Text) && String.IsNullOrWhiteSpace(keyInput.Text))
            {
                MessageBox.Show("Please enter key and plain text");
                return false;
            }

            return true;
        }
        /// <summary>
        /// Metoda wypisująca komunikaty w polu tekstowym
        /// </summary>
        /// <param name="message">Wiadomość do wyświetlenia</param>
        /// <param name="isOutgoing">True gdy z klienta do serwera
        /// False gdy z serwera do klienta
        /// </param>
        private void Log(string message, bool isOutgoing)
        {
            string direction = (isOutgoing) ? ">> " : "<< ";
            txtOutput.Text += Environment.NewLine + direction + message;
        }

        /// <summary>
        /// Wyczyszczenie pola tekstowego
        /// </summary>
        private void ClearLog()
        {
            txtOutput.Text = String.Empty;
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        public MainPage()
        {
            InitializeComponent();
            connected = false;
        }
        /// <summary>
        /// Metoda wysyłająca wiadomość do serwera i wylogowująca się
        /// </summary>
        public static void logOut()
        {
            if (client != null)
            {
                client.Send("quit\n");
            }
        }
        
    }
}