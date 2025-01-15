//SVCUTIL.EXE 가 자동으로 만든 프로그램
namespace WCF_LBS.Network
{
    using System.Runtime.Serialization;
    
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Runtime.Serialization", "4.0.0.0")]
    [System.Runtime.Serialization.DataContractAttribute(Name="ClientInfo", Namespace="http://schemas.datacontract.org/2004/07/CIM.Network.WCF")]
    public partial class ClientInfo : object, System.Runtime.Serialization.IExtensibleDataObject
    {
        
        private System.Runtime.Serialization.ExtensionDataObject extensionDataField;
        
        private string DescriptionField;
        
        private string IPAddressField;
        
        private string UserIDField;
        
        public System.Runtime.Serialization.ExtensionDataObject ExtensionData
        {
            get
            {
                return this.extensionDataField;
            }
            set
            {
                this.extensionDataField = value;
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public string Description
        {
            get
            {
                return this.DescriptionField;
            }
            set
            {
                this.DescriptionField = value;
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public string IPAddress
        {
            get
            {
                return this.IPAddressField;
            }
            set
            {
                this.IPAddressField = value;
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public string UserID
        {
            get
            {
                return this.UserIDField;
            }
            set
            {
                this.UserIDField = value;
            }
        }
    }
}


[System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
[System.ServiceModel.ServiceContractAttribute(ConfigurationName="IStkControlService", CallbackContract=typeof(IStkControlServiceCallback))]
public interface IStkControlService
{
    
    [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IStkControlService/Connect", ReplyAction="http://tempuri.org/IStkControlService/ConnectResponse")]
    int Connect(WCF_LBS.Network.ClientInfo clientInfo);
    
    [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IStkControlService/Connect", ReplyAction="http://tempuri.org/IStkControlService/ConnectResponse")]
    System.Threading.Tasks.Task<int> ConnectAsync(WCF_LBS.Network.ClientInfo clientInfo);
    
    [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IStkControlService/Disconnect", ReplyAction="http://tempuri.org/IStkControlService/DisconnectResponse")]
    int Disconnect(WCF_LBS.Network.ClientInfo clientInfo);
    
    [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IStkControlService/Disconnect", ReplyAction="http://tempuri.org/IStkControlService/DisconnectResponse")]
    System.Threading.Tasks.Task<int> DisconnectAsync(WCF_LBS.Network.ClientInfo clientInfo);
    
    [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IStkControlService/SendString", ReplyAction="http://tempuri.org/IStkControlService/SendStringResponse")]
    SendStringResponse SendString(SendStringRequest request);
    
    // CODEGEN: 작업에 여러 개의 반환 값이 있기 때문에 메시지 계약을 생성하는 중입니다.
    [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IStkControlService/SendString", ReplyAction="http://tempuri.org/IStkControlService/SendStringResponse")]
    System.Threading.Tasks.Task<SendStringResponse> SendStringAsync(SendStringRequest request);
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
public interface IStkControlServiceCallback
{
    
    [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IStkControlService/ReceiveString", ReplyAction="http://tempuri.org/IStkControlService/ReceiveStringResponse")]
    int ReceiveString(out string recvStr, string sendStr);
}

[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
[System.ServiceModel.MessageContractAttribute(WrapperName="SendString", WrapperNamespace="http://tempuri.org/", IsWrapped=true)]
public partial class SendStringRequest
{
    
    [System.ServiceModel.MessageBodyMemberAttribute(Namespace="http://tempuri.org/", Order=0)]
    public string sendStr;
    
    public SendStringRequest()
    {
    }
    
    public SendStringRequest(string sendStr)
    {
        this.sendStr = sendStr;
    }
}

[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
[System.ServiceModel.MessageContractAttribute(WrapperName="SendStringResponse", WrapperNamespace="http://tempuri.org/", IsWrapped=true)]
public partial class SendStringResponse
{
    
    [System.ServiceModel.MessageBodyMemberAttribute(Namespace="http://tempuri.org/", Order=0)]
    public int SendStringResult;
    
    [System.ServiceModel.MessageBodyMemberAttribute(Namespace="http://tempuri.org/", Order=1)]
    public string recvStr;
    
    public SendStringResponse()
    {
    }
    
    public SendStringResponse(int SendStringResult, string recvStr)
    {
        this.SendStringResult = SendStringResult;
        this.recvStr = recvStr;
    }
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
public interface IStkControlServiceChannel : IStkControlService, System.ServiceModel.IClientChannel
{
}

[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
public partial class StkControlServiceClient : System.ServiceModel.DuplexClientBase<IStkControlService>, IStkControlService
{
    
    public StkControlServiceClient(System.ServiceModel.InstanceContext callbackInstance) : 
            base(callbackInstance)
    {
    }
    
    public StkControlServiceClient(System.ServiceModel.InstanceContext callbackInstance, string endpointConfigurationName) : 
            base(callbackInstance, endpointConfigurationName)
    {
    }
    
    public StkControlServiceClient(System.ServiceModel.InstanceContext callbackInstance, string endpointConfigurationName, string remoteAddress) : 
            base(callbackInstance, endpointConfigurationName, remoteAddress)
    {
    }
    
    public StkControlServiceClient(System.ServiceModel.InstanceContext callbackInstance, string endpointConfigurationName, System.ServiceModel.EndpointAddress remoteAddress) : 
            base(callbackInstance, endpointConfigurationName, remoteAddress)
    {
    }
    
    public StkControlServiceClient(System.ServiceModel.InstanceContext callbackInstance, System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress) : 
            base(callbackInstance, binding, remoteAddress)
    {
    }
    
    public int Connect(WCF_LBS.Network.ClientInfo clientInfo)
    {
        return base.Channel.Connect(clientInfo);
    }
    
    public System.Threading.Tasks.Task<int> ConnectAsync(WCF_LBS.Network.ClientInfo clientInfo)
    {
        return base.Channel.ConnectAsync(clientInfo);
    }
    
    public int Disconnect(WCF_LBS.Network.ClientInfo clientInfo)
    {
        return base.Channel.Disconnect(clientInfo);
    }
    
    public System.Threading.Tasks.Task<int> DisconnectAsync(WCF_LBS.Network.ClientInfo clientInfo)
    {
        return base.Channel.DisconnectAsync(clientInfo);
    }
    
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    SendStringResponse IStkControlService.SendString(SendStringRequest request)
    {
        return base.Channel.SendString(request);
    }
    
    public int SendString(string sendStr, out string recvStr)
    {
        SendStringRequest inValue = new SendStringRequest();
        inValue.sendStr = sendStr;
        SendStringResponse retVal = ((IStkControlService)(this)).SendString(inValue);
        recvStr = retVal.recvStr;
        return retVal.SendStringResult;
    }
    
    public System.Threading.Tasks.Task<SendStringResponse> SendStringAsync(SendStringRequest request)
    {
        return base.Channel.SendStringAsync(request);
    }
}
