using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

/// <summary>
/// Summary description for header
/// </summary>
public class header : IHttpModule
{
    public void Init(HttpApplication context)
    {
        context.PreSendRequestHeaders += new EventHandler(this.PreSendRequestHeaders);
    }

    public void Dispose() { }


    public void PreSendRequestHeaders(object sender, EventArgs e)
    {
        HttpApplication httpApplication = (HttpApplication)sender;
        HttpApplication application = (HttpApplication)sender;
        HttpContext context = application.Context;
        context.Response.Headers.Remove("Server");
        context.Response.Headers.Remove("X-AspNet-Version");
    }
}