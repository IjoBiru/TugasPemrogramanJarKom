using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;
using MovingObjectClient;

namespace BasicAsyncClient
{
    public partial class ClientForm : Form
    {
        private Socket clientSocket;
        private byte[] buffer;
        private byte[] buffer2;
        private MOClientForm1 _moClientForm1;

        public ClientForm()
        {
            InitializeComponent();
            _moClientForm1 = new MOClientForm1();
            _moClientForm1.Show();
        }

        private static void ShowErrorDialog(string message)
        {
            MessageBox.Show(message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void ReceiveCallback(IAsyncResult AR)
        {
            try
            {
                int received = clientSocket.EndReceive(AR);

                if (received == 0)
                {
                    return;
                }


                string message = Encoding.ASCII.GetString(buffer, 0 , received);
                if (TryParseRectangle(message, out Rectangle rect))
                {
                    _moClientForm1?.UpdateRectangle(rect);
                }
                Invoke((Action)delegate { Text = "Server says: " + message; });
                Console.WriteLine("Server says: " + message);
                // Start receiving data again.
                clientSocket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ReceiveCallback, null);
            }
            // Avoid Pokemon exception handling in cases like these.
            catch (SocketException ex)
            {
                ShowErrorDialog(ex.Message);
            }
            catch (ObjectDisposedException ex)
            {
                ShowErrorDialog(ex.Message);
            }
        }

        private void ConnectCallback(IAsyncResult AR)
        {
            try
            {
                clientSocket.EndConnect(AR);
                UpdateControlStates(true);
                buffer = new byte[clientSocket.ReceiveBufferSize];
                clientSocket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ReceiveCallback, null);
            }
            catch (SocketException ex)
            {
                ShowErrorDialog(ex.Message);
            }
            catch (ObjectDisposedException ex)
            {
                ShowErrorDialog(ex.Message);
            }
        }

        private void SendCallback(IAsyncResult AR)
        {
            try
            {
                clientSocket.EndSend(AR);
            }
            catch (SocketException ex)
            {
                ShowErrorDialog(ex.Message);
            }
            catch (ObjectDisposedException ex)
            {
                ShowErrorDialog(ex.Message);
            }
        }

        /// <summary>
        /// A thread safe way to enable the send button.
        /// </summary>
        private void UpdateControlStates(bool toggle)
        {
            Invoke((Action)delegate
            {
                buttonSend.Enabled = toggle;
                buttonConnect.Enabled = !toggle;
                labelIP.Visible = textBoxAddress.Visible = !toggle;
            });
        }

        private void buttonSend_Click(object sender, EventArgs e)
        {
            try
            {
                // Serialize the textBoxes text before sending.
                PersonPackage person = new PersonPackage(checkBoxMale.Checked, (ushort)numberBoxAge.Value,
                    textBoxEmployee.Text);
                byte[] buffer = person.ToByteArray();
                clientSocket.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, SendCallback, null);
            }
            catch (SocketException ex)
            {
                ShowErrorDialog(ex.Message);
                UpdateControlStates(false);
            }
            catch (ObjectDisposedException ex)
            {
                ShowErrorDialog(ex.Message);
                UpdateControlStates(false);
            }
        }

        private void buttonConnect_Click(object sender, EventArgs e)
        {
            try
            {
                clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                // Connect to the specified host.
                var endPoint = new IPEndPoint(IPAddress.Parse(textBoxAddress.Text), 3333);
                clientSocket.BeginConnect(endPoint, ConnectCallback, null);
            }
            catch (SocketException ex)
            {
                ShowErrorDialog(ex.Message);
            }
            catch (ObjectDisposedException ex)
            {
                ShowErrorDialog(ex.Message);
            }
        }


// Method to parse the Rectangle.ToString() format
        private bool TryParseRectangle(string input, out Rectangle rectangle)
        {
            rectangle = new Rectangle();

            try
            {
                // Remove the surrounding braces and split by commas
                // Expected format: {X=20,Y=20,Width=30,Height=30}
                string[] parts = input.Trim('{', '}').Split(',');

                // Create a dictionary to store the parsed values
                Dictionary<string, int> values = new Dictionary<string, int>();

                foreach (string part in parts)
                {
                    // Split each part by '='
                    string[] pair = part.Split('=');
                    if (pair.Length == 2 && int.TryParse(pair[1], out int value))
                    {
                        values[pair[0].Trim()] = value;
                    }
                }

                // Check if all required parts are present
                if (values.ContainsKey("X") && values.ContainsKey("Y") &&
                    values.ContainsKey("Width") && values.ContainsKey("Height"))
                {
                    // Construct the rectangle using parsed values
                    rectangle = new Rectangle(
                        values["X"],
                        values["Y"],
                        values["Width"],
                        values["Height"]
                    );
                    return true;
                }
            }
            catch
            {
                // Parsing failed
            }

            return false;
        }
    }
}