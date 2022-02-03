using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using KANO.Core.Lib.Helper;
using KANO.Core.Model;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RestSharp;

namespace KANO.Core.Service.Odoo
{
    public static class OdooService
    {    
        public static string Authenticate(IConfiguration config){
            var sessionID = "";
            
            var url = config["Odoo:Url"];
            if (string.IsNullOrWhiteSpace(url))
                throw new Exception("Odoo:Url is not set in the configuration!");
  
            var username = config["Odoo:Username"];
            if (string.IsNullOrWhiteSpace(username))
                throw new Exception("Odoo:Username is not set in the configuration!");

            var password = config["Odoo:Password"];
            if (string.IsNullOrWhiteSpace(password))
                throw new Exception("Odoo:Password is not set in the configuration!");

            var db = config["Odoo:DB"];
            if (string.IsNullOrWhiteSpace(db))
                throw new Exception("Odoo:DB is not set in the configuration!");

            var c = new RestClient(url);
            c.AddDefaultHeader("Content-Type", "application/json");

            var param = new OdooRequest();            
            param.Params.Add("db", db);
            param.Params.Add("login", username);
            param.Params.Add("password", password);

            var r = new RestRequest("/web/session/authenticate", Method.POST);
            r.AddHeader("Accept", "application/json");
            r.Parameters.Clear();            
            r.AddParameter("application/json; charset=utf-8", JsonConvert.SerializeObject(param), ParameterType.RequestBody);

            try
            {
                var response = c.Execute(r);
                if (response.StatusCode == System.Net.HttpStatusCode.OK) {
                    var cookies = response.Cookies;
                    var cookie = cookies.Where(n => n.Name == "session_id").FirstOrDefault();
                    dynamic result = JsonConvert.DeserializeObject(response.Content);

                    if (cookie != null) {
                        sessionID = cookie.Value;
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            
            
            return sessionID;
        }

        public static OdooSurveyResponse GetSurveyResult(IConfiguration config, string sesId)
        {
            var url = config["Odoo:Url"];
            if (string.IsNullOrWhiteSpace(url))
                throw new Exception("Odoo:Url is not set in the configuration!");

            var pa = new ParamSurveyResult();
            pa.Params.fields = new paramField();
            var json = JsonConvert.SerializeObject(pa);

            var client = new RestClient(url);
            client.AddDefaultHeader("Content-Type", "application/json");

            var request = new RestRequest("/web/dataset/search_read", Method.POST);
            request.AddHeader("Accept", "application/json");
            request.AddCookie("session_id", sesId);
            request.AddParameter("application/json", json, ParameterType.RequestBody);

            IRestResponse response = client.Execute(request);
            var result = JsonConvert.DeserializeObject<OdooSurveyResponse>(response.Content);
            return result;
        }

        public static OdooSurveyResponse MGetOdooUrl(IConfiguration config, string sessID)
        {
            string url = String.Empty;
            url = config["Odoo:Url"];
            if (string.IsNullOrWhiteSpace(url))
            {
                throw new Exception("Odoo:Url is not set in the configuration!");
            }

            try
            {
                var pa = new ParamSurveyResult();
                pa.Params.fields = new paramField();
                var json = JsonConvert.SerializeObject(pa);

                var client = new RestClient(url);
                client.AddDefaultHeader("Content-Type", "application/json");

                var request = new RestRequest("/web/dataset/search_read", Method.POST);
                request.AddHeader("Accept", "application/json");
                request.AddCookie("session_id", sessID);
                request.AddParameter("application/json", json, ParameterType.RequestBody);

                IRestResponse response = client.Execute(request);
                var result = JsonConvert.DeserializeObject<OdooSurveyResponse>(response.Content);
                return result;
            }
            catch (Exception e)
            {
                throw new Exception("Odoo:Url is not set in the configuration!");
            }

        }

        public static OdooSurveyResponse MGetSurveyResult(IConfiguration config, string sesId)
        {
            var url = config["Odoo:Url"];
            if (string.IsNullOrWhiteSpace(url))
                throw new Exception("Odoo:Url is not set in the configuration!");

            var pa = new ParamSurveyResult();
            pa.Params.fields = new paramField();
            var json = JsonConvert.SerializeObject(pa);

            var client = new RestClient(url);
            client.AddDefaultHeader("Content-Type", "application/json");

            var request = new RestRequest("/web/dataset/search_read", Method.POST);
            request.AddHeader("Accept", "application/json");
            request.AddCookie("session_id", sesId);
            request.AddParameter("application/json", json, ParameterType.RequestBody);

            IRestResponse response = client.Execute(request);
            var result = JsonConvert.DeserializeObject<OdooSurveyResponse>(response.Content);
            return result;
        }

    }

    public class OdooRequest{
        [JsonProperty(PropertyName = "jsonrpc")]
        public string JsonRPC {set; get;} = "2.0";
        
        [JsonProperty(PropertyName = "params")]
        public Dictionary<string, object> Params {set; get;} = new Dictionary<string, object>();
    }


    public class OdooSurveyResponse
    {
        [JsonProperty(PropertyName = "jsonrpc")]
        public string JsonRPC { get; set; }

        [JsonProperty(PropertyName = "id")]
        public Nullable<int> Id { get; set; }

        [JsonProperty(PropertyName = "result")]
        public OdooSurveyResult Result { get; set; }

        [JsonProperty(PropertyName = "error")]
        public OdooError Error { get; set; }
    }

    public class OdooSurveyResult
    {
        [JsonProperty(PropertyName = "length")]
        public int Length { get; set; }

        [JsonProperty(PropertyName = "records")]
        public List<OdooSurveyRecord> Records { get; set; }
    }

    public class OdooSurveyResponseData
    {
        [JsonProperty(PropertyName = "jsonrpc")]
        public string JsonRPC { get; set; }

        [JsonProperty(PropertyName = "id")]
        public Nullable<int> Id { get; set; }

        [JsonProperty(PropertyName = "result")]
        public OdooSurveyResultData Result { get; set; }

        [JsonProperty(PropertyName = "error")]
        public OdooError Error { get; set; }
    }

    public class OdooSurveyResultData
    {
        [JsonProperty(PropertyName = "length")]
        public int Length { get; set; }

        [JsonProperty(PropertyName = "records")]
        public List<OdooSurvey> Records { get; set; }
    }

    public class OdooSurveyResponseAnswer
    {
        [JsonProperty(PropertyName = "jsonrpc")]
        public string JsonRPC { get; set; }

        [JsonProperty(PropertyName = "id")]
        public Nullable<int> Id { get; set; }

        [JsonProperty(PropertyName = "result")]
        public OdooSurveyResultAnswer Result { get; set; }

        [JsonProperty(PropertyName = "error")]
        public OdooError Error { get; set; }
    }

    public class OdooSurveyResultAnswer
    {
        [JsonProperty(PropertyName = "length")]
        public int Length { get; set; }

        [JsonProperty(PropertyName = "records")]
        public List<OdooSurveyAnswer> Records { get; set; }
    }

    public class OdooError
    {
        public string code { get; set; }
        public string message { get; set; }
        //public Dictionary<string, Object> data { get; set; }
    }

    public class OdooSurveyRecord
    {
        [JsonProperty(PropertyName = "id")]
        public int Id { get; set; }

        [JsonProperty(PropertyName = "survey_id")]
        public ArrayList SurveyID { get; set; }

        [JsonProperty(PropertyName = "create_date")]
        public DateTime CreateDate { get; set; }

        [JsonProperty(PropertyName = "user_id")]
        public string UserID { get; set; }

        [JsonProperty(PropertyName = "user_name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "department")]
        public string Department { get; set; }

        [JsonProperty(PropertyName = "mail_id")]
        public string Email { get; set; }

        [JsonProperty(PropertyName = "state")]
        public string Status { get; set; }

        [JsonProperty(PropertyName = "scoring_type")]
        public string ScoringType { get; set; }

        [JsonProperty(PropertyName = "quizz_score")]
        public double Score { get; set; }

        [JsonProperty(PropertyName = "passed_category")]
        public string PassedCategory { get; set; }

        [JsonProperty(PropertyName = "passing_grade_category")]
        public string PassingGradeCategory { get; set; }
    }

    public class ParamSurvey
    {
        [JsonProperty(PropertyName = "jsonrpc")]
        public string JsonRPC { set; get; } = "2.0";

        [JsonProperty(PropertyName = "method")]
        public string Method = "call";

        [JsonProperty(PropertyName = "params")]
        public ParamModel Params = new ParamModel();

    }

    public class ParamModel
    {
        public string model { get; set; } = "survey.survey";
    }

    public class ParamSurveyResult
    {
        [JsonProperty(PropertyName = "jsonrpc")]
        public string JsonRPC { set; get; } = "2.0";

        [JsonProperty(PropertyName = "method")]
        public string Method = "call";

        [JsonProperty(PropertyName = "params")]
        public paramParams Params = new paramParams();
    }

    public class paramParams
    {
        public string login { get; set; }
        public string password { get; set; }
        public string db { get; set; }
        public string model { get; set; } = "survey.user_input";
        public List<dynamic> domain { get; set; } = new List<dynamic>();
        public paramField fields = new paramField();
    }

    public class paramField
    {
        [JsonProperty(PropertyName = "survey_id")]
        public string SurveyId = "survey_id";

        [JsonProperty(PropertyName = "create_date")]
        public string CreateDate = "create_date";

        public string deadline = "deadline";
        public string user_id = "user_id";
        public string mail_id = "umail";
        public string state = "state";
        public string scoring_type = "scoring_type";
        public string quizz_score = "quizz_score";
        public string passed_category = "passed_category";
        public string passing_grade_category = "passing_grade_category";        
    }
}