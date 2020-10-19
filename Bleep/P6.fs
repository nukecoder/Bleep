module P6
//#r "System.ServiceModel"
//#r "FSharp.Data.TypeProviders.dll"
//open Microsoft.FSharp.Data.TypeProviders
open FSharp.Data.TypeProviders
open System.Linq.Expressions
open System
open System.ServiceModel
open System.Xml
open System.ServiceModel.Channels
open System.ServiceModel.Security

type P6Wsdl = WsdlService<"http://p6ws.cpnpp.net:8206/p6ws/services/ProjectService?wsdl">
let un = "xb0g"
let pw = "Granite2019"

let randomString len =
    let chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"
    let charsLen = chars.Length
    let random = System.Random()
    let randomChars = [| for i in 0 .. len -> chars.[random.Next(charsLen)]|]
    new System.String(randomChars)

let nonceString (str : string) =
    let sha1 = System.Security.Cryptography.SHA1.Create()
    let myba = System.Text.UnicodeEncoding.UTF8.GetBytes(str + (randomString 10))
    let hash = sha1.ComputeHash myba
    (Convert.ToBase64String hash).Substring(0,22) + "=="

//#region AWSexample
let soapSig op =
    let dtn = DateTime.UtcNow
    let dt = new DateTime(dtn.Year,dtn.Month,dtn.Day,dtn.Hour,dtn.Minute,dtn.Second,1)
    let ts = XmlConvert.ToString(dt,XmlDateTimeSerializationMode.Utc)
    let str = sprintf "AmazonS3%s%s" op ts
    let sigStr = nonceString str
    sigStr,dt

let wssClient url un pw =
    let b = new CustomBinding()
    // security
    let security = TransportSecurityBindingElement.CreateUserNameOverTransportBindingElement()
    security.AllowInsecureTransport <- true
    security.IncludeTimestamp <- false
    security.DefaultAlgorithmSuite <- SecurityAlgorithmSuite.Basic256
    security.MessageSecurityVersion <- MessageSecurityVersion.WSSecurity10WSTrustFebruary2005WSSecureConversationFebruary2005WSSecurityPolicy11BasicSecurityProfile10

    // message encoding
    let encoding = new TextMessageEncodingBindingElement()
    encoding.MessageVersion = MessageVersion.Soap11

    // transport
    let transport = new HttpTransportBindingElement()
    //let transport = new BasicHttpBinding(BasicHttpSecurityMode.Transport)
    transport.MaxReceivedMessageSize <- 10000000L // 10 megs

    b.Elements.Add security
    b.Elements.Add encoding
    b.Elements.Add transport

    // endpoint
    let epr = new EndpointAddress("http://p6ws.cpnpp.net:8206/p6ws/services/ProjectService")
    let client = new P6Wsdl.ServiceTypes.ProjectPortTypeClient(b,epr)
    client.ChannelFactory.Endpoint.Behaviors.Remove(System.ServiceModel.Description.ClientCredentials())
    //client.ChannelFactory.Endpoint.Behaviors.Remove(System.ServiceModel.Description.ClientCredentials)
    client.ChannelFactory.Endpoint.Behaviors.Add(new CustomCredentials())
    client.ClientCredentials.UserName.UserName <- un
    client.ClientCredentials.UserName.Password <- pw
    client

let runOp op f =
    //let b = new BasicHttpBinding(BasicHttpSecurityMode.Transport)
    //b.MaxReceivedMessageSize <- 10000000L
    use ser = wssClient "http://p6ws.cpnpp.net:8206/p6ws/services/ProjectService" "xb0g" "Granite2019"
    try
        let readProjs = new P6Wsdl.ServiceTypes.ReadProjects()
        readProjs.Filter <- "ObjectId = 3743"
        readProjs.Field <- [| P6Wsdl.ServiceTypes.ProjectFieldType.Id; P6Wsdl.ServiceTypes.ProjectFieldType.Name; P6Wsdl.ServiceTypes.ProjectFieldType.DataDate |]
        let sign,ts = soapSig op
        let resp = f ser sign ts
        let projects = ser.ReadProjects(readProjs)
        for p in projects
            do printfn "%A" p.Name
        ser.Close()
        resp
    with ex ->
        ser.Abort()
        raise ex
//#endregion


let P6Client = P6Wsdl.GetProjectPort()
P6Client.DataContext.ClientCredentials.UserName.UserName <- "xb0g"
P6Client.DataContext.ClientCredentials.UserName.Password <- "Granite2019"



type ProjId = 
    { Id : int }

try
    let readProjs = new P6Wsdl.ServiceTypes.ReadProjects()
    readProjs.Filter <- "ObjectId = 3743"
    readProjs.Field <- [| P6Wsdl.ServiceTypes.ProjectFieldType.Id; P6Wsdl.ServiceTypes.ProjectFieldType.Name; P6Wsdl.ServiceTypes.ProjectFieldType.DataDate |]
    
    //let req = P6Wsdl.ServiceTypes.ReadProjectsRequest(readProjs)
    let res = P6Client.ReadProjects(readProjs)
    printfn "%A" res
with 
    | ex ->
        let rec inner (ex : Exception) =
            if ex.InnerException <> null then
                inner ex.InnerException
            printfn "%s" ex.Message
        inner ex
