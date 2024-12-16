using ChatLibrary;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace ChatForm
{
    [CallbackBehavior(ConcurrencyMode = ConcurrencyMode.Reentrant)]
    public partial class Form1 : Form, IChatCallback
    {
        private IChatService _chatService;
        private DuplexChannelFactory<IChatService> _factory;
        private string _username;
        private string _selectedUser;

        public Form1()
        {
            InitializeComponent();
        }

        private async Task ConnectToServerAsync()
        {
            try
            {
                _factory = new DuplexChannelFactory<IChatService>(this, new NetTcpBinding(),
                    new EndpointAddress("net.tcp://localhost:8000/ChatService"));
                _chatService = _factory.CreateChannel();

                var connected = await _chatService.ConnectAsync(_username);

                if (connected)
                {
                    ChatBox.AppendText("Connected to chat." + Environment.NewLine);
                    ConnectButton.Enabled = false;
                    UsernameBox.Enabled = false;
                }
                else
                {
                    ChatBox.AppendText("Failed to connect." + Environment.NewLine);
                }
            }
            catch
            {
                ChatBox.AppendText("Failed to connect." + Environment.NewLine);
            }
        }

        public void ReceiveMessage(string message)
        {
            ChatBox.Invoke(new MethodInvoker(() => ChatBox.Text += message + Environment.NewLine));
        }

        public void UpdateUserList(List<string> users)
        {
            if (OnlineUsers.InvokeRequired)
            {
                OnlineUsers.Invoke(new Action(() =>
                {
                    OnlineUsers.Items.Clear();
                    foreach (var user in users)
                    {
                        OnlineUsers.Items.Add(user);
                    }
                }));
            }
            else
            {
                OnlineUsers.Items.Clear();
                foreach (var user in users)
                {
                    OnlineUsers.Items.Add(user);
                }
            }
        }
        private async void ConnectButton_Click(object sender, EventArgs e)
        {
            _username = UsernameBox.Text;
            await ConnectToServerAsync();
        }

        private async void SendButton_Click(object sender, EventArgs e)
        {
            var message = Message.Text;
            var currentTime = DateTime.Now.ToString("HH:mm");
    
            if (!string.IsNullOrEmpty(_selectedUser) && _selectedUser != _username)
            {
                ChatBox.AppendText($"[{currentTime}]");
                await _chatService.SendPrivateMessageAsync(message, _selectedUser, _username);
                string newMess = $"[You -> {_selectedUser}]: {message}";
                ChatBox.AppendText(newMess + Environment.NewLine);

                int my1stPosition = ChatBox.Text.LastIndexOf(newMess);

                if (my1stPosition != -1)
                {
                    ChatBox.SelectionStart = my1stPosition;
                    ChatBox.SelectionLength = newMess.Length;
                    ChatBox.SelectionColor = Color.Red;
                }

            }
            else
            {
                await _chatService.SendMessageToGroupAsync(message, _username);
                string newMess = $"{_username}: {message}";
                
                int my1stPosition = ChatBox.Text.LastIndexOf(newMess);
                if (my1stPosition != -1)
                {
                    ChatBox.SelectionStart = my1stPosition;
                    ChatBox.SelectionLength = newMess.Length;
                    ChatBox.SelectionColor = Color.LightGreen; 
                }

            }
            ChatBox.SelectionStart = ChatBox.Text.Length;
            ChatBox.ScrollToCaret();
            Message.Clear();
        }
        private async void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_chatService != null && !string.IsNullOrEmpty(_username))
            {
                await _chatService.DisconnectAsync(_username);
            }
        }
        private void OnlineUsers_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (OnlineUsers.SelectedItem != null)
            {
                if (_selectedUser != null && _selectedUser == OnlineUsers.SelectedItem.ToString())
                {
                    OnlineUsers.ClearSelected();
                    _selectedUser = null; 
                }
                else
                {
                    _selectedUser = OnlineUsers.SelectedItem.ToString();
                }
            }
            else
            {
                _selectedUser = null;
            }
        }
    }
}
