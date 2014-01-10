using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using RC4_Client.Resources;
using System.Text;

namespace RC4_Client
{
    /// <summary>
    /// Klasa głównego okna aplikacji
    /// </summary>
    public partial class MainPage : PhoneApplicationPage
    {
        const int REMOTE_PORT = 10100;
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
                    //Zaszyfruj klucz
                    AesManaged myAes = new AesManaged();
                    String AESKey = "SuperTajnyKlucz1"; //128 bit
                    myAes.Key = Encoding.UTF8.GetBytes(AESKey);
                    String AESIV = "SuperTajnyWektor"; //128 bit
                    myAes.IV = Encoding.UTF8.GetBytes(AESIV);
                    // Koduje klucz jako ciąg bitów
                    byte[] encrypted = EncryptStringToBytes_Aes(keyInput.Text, myAes.Key, myAes.IV);

                    // Przekształca ciąg bitów na string by go wysłać łatwiej było
                    //string encryptedKey = Encoding.UTF8.GetString(encrypted, 0, encrypted.Length);
                    string encryptedKey = ByteArrayToString(encrypted);
                    result = client.Send(encryptedKey + "\n");
                    Log(result, false);

                    if (!asHexCheckbox.IsChecked ?? false)
                    {
                        client.Send("text\n");
                        Log(String.Format("Sending key : " + keyInput.Text + " as \n" + encryptedKey + " \nand text :\n" + txtInput.Text + " \nto server"), true);
                        client.Send(txtInput.Text + "\n");
                        Log("Sent!", true);
                    }
                    else
                    {                   
                        try
                        {
                            byte[] test = StringToByteArray(txtInput.Text);
                            client.Send("hex\n");
                            Log(String.Format("Sending key : " + keyInput.Text + " as \n" + encryptedKey + " \nand hex string :\n" + txtInput.Text + " \nto server"), true);
                            client.Send(txtInput.Text + "\n");
                            Log("Sent!", true);
                        }
                        catch
                        {
                            Log("Wrong hex format! Sending as text!", false);
                            client.Send("text\n");
                            Log(String.Format("Sending key : " + keyInput.Text + " as \n" + encryptedKey + " \nand text :\n" + txtInput.Text + " \nto server"), true);
                            client.Send(txtInput.Text + "\n");
                            Log("Sent!", true);
                        }
                    }
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
                    //String caesarkey = client.Receive();
                    //Log("Caesar Cipher key received: " + caesarkey, false);
                    //ckey = int.Parse(caesarkey);
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
        /// Metoda szyfrująca dany string z wykorzystaniem AES
        /// </summary>
        /// <param name="plainText">tekst to zaszysfrowania</param>
        /// <param name="Key">klucz AES (128, 192 lub 256 bit)</param>
        /// <param name="IV">wektor początkowy (128 bit)</param>
        /// <returns>tablica bajtów zaszyfrowanej wiadomości</returns>
        static byte[] EncryptStringToBytes_Aes(string plainText, byte[] Key, byte[] IV)
        {
            // sprawdź argumenty
            if (plainText == null || plainText.Length <= 0)
                throw new ArgumentNullException("plainText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("Key");
            byte[] encrypted;
            // Tworzenie obiektu AESManaged z danym kluczem i wektorem początkowym
            using (AesManaged aesAlg = new AesManaged())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;

                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(plainText);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }
            }
            return encrypted;
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
        /// Metoda zmieniająca tablicę bajtów na string będący zapisem tej tablicy w formacie hex
        /// </summary>
        /// <param name="ba">Podana tablica bajtów</param>
        /// <returns>String będący wynikiem działania metody</returns>
        public string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
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
            asHexCheckbox.IsChecked = false;
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
        /// <summary>
        /// Metoda zmieniająca string będący zapisem bajtów w formacie hex na tablicę bajtów
        /// </summary>
        /// <param name="hex">String wejściowy</param>
        /// <returns>tablica bajtów będąca wynikiem działania metody</returns>
        public byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        public static string DecryptStringFromBytes_Aes(byte[] cipherText, byte[] Key, byte[] IV)
        {
            // Sprawdź argumenty 
            if (cipherText == null || cipherText.Length <= 0)
                throw new ArgumentNullException("cipherText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("Key");

            string plaintext = null;

            // Tworzenie obiektu AESManaged z danym kluczem i wektorem początkowym
            using (AesManaged aesAlg = new AesManaged())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            plaintext = srDecrypt.ReadToEnd();
                        }
                    }
                }
            }
            return plaintext;
        }






    }
}