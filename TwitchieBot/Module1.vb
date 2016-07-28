Imports TwitchLib
Imports Newtonsoft.Json
Imports System.IO
Module Module1
    Dim Authentication As New TwitchAuth
    Dim Credentials
    Dim Config As New Configuration
    Dim WithEvents ChatClient As TwitchChatClient
    Sub Main()
        'Reading in Authentication Information.
        If File.Exists("Credentials.json") Then
            Authentication = JsonConvert.DeserializeObject(Of TwitchAuth)(File.ReadAllText("Credentials.json"))
        Else
            Console.WriteLine("Config File missing, please fill it out." & vbNewLine & "Generating and Writing...")
            File.WriteAllText("Credentials.json", JsonConvert.SerializeObject(Authentication, Formatting.Indented))
            Console.WriteLine("Config File Written. Please edit and restart.")
            Console.ReadKey()
            Environment.Exit(1)
        End If
        If Authentication.Edited = False Then
            Console.WriteLine("Please edit the config file and set Edited to True.")
            Console.ReadKey()
            Environment.Exit(1)
        End If

        If File.Exists("Config.json") Then
            Config = JsonConvert.DeserializeObject(Of Configuration)(File.ReadAllText("Config.json"))
        Else
            Console.WriteLine("Generating Configuration File for Commands...")
            File.WriteAllText("Config.json", JsonConvert.SerializeObject(Config, Formatting.Indented))
        End If

        Credentials = New ConnectionCredentials(ConnectionCredentials.ClientType.Chat, New TwitchIpAndPort(Authentication.twitchChannel, True), Authentication.twitchUser, Authentication.twitchOAuth)
        ChatClient = New TwitchChatClient(Authentication.twitchChannel, Credentials, "!")
        ChatClient.Connect()

        'We now have a functional Chat Client with Events!
        'Now lets make it so that stuff typed into the console gets sent to the channel!
        Do
            Dim Input As String = Console.ReadLine
            If Input.ToLower.StartsWith("host") Then
                If Config.Console_Commands.Host = True Then
                    Dim host = Input.Split(" ")
                    ChatClient.SendMessage("/host " & host(1))
                End If
            Else 'This must always be last.
                If Config.EnableConsoleChat = True Then
                    ChatClient.SendMessage(Console.ReadLine)
                    If Config.ChatLogger.Enabled = True Then
                        If File.Exists(Config.ChatLogger.FileName) Then
                            File.WriteAllText(Config.ChatLogger.FileName, File.ReadAllText(Config.ChatLogger.FileName & vbNewLine & Input))
                        End If
                    End If
                End If
            End If
        Loop
    End Sub

    Sub MessageRecieved(ByVal sender As Object, ByVal e As TwitchChatClient.OnMessageReceivedArgs) Handles ChatClient.OnMessageReceived
        Dim Formatted_Message As String = (TimeOfDay.ToLocalTime & " " & e.ChatMessage.DisplayName & ": " & e.ChatMessage.Message)
        Console.WriteLine(Formatted_Message)
        If Config.ChatLogger.Enabled = True Then
            If File.Exists(Config.ChatLogger.FileName) Then
                File.WriteAllText(Config.ChatLogger.FileName, File.ReadAllText(Config.ChatLogger.FileName & vbNewLine & Formatted_Message))
            End If
        End If
    End Sub

    Async Sub CommandRecieved(ByVal sender As Object, ByVal e As TwitchChatClient.OnCommandReceivedArgs) Handles ChatClient.OnCommandReceived
        Console.WriteLine("Recieved Command " & e.ChatMessage.Message & " from " & e.ChatMessage.DisplayName)
        If e.Command = "uptime" Then
            Dim uptime As TimeSpan = Await TwitchApi.GetUptime(Authentication.twitchChannel)
            ChatClient.SendMessage((String.Format("uptime: {0} days, {1} hours, {2} minutes, {3} seconds", uptime.Days, uptime.Hours, uptime.Minutes, uptime.Seconds)))
        End If
    End Sub

    Sub ConnectedToChat(ByVal sender As Object, ByVal e As TwitchChatClient.OnConnectedArgs) Handles ChatClient.OnConnected
        Console.WriteLine("Connected to " & Authentication.twitchChannel & "'s Twitch Chat.")
        'This does not have a configuration, since it only makes sense to have it.
    End Sub

    Sub UserJoined(ByVal sender As Object, ByVal e As TwitchChatClient.OnViewerJoinedArgs) Handles ChatClient.OnViewerJoined
        If Config.Console_Notifications.UserJoinAndLeave = True Then
            Console.WriteLine(e.Username & " has joined the channel.")
        End If
    End Sub

    Sub UserLeft(ByVal sender As Object, ByVal e As TwitchChatClient.OnViewerLeftArgs) Handles ChatClient.OnViewerLeft
        If Config.Chat_Notifications.AlertOnFollow = True Then
            Console.WriteLine(e.Username & " has left the channel.")
        End If
    End Sub

    Sub UserSubscribed(ByVal sender As Object, ByVal e As TwitchChatClient.OnNewSubscriberArgs) Handles ChatClient.OnNewSubscriber
        If Config.Chat_Notifications.AlertOnFollow = True Then
            ChatClient.SendMessage(e.Subscriber.Name & " just subscribed!")
        End If
    End Sub
#Region "Main Configuration File"
    Class TwitchAuth
        Public Property twitchUser As String
        Public Property twitchOAuth As String
        Public Property twitchChannel As String
        Public Property Edited As Boolean = False 'To make sure the user edited as instructed
    End Class
#End Region
#Region "Control Configuration"
    Class Configuration
        Public Property EnableConsoleChat As Boolean = False
        Public Property ChatLogger As New Chat_Log
        Public Property Console_Notifications As New Notifications_Console
        Public Property Console_Commands As New Commands_Console
        Public Property Chat_Notifications As New Notifications_Chat
        Public Property Chat_Commands As New Commands_Chat
    End Class

    Class Chat_log
        Public Property Enabled As Boolean = False
        Public Property FileName As String = "ChatLog.log"
    End Class

    Class Notifications_Console
        Public Property AlertOnFollow As Boolean = False
        Public Property UserJoinAndLeave As Boolean = False
    End Class
    Class Notifications_Chat
        Public Property AlertOnFollow As Boolean = False
    End Class
    Class Commands_Chat
        Public Property Uptime As Boolean = False
    End Class
    Class Commands_Console
        Public Property Host As Boolean = False
    End Class
#End Region
End Module
