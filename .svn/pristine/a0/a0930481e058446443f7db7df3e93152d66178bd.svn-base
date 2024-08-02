using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using System.Globalization;

namespace PCTSApp.Models
{
    public class ErrorHandler
    {

        public static void WriteError(string errorMessage)
        {
            try
            {
                string message = string.Format("Time: {0}", DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt"));
                message += Environment.NewLine;
                message += "-----------------------------------------------------------";
                message += Environment.NewLine;
                message += string.Format("Message: {0}", errorMessage);
                message += Environment.NewLine;
                message += "-----------------------------------------------------------";
                message += Environment.NewLine;
                string path = "~/Error/" + DateTime.Today.ToString("dd-MM-yyyy") + ".txt";
                if (!File.Exists(System.Web.HttpContext.Current.Server.MapPath(path)))
                {
                    File.Create(System.Web.HttpContext.Current.Server.MapPath(path));
                }
                using (StreamWriter writer = new StreamWriter(System.Web.HttpContext.Current.Server.MapPath(path), true))
                {
                    writer.WriteLine(message);
                    writer.Close();
                }
            }
            catch 
            {
                
            }
        }
    }

}