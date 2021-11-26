using KANO.Core.Lib.Extension;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;

namespace KANO.Core.Model
{
    [Collection("TicketCategory")]
    [BsonIgnoreExtraElements]
    public class TicketCategory : BaseDocumentVerification, IMongoPreSave<TicketCategory>
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public List<TicketCategoryContact> Contacts { get; set; } = new List<TicketCategoryContact>();
        
        public TicketCategory() : base() { }
        public TicketCategory(IMongoDatabase mongoDB, IConfiguration configuration) : base(mongoDB, configuration) { }

        public new void PreSave(IMongoDatabase db)
        {
            base.PreSave(db);            
        }
    }

    public class TicketCategoryContact 
    {
        public string EmployeeID {set; get;}
        public string Name {set; get;}
        public string Description {set; get;}
        public string Email{set; get;}
    }    
}
