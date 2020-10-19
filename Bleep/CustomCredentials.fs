open System.ServiceModel.Security

module CustomCredentials
open System.ServiceModel
open System.ServiceModel.Description
open System.Security.
open System.IdentityModel.Selectors

type CustomCredentials(cc) = class
    inherit ClientCredentials(cc)
    new (cc : CustomeCredentials) = base cc
    override x.CreateSecurityTokenManager() =
        new CustomSecurityTokenManager(x)
end

type CustomSecurityTokenManager(cred : CustomCredentials) = 
    inherit ClientCredentialsSecurityTokenManager(cred)
    override x.CreateSecurityTokenSerializer(version : System.IdentityModel.Selectors.SecurityTokenVersion) =
        new CustomTokenSerializer(System.ServiceModel.Security.SecurityVersion.WSSecurity11)

type CustomTokenSerializer(version : SecurityVersion)
    inherit WSSecurityTokenSerializer(version)
    override x.WriteTokenCore(writer : System.Xml.XmlWriter, token : System.IdentityModel.Tokens.SecurityToken) =
        let userToken = 
