﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MovingObjectServer;

//add MovingObjects

namespace BasicAsyncServer
{
    public partial class ServerForm : Form
    {
        private Socket serverSocket;
        private List<Socket> clientSockets = new List<Socket>();        
        private byte[] buffer;
        private Form1 form = new Form1();
        private Rectangle formRect;

        public ServerForm()
        {
            InitializeComponent();
            StartServer();
            TestAsyc();
        }

        private static void ShowErrorDialog(string message)
        {
            MessageBox.Show(message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        /// <summary>
        /// Construct server socket and bind socket to all local network interfaces, then listen for connections
        /// with a backlog of 10. Which means there can only be 10 pending connections lined up in the TCP stack
        /// at a time. This does not mean the server can handle only 10 connections. The we begin accepting connections.
        /// Meaning if there are connections queued, then we should process them.
        /// </summary>
        private void StartServer()
        {
            try
            {
                form.Show();
                Console.WriteLine("Server started.");
                serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                serverSocket.Bind(new IPEndPoint(IPAddress.Any, 3333));
                serverSocket.Listen(10);
                serverSocket.BeginAccept(AcceptCallback, null);
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

        private void AcceptCallback(IAsyncResult AR)
        {
            try
            {
                Console.WriteLine("Client connected.");
                Socket handler = serverSocket.EndAccept(AR);
                clientSockets.Add(handler);

                buffer = new byte[handler.ReceiveBufferSize];

                // Send a message to the newly connected client.
                var sendData = Encoding.ASCII.GetBytes("Hello");
                handler.BeginSend(sendData, 0, sendData.Length, SocketFlags.None, SendCallback, handler);
                // Listen for client data.
                handler.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ReceiveCallback, handler);
                // Continue listening for clients.
                serverSocket.BeginAccept(AcceptCallback, null);
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
                Socket current = (Socket)AR.AsyncState;
                current.EndSend(AR);
                //clientSocket.EndSend(AR);
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

        private void ReceiveCallback(IAsyncResult AR)
        {
            try
            {
                // Socket exception will raise here when client closes, as this sample does not
                // demonstrate graceful disconnects for the sake of simplicity.
                Socket current = (Socket)AR.AsyncState;
                int received = current.EndReceive(AR);
                //int received = clientSocket.EndReceive(AR);

                if (received == 0)
                {
                    return;
                }

                // The received data is deserialized in the PersonPackage ctor.
                PersonPackage person = new PersonPackage(buffer);
                SubmitPersonToDataGrid(person);

                // Start receiving data again.
                //clientSocket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ReceiveCallback, null);
                current.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ReceiveCallback, current);
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

        /// <summary>
        /// Provides a thread safe way to add a row to the data grid.
        /// </summary>
        private void SubmitPersonToDataGrid(PersonPackage person)
        {
            Invoke((Action)delegate
            {
                dataGridView.Rows.Add(person.Name, person.Age, person.IsMale);
            });
        }

        private async void TestAsyc()
        {
            
            while (true)
            {
                formRect = form.GetRect();
                foreach (Socket client in clientSockets)
                {
                    // Send the rectangle data to the client.
                    var sendData = Encoding.ASCII.GetBytes(formRect.ToString());
                    client.BeginSend(sendData, 0, sendData.Length, SocketFlags.None, SendCallback, client);
                }
                await Task.Delay(100);
            }
        }
    }
}
