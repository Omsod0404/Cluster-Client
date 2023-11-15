﻿using Cluster_Client.Core;
using Cluster_Client.Core.Events;
using Cluster_Client.View;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;
using System.Windows.Markup;
using System.Net;
using Cluster_Client.Model;
using System.Windows.Media.Imaging;
using System.Net.Mime;
using System.Windows.Automation.Peers;

namespace Cluster_Client.ViewModel
{
    public class VideoActionsViewModel : BaseViewModel
    {
        private CoreHandler _coreHandler;
        private ICommand _executeCloseWindowCommand;
        private ICommand _executeLoadVideoCommand;
        private ICommand _executePSLocalVideoCommand;
        private ICommand _executeSendVideoCommand;
        private bool _isConnected;
        private bool _isVideoLoaded;
        private Status _serverDisponibility;
        private int _clientsBefore;
        private string _messagesPath;
        private string _messages;
        private Grid _allContent;
        private UIElement _defaultContentMain;
        private List<RowDefinition> _defaultRowDefinitionsMain;
        private string _localVideo;
        private bool _isLocalVideoStop;
        private bool _isVideoSended;
        private UIElement _defaultContentRecived;
        private List<RowDefinition> _defaultRowDefinitionsRecived;

        public ICommand ExecuteCloseWindowCommand
        {
            get { return _executeCloseWindowCommand; }
        }
        public ICommand ExecuteLoadVideoCommand
        { 
            get { return _executeLoadVideoCommand; } 
        }
        public ICommand ExecutePSLocalVideoCommand
        {
            get { return _executePSLocalVideoCommand; }
        }
        public ICommand ExecuteSendVideoCommand
        {
            get { return _executeSendVideoCommand; }
        }
        public bool IsConnected
        {
            get { return _isConnected; }
            set
            {
                _isConnected = value;
                OnPropertyChanged(nameof(IsConnected));
            }
        }
        public bool IsVideoLoaded
        {
            get { return _isVideoLoaded; }
            set
            {
                _isVideoLoaded = value;
                OnPropertyChanged(nameof(IsVideoLoaded));
            }
        }
        public Status ServerDisponibility
        {
            get { return _serverDisponibility; }
            set
            {
                _serverDisponibility = value;
                OnPropertyChanged(nameof(ServerDisponibility));
            }
        }
        public int ClientsBefore
        {
            get { return _clientsBefore; }
            set
            {
                _clientsBefore = value;
                OnPropertyChanged(nameof(ClientsBefore));
            }
        }
        public string MessagesPath
        {
            get { return _messagesPath; }
            set
            {
                _messagesPath = value;
                OnPropertyChanged(nameof(MessagesPath));
            }
        }
        public string Messages
        {
            get { return _messages; }
            set
            {
                _messages = value;
                OnPropertyChanged(nameof(Messages));
            }
        }
        public string LocalVideo
        {
            get { return _localVideo; }
            set
            {
                _localVideo = value;
                OnPropertyChanged(nameof(LocalVideo));
            }
        }
        public bool IsLocalVideoStop
        {
            get { return _isLocalVideoStop; }
            set
            {
                _isLocalVideoStop = value;
                OnPropertyChanged(nameof(IsLocalVideoStop));
            }
        }
        public bool IsVideoSended
        {
            get { return _isVideoSended; }
            set
            {
                _isVideoSended = value;
                OnPropertyChanged(nameof(IsVideoSended));
            }
        }

        public VideoActionsViewModel(Grid allContent)
        {
            _isConnected = true;
            _coreHandler = CoreHandler.Instance;
            _executeCloseWindowCommand = new CommandViewModel(CloseWindowAction);
            _executeLoadVideoCommand = new CommandViewModel(LoadVideoAction);
            _executePSLocalVideoCommand = new CommandViewModel(PSLocalVideo);
            _executeSendVideoCommand = new CommandViewModel(SendVideo);
            _coreHandler.ConnectedStatusEvent += _coreHandler_ConnectedStatusEvent;
            _coreHandler.ServerDisponibilityEvent += _coreHandler_ServerDisponibilityEvent;
            _coreHandler.ClientsBeforeEvent += _coreHandler_ClientsBeforeEvent;
            _coreHandler.VideoLoadedEvent += _coreHandler_VideoLoadedEvent;
            _coreHandler.LocalVideoEvent += _coreHandler_LocalVideoEvent;
            _messagesPath = "Resources/Messages.xaml";
            _messages = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, MessagesPath);
            _allContent = allContent; //This is the grid of this view
            _defaultContentMain = allContent.Children.Cast<UIElement>().FirstOrDefault(); //This is the deafault content of the previus grid
            _defaultRowDefinitionsMain = allContent.RowDefinitions.ToList(); //This is the structure of the grid
            HandleContentChange(); //When this view is loaded, change its content
        }

        private void HandleContentChange()
        {
            //While client is waiting for a satus from server show the response message
            ResourceDictionary resourceDic;
            using (FileStream fs = new FileStream(Messages, FileMode.Open))
            {
                resourceDic = (ResourceDictionary)XamlReader.Load(fs);
            }
            TextBlock rMessage = (TextBlock)resourceDic["ResponseMessage"];
            //Show the message on the main grid
            ChangeGridContent(rMessage);
        }

        private void ChangeGridContent(UIElement newContent)
        {
            _allContent.Children.Clear();
            //If newContent is a message remove the row definitions of the main grid
            if (newContent is TextBlock textBlock)
            {
                _allContent.RowDefinitions.Clear();
                _allContent.RowDefinitions.Add(new RowDefinition());
                Grid.SetRow(textBlock, 0);
                _allContent.Children.Add(textBlock);
            }
            else
            //if newContent is the deafault content, rebuild its row definitions
            {
                _allContent.RowDefinitions.Clear();
                foreach (var rowDefinition in _defaultRowDefinitionsMain)
                {
                    _allContent.RowDefinitions.Add(rowDefinition);
                }
                _allContent.Children.Add(newContent);
            }
        }

        private void _coreHandler_LocalVideoEvent(object? sender, LocalVideoEventArgs e)
        {
            //Event to show the local video on its media element
            LocalVideo = e.LocalVideoPath;
            ShowLocalVideo(LocalVideo);
        }

        private void ShowLocalVideo(string localVideoPath)
        {
            MediaElement mediaElementVideoLoaded = (MediaElement)Application.Current.MainWindow.FindName("mediaElementVideoLoaded");
            Uri uriLocalVideo = new Uri(localVideoPath);
            mediaElementVideoLoaded.Source = uriLocalVideo;
            mediaElementVideoLoaded.LoadedBehavior = MediaState.Manual;
            mediaElementVideoLoaded.LoadedBehavior = MediaState.Play;
            IsLocalVideoStop = false;
            mediaElementVideoLoaded.MediaEnded += MediaElementVideoLoaded_MediaEnded;
        }

        public void MediaElementVideoLoaded_MediaEnded(object sender, RoutedEventArgs e)
        {
            MediaElement mediaElementVideoLoaded = (MediaElement)sender;
            mediaElementVideoLoaded.LoadedBehavior = MediaState.Close;
            mediaElementVideoLoaded.LoadedBehavior = MediaState.Play;
            IsLocalVideoStop = false;
        }

        private void _coreHandler_VideoLoadedEvent(object? sender, VideoLoadedEventArgs e)
        {
            IsVideoLoaded = e.IsVideoLoaded;
            //If local video is loaded, show its control buttons
            if (IsVideoLoaded)
            {
                Button btnPSbtnPSLoadVideo = (Button)Application.Current.MainWindow.FindName("btnPSLoadVideo");
                btnPSbtnPSLoadVideo.Visibility = Visibility.Visible;
                Button btnSendVideo = (Button)Application.Current.MainWindow.FindName("btnSendVideo");
                btnSendVideo.Visibility = Visibility.Visible;
            }
        }

        private void _coreHandler_ConnectedStatusEvent(object? sender, ConnectedStatusEventArgs e)
        {
            IsConnected = e.Connected;
        }

        private void _coreHandler_ClientsBeforeEvent(object? sender, ClientsBeforeEventArgs e)
        {
            ClientsBefore = e.clientsBefore; //Number of clients before me
        }

        private void _coreHandler_ServerDisponibilityEvent(object? sender, ServerDisponibilityEventArgs e)
        {
            ServerDisponibility = e.serverDisponibility;
            //Open the messages resource
            ResourceDictionary resourceDic;
            using (FileStream fs = new FileStream(Messages, FileMode.Open))
            {
                resourceDic = (ResourceDictionary)XamlReader.Load(fs);
            }
            TextBlock message;
            switch (ServerDisponibility)
            {
                case Status.Waiting:
                    message = (TextBlock)resourceDic["WaitingMessage"];
                    ChangeGridContent(message);
                    break;

                case Status.Busy:
                    message = (TextBlock)resourceDic["BusyMessage"];
                    ChangeGridContent(message);
                    break;
                case Status.Ready:
                    ChangeGridContent(_defaultContentMain);
                    break;
            }
        }

        private void CloseWindowAction(object sender)
        {
            if (IsConnected)
            {
                CoreHandler.Instance.StopClientAsync();
            }
            Application.Current.Shutdown();
        }

        private void LoadVideoAction(object sender)
        {
            CoreHandler.Instance.LoadVideo();
        }

        private void SendVideo(object sender)
        {
            CoreHandler.Instance.SendVideo();
        }

        private void PSLocalVideo(object sender)
        {
            MediaElement mediaElementVideoLoaded = (MediaElement)Application.Current.MainWindow.FindName("mediaElementVideoLoaded");
            if (IsLocalVideoStop)
            {
                mediaElementVideoLoaded.LoadedBehavior = MediaState.Play;
                IsLocalVideoStop = false;
            }
            else
            {
                mediaElementVideoLoaded.LoadedBehavior = MediaState.Pause;
                IsLocalVideoStop = true;
            }
        }

        private void _coreHandler_IsVideoSendedEvent(object sender, IsVideoSendedEventArgs e) 
        {
            IsVideoSended = e.IsVideoSended;
            if(IsVideoSended)
            {
                Grid gridRecived = (Grid)Application.Current.MainWindow.FindName("receiveVideoContainer");
                _defaultContentRecived = gridRecived.Children.Cast<UIElement>().FirstOrDefault();
                _defaultRowDefinitionsRecived = gridRecived.RowDefinitions.ToList();

                using (FileStream fs = new FileStream(Messages, FileMode.Open))
                {
                    ResourceDictionary resourceDic = (ResourceDictionary)XamlReader.Load(fs); 
                    TextBlock sendMessage = (TextBlock)resourceDic["WaitingVideoMessage"]; 
                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        gridRecived.RowDefinitions.Clear();
                        gridRecived.RowDefinitions.Add(new RowDefinition());
                        Grid.SetRow(sendMessage, 0);
                        gridRecived.Children.Add(sendMessage);
                    }));
                }
                gridRecived.Visibility = Visibility.Visible;
            }
        }
    }
}