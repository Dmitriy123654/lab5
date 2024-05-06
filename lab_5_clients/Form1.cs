using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices.ComTypes;

using System.Net;

namespace lab_5_clients
{
    public partial class Form1 : Form
    {
        TcpClient client;
        string ipAddress;
        int port;
        public Form1()
        {
            InitializeComponent();
            comboBox1.Enabled = false;
            buttonRestart.Click += new EventHandler(btn_Restart);
            btnSend.Click += new EventHandler(btnSend_Click);
            this.FormClosing += new FormClosingEventHandler(Form1_FormClosing);
        }

        private void GetProductListFromServer(string ipAddress, int port)
        {
            try
            {
                client = new TcpClient(ipAddress, port);
                NetworkStream stream = client.GetStream();

                byte[] data = new byte[1024];
                int? bytes = stream?.Read(data, 0, data.Length);
                if (bytes == null)
                    throw new Exception();
                string productList = Encoding.UTF8.GetString(data, 0, (int)bytes);

                if (productList.StartsWith("Ошибка"))
                {
                    // Обработка ошибки получения списка товаров
                    string errorMessage = productList.Substring(7); // Убираем префикс "Ошибка: "
                    throw new Exception();                
                }
                else
                {
                    string[] products = productList.Split(',');
                    comboBox1.Items.AddRange(products);

                    comboBox1.SelectedIndex = -1;
                }
            }
            catch (SocketException ex)
            {
                MessageBox.Show("Ошибка при подключении к серверу: " + ex.Message);
            }
            catch (IOException ex)
            {
                MessageBox.Show("Ошибка ввода-вывода при получении списка товаров: " + ex.Message);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось получить список товаров: Файл отсуствует на сервере ");
            }
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            try
            {
                string ipAddress = txtIPAddress.Text;
                int port;
                if (!IPAddress.TryParse(ipAddress, out IPAddress address))
                {
                    MessageBox.Show("Некорректный IP-адрес.");
                    return;
                }
                if (!int.TryParse(txtPort.Text, out port) || port < 0 || port > 65535)
                {
                    MessageBox.Show("Некорректный порт. Введите значение от 0 до 65535.");
                    return;
                }
                if (comboBox1.SelectedItem == null)
                {
                    MessageBox.Show("Пожалуйста, выберите товар из списка.");
                    return;
                }

                if (!IsServerAvailable(ipAddress))
                {
                    MessageBox.Show("Сервер с указанным IP-адресом недоступен.");
                    return;
                }

                string productName = comboBox1.SelectedItem.ToString();
                NetworkStream stream = client.GetStream();
                byte[] data = Encoding.UTF8.GetBytes(productName);
                stream.Write(data, 0, data.Length);

                data = new byte[1024];
                StringBuilder responseData = new StringBuilder();
                int bytesRead = stream.Read(data, 0, data.Length);
                responseData.Append(Encoding.UTF8.GetString(data, 0, bytesRead));
                lblResponse.Text = "Цена товара: " + responseData.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message);
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                if (client != null && client.Connected)
                {
                    NetworkStream stream = client.GetStream();
                    byte[] closingMessage = Encoding.UTF8.GetBytes("closing");
                    stream.Write(closingMessage, 0, closingMessage.Length);

                    client.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при закрытии формы: " + ex.Message);
            }
        }

        private void btn_Restart(object sender, EventArgs e)
        {
            try
            {
                // Проверяем, есть ли уже открытое соединение, и закрываем его, если есть
                if (client != null && client.Connected)
                {
                    client.GetStream().Close(); // Закрываем поток
                    client.Close(); // Закрываем клиент
                }

                // Очищаем ComboBox
                comboBox1.Items.Clear();

                // Затем продолжаем с новым соединением
                ipAddress = txtIPAddress.Text;
                if (!IPAddress.TryParse(ipAddress, out IPAddress address))
                {
                    comboBox1.Enabled = false;
                    MessageBox.Show("Некорректный IP-адрес.");
                    return;
                }
                if (!int.TryParse(txtPort.Text, out port) || port < 0 || port > 65535)
                {
                    comboBox1.Enabled = false;
                    MessageBox.Show("Некорректный порт. Введите значение от 0 до 65535.");
                    return;
                }

                using (client = new TcpClient())
                {
                    if (!IsServerAvailable(ipAddress))
                    {
                        comboBox1.Enabled = false;
                        MessageBox.Show("Сервер с указанным IP-адресом недоступен.");
                        return;
                    }
                    else
                    {
                        Console.WriteLine(11);
                        comboBox1.Enabled = true;
                        GetProductListFromServer(ipAddress, port);
                        Console.WriteLine(12);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message);
            }
        }



        private bool IsServerAvailable(string ipAddress)
        {
            Ping ping = new Ping();
            PingReply reply = ping.Send(ipAddress);
            return reply.Status == IPStatus.Success;
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void buttonRestart_Click(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }
    }
}
