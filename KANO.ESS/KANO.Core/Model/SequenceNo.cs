using KANO.Core.Lib.Extension;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Newtonsoft.Json.Bson;
using System;


namespace KANO.Core.Model
{
    [Collection("SequenceNo")]
    public  class SequenceNo 
    {
        [BsonId]
        public string Id { get; set; }
        public string Title { get; set; }
        public int NextNo { get; set; }
        public string Format { get; set; }


        public  string ClaimAsString(IMongoDatabase db, string format = "", bool commit = true)
        {
            string ret = "";
            SequenceNo sn = db.GetCollection<SequenceNo>().Find(x => x.Id == Id).FirstOrDefault();  // this. DataHe.Populate<SequenceNo>("SequenceNos", Query.EQ("_id", _id)).FirstOrDefault();
            if (sn != null)
            {
                NextNo = sn.NextNo;
            }
            if (String.IsNullOrEmpty(format)) format = Format;
            ret = (format.Equals("")) ? NextNo.ToString() :
                String.Format(format, NextNo);
            if (commit)
            {
                this.NextNo++;
                db.Save<SequenceNo>(this);
                //dataHelper.Save("SequenceNos", new BsonDocument[] { this.ToBsonDocument() });
            }
            return ret;
        }

        public int ClaimAsInt(IMongoDatabase db, bool commit = true)
        {
            int ret = 0;
            //SequenceNo sn = dataHelper.Populate<SequenceNo>("SequenceNos", Query.EQ("_id", _id)).FirstOrDefault();
            SequenceNo sn = db.GetCollection<SequenceNo>().Find(x => x.Id == Id).FirstOrDefault();  // this. DataHe.Populate<SequenceNo>("SequenceNos", Query.EQ("_id", _id)).FirstOrDefault();

            if (sn != null)
            {
                NextNo = sn.NextNo;
            }
            ret = NextNo;
            if (commit)
            {
                this.NextNo++;
                db.Save<SequenceNo>(this);
                //dataHelper.Save("SequenceNos", new BsonDocument[] { this.ToBsonDocument() });
            }
            return ret;
        }

        public static SequenceNo Get(IMongoDatabase db, string id)
        {
            SequenceNo ret = db.GetCollection<SequenceNo>().Find(x => x.Id == id).FirstOrDefault();  // this. DataHe.Populate<SequenceNo>("SequenceNos", Query.EQ("_id", _id)).FirstOrDefault();

           // SequenceNo ret = dataHelper.Populate<SequenceNo>("SequenceNos", Query.EQ("_id", id)).FirstOrDefault();
            if (ret == null)
            {
                ret = new SequenceNo
                {
                    Id = id,
                    Title = id,
                    NextNo = 1,
                    Format = ""
                };
            }
            return ret;
        }

        public static int ClaimInteger(IMongoDatabase db, string id)
        {
            var seq = Get(db, id);
            var no = seq.NextNo;

            seq.NextNo++;
            db.Save(seq);
            return no;
        }
    }
}
