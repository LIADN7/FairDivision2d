using System;

[Serializable]
public class ChatMessage
{
    public string Message { get; private set; }
    public string PlayerName { get; private set; }
    public DateTime Date { get; private set; }

    public ChatMessage(string playerName, string message)
    {

        Message = message;
        PlayerName = playerName;
        Date = DateTime.Now;
    }

    public override string ToString()
    {
        string time = Date.ToString("HH:mm:ss");
        return "[" + time + ", "+ PlayerName + "] " + Message;
    }
}
