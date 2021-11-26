using KANO.Core.Model;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KANO.Core.Service
{
    public class SalesForceService
    {
        public Uri TokenUri { get; }
        public Uri APIUri { get; }

        public SalesForceOAuth2TokenResponse Token { get; private set; }

        public SalesForceService(Uri tokenUri, Uri apiUri)
        {
            TokenUri = tokenUri;
            APIUri = apiUri;
        }

        public SalesForceService(string tokenUri, string apiUri)
        {
            TokenUri = new Uri(tokenUri);
            APIUri = new Uri(apiUri);
        }

        public SalesForceOAuth2TokenResponse GetNewToken(SalesForceOAuth2Credential credential)
        {
            Token = null;
            var client = new RestClient(TokenUri);
            var request = new RestRequest(Method.POST);
            request.AddHeader("cache-control", "no-cache");
            request.AddHeader("content-type", "application/x-www-form-urlencoded");

            request.AddQueryParameter("grant_type", credential.GrantType);
            request.AddQueryParameter("client_id", credential.ClientID);
            request.AddQueryParameter("client_secret", credential.ClientSecret);
            request.AddQueryParameter("username", credential.Username);
            request.AddQueryParameter("password", credential.Password);

            IRestResponse response = client.Execute(request);

            if (response.IsSuccessful)
            {
                Token = Newtonsoft.Json.JsonConvert.DeserializeObject<SalesForceOAuth2TokenResponse>(response.Content);
                return Token;
            }
            throw new Exception("Token Request failure with response: (HTTP " + (int)response.StatusCode + " - " + response.StatusCode.ToString() + ") " + response.Content);
        }

        public string GetOpportunity(string token, string opportunityID)
        {
            var uri = APIUri.AbsoluteUri + "opportunity/" + (opportunityID ?? "");
            var client = new RestClient(uri);
            var request = new RestRequest(Method.GET);
            request.AddHeader("Authorization", "Bearer " + token);

            IRestResponse response = client.Execute(request);
            if (response.IsSuccessful)
            {
                return response.Content;
            }
            throw new Exception("Opportunity Request failure with response: (HTTP " + (int)response.StatusCode + " - " + response.StatusCode.ToString() + ") " + response.Content);
        }

        //public SalesForceOpportunityPatchLog PatchOpportunity<T>(string token, string LOOCode, string opportunityID, T patchData) where T : SalesForceOpportunityPatch
        //{
        //    if (patchData == null) throw new ArgumentNullException("patchData");
        //    var uri = APIUri.AbsoluteUri + "opportunity/" + opportunityID;
        //    var client = new RestClient(uri);
        //    var request = new RestRequest(Method.PATCH);
        //    request.AddHeader("Authorization", "Bearer " + token);
        //    request.AddHeader("content-type", "application/json");

        //    SalesForceOpportunityPatchLog sfo = SalesForceOpportunityPatchLog.Make(typeof(T).Name, opportunityID);
        //    sfo.Content = patchData;
        //    sfo.Created = DateTime.Now;
        //    sfo.LOOCode = LOOCode;
        //    sfo.LastStatus = "Success";
        //    sfo.Uri = uri;

        //    var json = Newtonsoft.Json.JsonConvert.SerializeObject(patchData);
        //    request.AddParameter("application/json", json, ParameterType.RequestBody);

        //    try
        //    {
        //        IRestResponse response = client.Execute(request);
        //        if (response.StatusCode == (System.Net.HttpStatusCode)204)
        //        {
        //            return sfo;
        //        }
        //        sfo.LastStatus = "Error";
        //        sfo.LastError = "Patch request failed with error: (HTTP " + (int)response.StatusCode + " - " + response.StatusCode.ToString() + ") " + response.Content;
        //    }
        //    catch (Exception ex)
        //    {
        //        sfo.LastStatus = "Error";
        //        sfo.LastError = "Patch operation failed with error: " + ex.Message;
        //    }
        //    return sfo;
        //}
    }

    public abstract class SalesForceOpportunityPatch
    {

    }

    public class SalesForceOpportunityPatchPaid : SalesForceOpportunityPatch
    {
        public string Paid__c { get; set; }
        public DateTime? Payment_Date__c { get; set; }
    }

    public class SalesForceOpportunityPatchSign : SalesForceOpportunityPatch
    {
        public bool Agreement_Signed_by_CoHive__c { get; set; }
        public bool Agreement_Signed_by_Tenant__c { get; set; }
        public DateTime? Agreement_Signed_Date_Cohive__c { get; set; }
        public DateTime? Agreement_Signed_Date_Tenant__c { get; set; }
        public string Agreement_Link__c { get; set; }
    }
    public class SalesForceOpportunityPatchDocumentLink : SalesForceOpportunityPatch
    {
        public string Agreement_Link__c { get; set; }
    }
    public class SalesForceOpportunityPatchAccept : SalesForceOpportunityPatch
    {
        public bool LOO_Accepted__c { get; set; }
    }
    public class SalesForceOpportunityPatchAgreement : SalesForceOpportunityPatch
    {
        public string Agreement_Number__c { get; set; } 
    }
    public class SalesForceOAuth2TokenResponse
    {
        public string access_token { get; set; }
        public string instance_url { get; set; }
        public string id { get; set; }
        public string token_type { get; set; }
        public string issued_at { get; set; }
        public string signature { get; set; }
    }

    public class SalesForceOAuth2Credential
    {
        public string ClientID { get; set; }
        public string ClientSecret { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string GrantType { get; set; }

        public SalesForceOAuth2Credential(string id = "", string secret = "")
        {
            GrantType = OAuth2GrantType.ClientCredential;
            ClientID = id;
            ClientSecret = secret;
        }
        public SalesForceOAuth2Credential(string username, string password, string id)
        {
            GrantType = OAuth2GrantType.Password;
            ClientID = id;
            Username = username;
            Password = password;
        }
    }

    public static class OAuth2GrantType
    {
        public const string Password = "password";
        public const string ClientCredential = "client_credentials";
    }
}
