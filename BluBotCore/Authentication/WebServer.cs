using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;


namespace BluBotCore.Authentication
{
    public class WebServer
    {
        private readonly HttpListener listener;

        public WebServer(string uri)
        {
            listener = new HttpListener();
            listener.Prefixes.Add(uri);
        }

        public async Task<Models.Authorization> Listen()
        {
            listener.Start();
            return await OnRequest();
        }

        private async Task<Models.Authorization> OnRequest()
        {
            while (listener.IsListening)
            {
                var ctx = await listener.GetContextAsync();
                var req = ctx.Request;
                var resp = ctx.Response;

                using (var writer = new StreamWriter(resp.OutputStream))
                {
                    if (req.QueryString.AllKeys.Any("code".Contains))
                    {
                        writer.WriteLine("Authorization started! Check your application!");
                        writer.Flush();
                        return new Models.Authorization(req.QueryString["code"]);
                    }
                    else
                    {
                        writer.WriteLine("No code found in query string!");
                        writer.Flush();
                    }
                }
            }
            return null;
        }
    }
}
