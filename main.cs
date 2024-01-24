using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Linq;

class Program
{
    static Dictionary<string, bool> ActiveSessions = new Dictionary<string, bool>();
    static Dictionary<string, string> PlayerTokens = new Dictionary<string, string>();

    static void Main(string[] args)
    {
        Console.WriteLine("Creating HTTP Listener...");
        var listener = new HttpListener();
        Console.WriteLine("Added HTTP Listener...");
        listener.Prefixes.Add("http://localhost:8080/");
        Console.WriteLine("Added Prefixes...");
        listener.Start();
        Console.WriteLine("Starting...");

        Console.WriteLine("Listening for requests...");

        while (true)
        {
            var context = listener.GetContext();
            ThreadPool.QueueUserWorkItem(o => HandleRequest(context));
        }
    }

    static void HandleRequest(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;

        if (request.HttpMethod == "GET")
        {
            var path = request.Url.LocalPath;

            if (path == "/Login/")
            {
                var playerID = request.QueryString["playerID"];

                if (ActiveSessions.ContainsKey(playerID) && ActiveSessions[playerID])
                {
                    response.StatusCode = (int)HttpStatusCode.Conflict;
                    byte[] buffer = Encoding.UTF8.GetBytes("Session already active");
                    response.OutputStream.Write(buffer, 0, buffer.Length);
                }
                else
                {
                    if (!ContainsLetter(playerID))
                    {
                        if (AuthenticateWithDillAuth(playerID))
                        {
                            string token = GenerateToken(playerID);
                            PlayerTokens[playerID] = token;

                            ActiveSessions[playerID] = true;
                            response.StatusCode = (int)HttpStatusCode.OK;
                            byte[] buffer = Encoding.UTF8.GetBytes("Authentication successful");
                            response.OutputStream.Write(buffer, 0, buffer.Length);
                        }
                        else
                        {
                            response.StatusCode = (int)HttpStatusCode.Unauthorized;
                            byte[] buffer = Encoding.UTF8.GetBytes("Authentication failed");
                            response.OutputStream.Write(buffer, 0, buffer.Length);
                        }
                    }
                    else
                    {
                        response.StatusCode = (int)HttpStatusCode.BadRequest;
                        byte[] buffer = Encoding.UTF8.GetBytes("Player ID contains letters");
                        response.OutputStream.Write(buffer, 0, buffer.Length);
                    }
                }
            }
            else
            {
                response.StatusCode = (int)HttpStatusCode.NotFound;
                byte[] buffer = Encoding.UTF8.GetBytes("Not Found");
                response.OutputStream.Write(buffer, 0, buffer.Length);
            }
        }

        response.Close();
    }

    static bool AuthenticateWithDillAuth(string playerID)
    {
        return !ContainsLetter(playerID);
    }

    static string GenerateToken(string playerID)
    {
        string token = Guid.NewGuid().ToString("N");
    
        PlayerTokens[playerID] = token;
    
        return token;
    }

    static bool ContainsLetter(string value)
    {
        return !string.IsNullOrEmpty(value) && value.Any(char.IsLetter);
    }
    
}
