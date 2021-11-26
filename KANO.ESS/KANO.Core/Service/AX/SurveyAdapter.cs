using KANO.Core.Lib.Helper;
using KANO.Core.Model;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KANO.Core.Service.AX
{
    public class SurveyAdapter  
    {
        private IConfiguration Configuration;
        private Credential credential;

        public SurveyAdapter(IConfiguration config)
        {
            Configuration = config;
        }

        public List<Survey> GetS(DateTime start, DateTime finish)
        {
            var surveys = new List<Survey>();            
            try
            {                
               // TODO : Implement ODOO get available survey API                      
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
               // If needed
            }

            return surveys;
        }

        public SurveySummary GetSummary(string surveyID)
        {
            var summary = new SurveySummary();            
            try
            {                
               // TODO : Implement ODOO get available survey API                      
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
               // If needed
            }

            return summary;
        }        
    }
}
