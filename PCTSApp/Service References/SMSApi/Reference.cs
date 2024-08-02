﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace PCTSApp.SMSApi {
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ServiceModel.ServiceContractAttribute(Namespace="https://pctsrajmedical.raj.nic.in/", ConfigurationName="SMSApi.SendSMSSoap")]
    public interface SendSMSSoap {
        
        [System.ServiceModel.OperationContractAttribute(Action="https://pctsrajmedical.raj.nic.in/SendSMS", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        string SendSMS(string AppID, string UserID, string Password, string Contenttype, string SenderID, string MobileNo, string Message, string InnerPassword, string Salt);
        
        [System.ServiceModel.OperationContractAttribute(Action="https://pctsrajmedical.raj.nic.in/SendSMS", ReplyAction="*")]
        System.Threading.Tasks.Task<string> SendSMSAsync(string AppID, string UserID, string Password, string Contenttype, string SenderID, string MobileNo, string Message, string InnerPassword, string Salt);
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public interface SendSMSSoapChannel : PCTSApp.SMSApi.SendSMSSoap, System.ServiceModel.IClientChannel {
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public partial class SendSMSSoapClient : System.ServiceModel.ClientBase<PCTSApp.SMSApi.SendSMSSoap>, PCTSApp.SMSApi.SendSMSSoap {
        
        public SendSMSSoapClient() {
        }
        
        public SendSMSSoapClient(string endpointConfigurationName) : 
                base(endpointConfigurationName) {
        }
        
        public SendSMSSoapClient(string endpointConfigurationName, string remoteAddress) : 
                base(endpointConfigurationName, remoteAddress) {
        }
        
        public SendSMSSoapClient(string endpointConfigurationName, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(endpointConfigurationName, remoteAddress) {
        }
        
        public SendSMSSoapClient(System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(binding, remoteAddress) {
        }
        
        public string SendSMS(string AppID, string UserID, string Password, string Contenttype, string SenderID, string MobileNo, string Message, string InnerPassword, string Salt) {
            return base.Channel.SendSMS(AppID, UserID, Password, Contenttype, SenderID, MobileNo, Message, InnerPassword, Salt);
        }
        
        public System.Threading.Tasks.Task<string> SendSMSAsync(string AppID, string UserID, string Password, string Contenttype, string SenderID, string MobileNo, string Message, string InnerPassword, string Salt) {
            return base.Channel.SendSMSAsync(AppID, UserID, Password, Contenttype, SenderID, MobileNo, Message, InnerPassword, Salt);
        }
    }
}