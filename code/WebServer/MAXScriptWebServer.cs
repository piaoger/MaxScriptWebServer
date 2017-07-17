using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Web;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

/// Autodesk DLLs
using Autodesk.Max;
using UiViewModels.Actions;

namespace MAXScriptWebServer
{
    /// <summary>
    /// A 3ds Max user action launches a web-server locally that will execute MAXScript requests sent via an HTML form.
    /// Place the DLL in 3ds Max 2013/bin/assemblies. Start 3ds Max 2013 in administrator mode (very important!)
    /// When 3ds Max 2013 starts up use the "customize" menu to associate the action with a menu or short-cut key.
    /// When you trigger the action, 3ds Max will start a small web server using HttpListener, that will process
    /// requests and returns HTML responses.
    /// <list type="bullet">
    /// <item>http://blog.mikehacker.net/2006/11/13/httplistener-and-forms/</item>
    /// <item>http://stackoverflow.com/questions/8637856/httplistener-with-post-data</item>
    /// <item>http://msdn.microsoft.com/en-us/library/3x8d2eck(v=vs.100).aspx</item>
    /// </list>
    /// </summary>
    public class MAXScriptWebServer : CuiActionCommandAdapter
    {
        /// <summary>
        /// Used to executed code on the main thread, even if the web-server is listening on a
        /// separate thread.
        /// </summary>
        static Dispatcher dispatcher;

        /// <summary>
        /// Listen to port 8080 by default
        /// </summary>
        static string port = "8080";

        /// <summary>
        /// The HTML sent back from 3ds Max to the web client.
        /// </summary>
        const string HtmlResponseTextTemplate = @"
<!DOCTYPE HTML PUBLIC ""-//W3C//DTD HTML 4.0 Transitional//EN"">
<html>
	<head>
		<title>MAXScript Web Server</title>
	</head>
	<body>
    <h1>Welcome to the MAXScript web server</h1>
    <p>Type in your MAXScript code here:</p>
    <form action=""http://localhost:{{PORT}}/"" method=""post"">
        <textarea rows=""4"" cols=""50"" name=""code"">
-- Some sample MAXScript code
sphere radius:5 pos:[0, 0, 0]
sphere radius:5 pos:[10, 20, 0]
sphere radius:5 pos:[20, 0, 0]
sphere radius:5 pos:[10, 10, 20]
        </textarea>
        <input type=""submit"" value=""Submit"">
    </form>
    <form action=""http://localhost:{{PORT}}/exit"">
        <input type=""submit"" value=""Kill web server"">
    </form>
    <p>Your previous request was:</p>
    <p>
        <textarea rows=""4"" cols=""50"">
{{REQUEST}}
        </textarea>
    </p>
	</body>
</html>";

        /// <summary>
        /// The web server
        /// </summary>
        HttpListener listener;

        /// <summary>
        /// Our hook into the 3ds Max .NET API
        /// </summary>
        IGlobal global;

        /// <summary>
        /// Returns the posted form data.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static string GetFormData(HttpListenerRequest request, string formName)
        {
            if (request == null || !request.HasEntityBody)
                return "";
            var body = request.InputStream;
            var encoding = request.ContentEncoding;
            var reader = new System.IO.StreamReader(body, encoding);
            var text = reader.ReadToEnd();
            body.Close();
            reader.Close();
            var queryVars = HttpUtility.ParseQueryString(text, encoding);
            return queryVars[formName];
        }

        /// <summary>
        /// Web request handling loop. Terminated when the user
        /// makes a request to http://localhost:{{port}}/exit
        /// </summary>
        public void ListenLoop()
        {
            try
            {
                while (listener.IsListening)
                {
                    var context = listener.GetContext();
                    var request = context.Request;
                    var url = request.Url;

                    // Check for the special shutdown URL http://localhost:8080/exit
                    if (url.PathAndQuery == "/exit")
                    {
                        string res = BuildResponseText(0, "success", "Shutting down!");
                        WriteResponse(context, res);
                        listener.Stop();
                    }
                    else if (context != null && listener.IsListening)
                    {
                        if (url.PathAndQuery == "/healthz")
                        {
                            string res = BuildResponseText(0, "success", "The service is healthy");
                            WriteResponse(context, res);
                        }
                        else
                        {
                            // Get the HTML request text from the form.
                            // This should be pure MAXScript
                            var code = GetFormData(request, "code");

                            // We return the HTML constant string, embedding the request body.
                            var responseText = HtmlResponseTextTemplate.Replace("{{PORT}}", port);
                            responseText = responseText.Replace("{{REQUEST}}", code);
                            WriteResponse(context, responseText);

                            // Execute the evaluation of the code as MAXScript on the main thread.
                            Action a = () => global.ExecuteMAXScriptScript(code, false, null);
                            dispatcher.Invoke(a);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }

        /// <summary>
        /// build response text.
        /// </summary>
        /// <param name="errcode"></param>
        /// <param name="msg"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public string BuildResponseText(int errcode, string msg, string data)
        {
            string strErrCode = errcode.ToString();
            string res = "{\"errcode\":" + strErrCode + "," +
                         "\"msg\":" + "\"" + msg + "\"" + "," +
                         "\"data\":{" +
                         "\"res\":" + "\"" +data + "\"" + "}}";
            return res;
        }

        /// <summary>
        /// Sends text back to the web-client as text.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="s"></param>
        public void WriteResponse(HttpListenerContext context, string s)
        {
            WriteResponse(context.Response, s);
        }

        /// <summary>
        /// Sends text back to the web-client as text.
        /// </summary>
        /// <param name="response"></param>
        /// <param name="s"></param>
        public void WriteResponse(HttpListenerResponse response, string s)
        {
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(s);
            response.ContentLength64 = buffer.Length;
            System.IO.Stream output = response.OutputStream;
            output.Write(buffer, 0, buffer.Length);
            output.Close();
        }

        /// <summary>
        /// Sets up the web-server and
        /// Called when the user trigger the 3ds Max user action. (e.g. menu selected, hotkey pressed, etc.)
        /// </summary>
        /// <param name="param"></param>
        public override void Execute(object param)
        {
            // I hate to compile this binary, so let it configurable.
            if (param != null && param.ToString() != "") {
                port = param.ToString();
            }

            try
            {
                if (listener != null)
                    return;

                dispatcher = Dispatcher.CurrentDispatcher;
                global = Autodesk.Max.GlobalInterface.Instance;
                listener = new HttpListener();

                if (!HttpListener.IsSupported)
                {
                    WriteLine("HTTP Listener is not supported on this platform.");
                    return;
                }

                // Receieve requests on port 8080. (e.g. http://localhost:8080/)
                // http://www.itgo.me/a/4185947601524058541/httplistener-access-denied
                // https://stackoverflow.com/questions/4019466/httplistener-access-denied
                // listener.Prefixes.Add("http://*:"+ port + "/");
                // listener.Prefixes.Add("http://127.0.0.1:"+ port + "/");
                listener.Prefixes.Add("http://localhost:"+ port + "/");
                try
                {
                    WriteLine("Starting HTTP listener");
                    listener.Start();
                    WriteLine("HTTP listener started");
                }
                catch (HttpListenerException he)
                {
                    WriteLine("Unable to start the HTTP listener service. Try running 3ds Max in administrator mode. " + he.Message);
                }

                WriteLine("Launching new thread");
                var t = new Thread(() => ListenLoop());
                t.Start();
            }
            catch (Exception e)
            {
                WriteLine(e.Message);
            }
        }

        /// <summary>
        /// The string associated with the user action
        /// </summary>
        public override string ActionText
        {
            get { return "MAXScript web server";  }
        }

        /// <summary>
        /// The category associated with the user action. This is shown when customizing the user interface.
        /// </summary>
        public override string Category
        {
            get { return ".NET Samples"; }
        }

        /// <summary>
        /// The string associated with the user action, non-localized.
        /// </summary>
        public override string InternalActionText
        {
            get { return ActionText;  }
        }

        /// <summary>
        /// The category name, non-localized.
        /// </summary>
        public override string InternalCategory
        {
            get { return Category;  }
        }

        /// <summary>
        /// Print text to the MAXScript listener window.
        /// </summary>
        /// <param name="s"></param>
        public void Write(string s)
        {
            global.TheListener.EditStream.Wputs(s);
            global.TheListener.EditStream.Flush();
        }

        /// <summary>
        /// Print text to the MAXScript listerner window.
        /// </summary>
        /// <param name="s"></param>
        public void WriteLine(string s)
        {
            Write(s + "\n");
        }
    }
}

