using KANO.Core.Lib.Extension;
using KANO.Core.Model;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KANO.Core.Service
{
    public static class DataRestriction
    {
        public static List<string> GetBuildingCodes(IMongoDatabase DB, string username)
        {
            var get = DB.GetCollection<User>().Find(x => x.Username == username).FirstOrDefault();
            List<string> buildngCode = new List<string>();
            //if (get != null && get.Building != null && get.Building.Count() > 0)
            //{
            //    buildngCode.AddRange(get.Building.Select(x => x.Id));
            //    return buildngCode;
            //}
            return new List<string>();
        }

        //public static string GetBuildingID(IMongoDatabase DB, string unitId)
        //{
        //    var getBuilding = DB.GetCollection<Unit>().Find(x => x.Id == unitId).FirstOrDefault();
        //    if (getBuilding != null)
        //        return getBuilding.Building.Id;
        //    else
        //        return "";
        //}

        //public static List<LOO> RestricLOOdatabyLooID(IMongoDatabase DB, List<string> LOOs, List<string> Buildings )
        //{
        //    var getallLOO = DB.GetCollection<LOO>().Find(x => LOOs.Contains(x.Id)  ).ToList();
        //    return getallLOO.Where(x => Buildings.Contains(x.BuildingId)).ToList();
        //}

        //public static List<ProformaInvoice> RestricLOOdatabyLooIDProforma(IMongoDatabase DB, List<string> LOOs, List<string> Buildings)
        //{
        //    var getallLOO = DB.GetCollection<LOO>().Find(x => LOOs.Contains(x.Id)).ToList();
        //    if (getallLOO != null && getallLOO.Count() > 0)
        //    {
        //        var loodata = getallLOO.Where(x => Buildings.Contains(x.BuildingId)).ToList();
        //        if(loodata != null && loodata.Count() > 0)
        //        {
        //            var strdata = loodata.Select(y => y.Id).ToList();
        //            var data =  DB.GetCollection<ProformaInvoice>().Find(x => strdata.Contains(x.Reference)).ToList();
        //            return data;
        //        }
        //    }
        //    return new List<ProformaInvoice>();
        //}
    }
}
