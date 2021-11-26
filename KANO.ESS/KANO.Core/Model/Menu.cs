using KANO.Core.Lib.Extension;
using KANO.Core.Service;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System;
namespace KANO.Core.Model
{
    [Collection("Menu")]
    public class Menu : BaseT,IMongoPreSave<Menu>,  IMongoExtendedPostSave<Menu>, IMongoExtendedPostDelete<Menu>
    {
        [BsonId]
        public string Id { get; set; }
        public string MenuCode { get; set; }
        public string Category { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }
        public string Icon { get; set; }
        public string ParentId { get; set; }
        public int Index { get; set; }
        public bool CreateAcces { get; set; }
        public bool ReadAcces { get; set; }
        public bool UpdateAcces { get; set; }
        public bool DeleteAcces { get; set; }
        public bool UploadAcces { get; set; }
        public string CreateBy { get; set; }
        public DateTime CreateDate { get; set; }
        public Menu Submenu { get; set; }

        public void PreSave(IMongoDatabase db)
        {
            this.LastUpdate = Tools.ToUTC(DateTime.Now);
            if (string.IsNullOrEmpty(Id))
            {
                Id = MenuCode; // ObjectId.GenerateNewId().ToString();
                CreateDate = DateTime.Now; 
            }
        }

        public static bool isCodeAlreadyUsed(IMongoDatabase db, string MenuCode)
        {
            var ret = db.GetCollection<Menu>().Find(x => x.MenuCode == MenuCode).ToList();
            if (ret != null && ret.Count > 0)
                return false;
            else
            {
                return true;
            }
        }

        public void PostSave(IMongoDatabase db, Menu originalObject)
        {
        }

        public void PostDelete(IMongoDatabase db, Menu originalObject)
        {
        }
    }
}
