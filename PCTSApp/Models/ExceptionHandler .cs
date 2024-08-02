using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net;
using System.Net.Http;
using System.Web.Http.Filters;

namespace PCTSApp.Models
{
    public class ExceptionHandler : ExceptionFilterAttribute 
    {
        public override void OnException(HttpActionExecutedContext context)
        {
            ResponseModel _objResponseModel = new ResponseModel();
            _objResponseModel.Status = false;
            _objResponseModel.Message = "";
            //if (context.Exception is NotImplementedException)
            //{
            //    _objResponseModel.Message = "HttpStatusCode.NotImplemented";
            //}
            //else if (context.Exception is NullReferenceException)
            //{
            //    context.Response = new HttpResponseMessage(HttpStatusCode.NoContent);
            //}
            context.Response = context.Request.CreateResponse(HttpStatusCode.OK, _objResponseModel);
        
        }
    }
}