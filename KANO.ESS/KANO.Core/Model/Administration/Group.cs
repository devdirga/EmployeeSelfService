using KANO.Core.Lib.Extension;
using KANO.Core.Service;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KANO.Core.Model
{
    [Collection("Groups")]
    public class Group : BaseT, IMongoPreSave<Group>
    {
        [BsonId]
        public string Id { get; set; }
        public string Name { get; set; }
        public bool Enable { get; set; }
        public List<int> Types { get; set; }
        public List<AccessGrant> Grant { get; set; } = new List<AccessGrant>();
        public string CreateBy { get; set; }
        public DateTime CreateDate { get; set; }
        public void PreSave(IMongoDatabase db)
        {
            this.LastUpdate = DateTime.Now;
            if (string.IsNullOrEmpty(Id))
            {
                Id = Guid.NewGuid().ToString("N");
                CreateDate = DateTime.Now;
            }
        }

        public static bool IsNameAlreadyUsed(IMongoDatabase db, Group grp)
        {
            return db.GetCollection<Group>().Find(x => x.Name == grp.Name && x.Id != grp.Id).FirstOrDefault() != null;
        }

        public static void Init(IMongoDatabase db, List<Page> pages)
        {
            var administrator = "Administrator";
            var groupCount = db.GetCollection<Group>().CountDocuments(x => x.Id == administrator);
            if (groupCount > 0) return;

            var group = new Group
            {
                Id = administrator,
                CreateBy = "system",
                CreateDate = DateTime.Now,
                Enable = true,
                LastUpdate = DateTime.Now,
                Name = administrator,
                Grant = new List<AccessGrant>(),
                Types = new List<int>(),
            };

            foreach (var page in pages)
            {
                group.Grant.Add(new AccessGrant
                {
                    Actions=ActionGrant.All,
                    PageID=page.Id,
                    PageCode=page.PageCode,
                    PageTitle=page.Title,
                });
            }

            foreach (int type in Enum.GetValues(typeof(SpecialGroupType))) {
                group.Types.Add(type);
            }

            db.Save(group);
        }
    }

    public class AccessGrant
    {
        public string PageID { get; set; }
        public string PageCode { get; set; }
        public string PageTitle { get; set; }
        [BsonIgnore]
        public string Url { get; set; }
        public List<string> SpecialActions { get; set; } = new List<string>();

        [BsonIgnore]
        public bool CanCreate { get => (Actions & ActionGrant.Create) > 0; set => Actions = Actions | ActionGrant.Create; }
        [BsonIgnore]
        public bool CanRead { get => (Actions & ActionGrant.Read) > 0; set => Actions = Actions | ActionGrant.Read; }
        [BsonIgnore]
        public bool CanUpdate { get => (Actions & ActionGrant.Update) > 0; set => Actions = Actions | ActionGrant.Update; }
        [BsonIgnore]
        public bool CanDelete { get => (Actions & ActionGrant.Delete) > 0; set => Actions = Actions | ActionGrant.Delete; }
        [BsonIgnore]
        public bool CanUpload { get => (Actions & ActionGrant.Upload) > 0; set => Actions = Actions | ActionGrant.Upload; }
        [BsonIgnore]
        public bool CanDownload { get => (Actions & ActionGrant.Download) > 0; set => Actions = Actions | ActionGrant.Download; }
        public ActionGrant Actions { get; set; }

        public void SetActions(ActionGrant grant) { Actions = grant; }
    }

    public class AccessGrantTreeNode : AccessGrant
    {
        public List<AccessGrantTreeNode> Children { get; set; } = new List<AccessGrantTreeNode>();

        public void GenerateChildren(IEnumerable<Page> pages, Group group)
        {
            var sub = pages.Where(p => p.ParentId == PageID).ToList();
            foreach (var s in sub)
            {
                var node = new AccessGrantTreeNode()
                {
                    Actions = group.Grant.Where(g => g.PageID == s.Id).FirstOrDefault()?.Actions ?? 0,
                    PageCode = s.PageCode,
                    PageID = s.Id,
                    PageTitle = s.Title
                };
                node.GenerateChildren(pages, group);
                Children.Add(node);
            }
        }

        public static List<AccessGrantTreeNode> GenerateFullGrants(IMongoDatabase DB, Group group)
        {
            var pages = DB.GetCollection<Page>().Find(p => p.Enabled).ToList();
            var res = new List<AccessGrantTreeNode>();

            var tld = pages.Where(p => string.IsNullOrEmpty(p.ParentId)).ToList();
            foreach (var t in tld)
            {
                var node = new AccessGrantTreeNode()
                {
                    Actions = group.Grant.Where(g => g.PageID == t.Id).FirstOrDefault()?.Actions ?? 0,
                    PageCode = t.PageCode,
                    PageID = t.Id,
                    PageTitle = t.Title
                };
                node.GenerateChildren(pages, group);
                res.Add(node);
            }

            return res;
        }
    }

    public enum ActionGrant : int
    {
        None = 0,
        Create = 1,
        Read = 2,
        Update = 4,
        Delete = 8,
        Upload = 16,
        Download = 32,

        All = 255
    }

    public enum SpecialGroupType { 
        HumanResourceAdministrator = 10,
        Manager = 20,
        DepartementHead = 30,
        Director = 40,
    }
}
