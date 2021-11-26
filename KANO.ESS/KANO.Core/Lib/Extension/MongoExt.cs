using KANO.Core.Model;
using KANO.Core;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace KANO.Core.Lib.Extension
{
    [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false)]
    public class CollectionAttribute : System.Attribute
    {
        public CollectionAttribute(string name)
        {
            Name = name;
        }

        public readonly string Name;
    }

    public static class MongoExt
    {
        public static IMongoCollection<T> GetCollection<T>(this IMongoDatabase db)
        {
            var attr = typeof(T).GetCustomAttribute<CollectionAttribute>(true);

            if (attr == null)
            {
                throw new ArgumentException(
                    $"Type {nameof(T)} does not declared as Collection");
            }

            return db.GetCollection<T>(attr.Name);
        }
        protected class KeyObject
        {
            public object Value { get; }
            public PropertyInfo Prop { get; }

            public KeyObject(object val, PropertyInfo prop)
            {
                Value = val;
                Prop = prop;
            }

            public static KeyObject CreateFrom(object obj)
            {
                object keyValue = null;
                PropertyInfo[] props = obj.GetType().GetProperties();
                foreach (PropertyInfo prop in props)
                {
                    var attr = prop.GetCustomAttribute<BsonIdAttribute>(true);
                    if (attr == null)
                    {
                        continue;
                    }

                    keyValue = prop.GetValue(obj);
                    if (keyValue == null)
                        return new KeyObject(keyValue, prop);

                    var bsonrepr = prop.GetCustomAttribute<BsonRepresentationAttribute>(true);
                    if (bsonrepr == null)
                        return new KeyObject(keyValue, prop);

                    // This should cast property into BsonRepresentation
                    // However for now, the code is very crude.
                    // TODO: FIX THIS.
                    switch (bsonrepr.Representation)
                    {
                        case BsonType.ObjectId:
                            if (prop.PropertyType == typeof(string))
                            {
                                keyValue = new ObjectId((string)keyValue);
                                break;
                            }

                            throw new Exception("Unsupported type for id");
                        default:
                            throw new Exception("Unsupported type for id");
                    }

                    return new KeyObject(keyValue, prop);
                }

                return null;
            }
        }

        static List<string> UpdateBys()
        {
            return new List<string>() { "UpdatedBy", "UpdateBy", "LastUpdateBy" };
        }

        static void WriteLog<T>(T del, IMongoDatabase db, string logType)
        {
            Type t = del.GetType();
            try
            {

                foreach (var p in UpdateBys())
                {
                    var pp = t.GetProperty(p);
                    if (pp != null)
                    {
                        var yy = pp.GetValue(del);
                        if (yy != null)
                        {
                            var xx = new Audit<T>(del, logType, yy.ToString(), db);
                            var colx = db.GetCollection<BsonDocument>(del.GetType().Name + "_Log");
                            colx.InsertOne(xx.ToBsonDocument());
                            break;
                        }
                    }
                }



            }
            catch { }
        }


        // Save with support of replacing _id
        // If key is null, then obj will be inserted into collection
        public static void Save<T>(this IMongoDatabase db, object key, T obj) where T : class
        {
            var curkey = KeyObject.CreateFrom(obj);
            var preSave = obj as IMongoPreSave<T>;
            var extpreSave = obj as IMongoExtendedPreSave<T>;
            var extpostSave = obj as IMongoExtendedPostSave<T>;

            if (curkey == null)
            {
                throw new Exception("Property ID not found");
            }

            // use normal save if the key is same
            if (key == curkey.Value)
            {
                Save(db, obj);

                if (extpostSave != null)
                {
                    extpostSave.PostSave(db, obj);
                    if (Startup.StaticConfig != null)
                    {
                        var cfg = Startup.StaticConfig["AuditLog:Activated"];
                        if (!String.IsNullOrEmpty(cfg) && (cfg).Equals("1"))
                        {
                            WriteLog<T>(obj, db, "Insert-Update");
                        }
                    }
                }

                return;
            }

            // let's do this
           
            var query = Builders<T>.Filter.Eq("_id", key);

            // store collection var
            var collection = db.GetCollection<T>();

            // call extended presave, or simple presave
            if (extpreSave != null)
            {
                // extended presave need to pull old document
                var oldobj = collection.Find(query).FirstOrDefault();
                if (oldobj == null)
                {
                    throw new Exception("Invalid old key, not found in collection");
                }

                extpreSave.PreSave(db, oldobj);
            }
            else if (preSave != null)
            {
                preSave.PreSave(db);
            }

            // remove old object
            var del = collection.FindOneAndDelete(query);
            if (del == null)
            {
                throw new Exception("Invalid old key, not found in collection");
            }

            collection.InsertOne(obj);

            // call extended postsave
            if (extpostSave != null)
            {
                extpostSave.PostSave(db, del);
                if (Startup.StaticConfig != null)
                {
                    var cfg = Startup.StaticConfig["AuditLog:Activated"];
                    if (!String.IsNullOrEmpty(cfg) && (cfg).Equals("1"))
                    {
                        WriteLog<T>(del, db, "Insert-Update");
                    }
                }

            }
        }

      

        public static void Delete<T>(this IMongoDatabase db, T obj) where T : class
        {
            var postDelete = obj as IMongoExtendedPostDelete<T>;
            var collection = db.GetCollection<T>();
            var keyVal = obj.ToBsonDocument().GetElement("_id").Value.ToString();
            if (keyVal == String.Empty)
            {
                throw new Exception("Key is Null");
            }
            else
            {

                var query = Builders<T>.Filter.Eq("_id", keyVal);
                T oldobj = collection.Find(query).FirstOrDefault();

                collection.FindOneAndDelete(query);

                if (postDelete != null)
                {
                    postDelete.PostDelete(db, oldobj);
                    if (Startup.StaticConfig != null)
                    {
                        var cfg = Startup.StaticConfig["AuditLog:Activated"];
                        if (!String.IsNullOrEmpty(cfg) && cfg.Equals("1"))
                        {
                            WriteLog<T>(oldobj, db, "Delete");
                        }
                    }
                }
            }
        }

        // Simple save
        public static void Save<T>(this IMongoDatabase db, T obj) where T : class
        {
            var preSave = obj as IMongoPreSave<T>;
            var extpreSave = obj as IMongoExtendedPreSave<T>;
            var extpostSave = obj as IMongoExtendedPostSave<T>;

            // Search ID and determine action
            var keyobj = KeyObject.CreateFrom(obj);
            var collection = db.GetCollection<T>();
            T oldobj = null;

            // error, no property id
            if (keyobj == null)
            {
                throw new Exception("Property ID not found");
            }

            // id already exists and model requesting extended presave
            if (extpreSave != null && keyobj.Value != null)
            {
                // check if document exists
                var query = Builders<T>.Filter.Eq("_id", keyobj.Value);
                oldobj = collection.Find(query).FirstOrDefault();
            }

            // call extended presave, or simple presave
            if (extpreSave != null)
            {
                extpreSave.PreSave(db, oldobj);
            }
            else if (preSave != null)
            {
                preSave.PreSave(db);
            }

            // do insert explicitely if key null, or use upsert
            var keyVal = obj.ToBsonDocument().GetElement("_id").Value.ToString();

            // do insert for ID Type ObjectID
            var objIDType = obj.ToBsonDocument().GetElement("_id").Value.GetType();
            if (objIDType.Name == "BsonObjectId")
            {
                if (ObjectId.Parse(keyVal) == ObjectId.Empty)
                {
                    collection.InsertOne(obj);
                }
                else
                {
                    var query = Builders<T>.Filter.Eq("_id", ObjectId.Parse(keyVal));
                    oldobj = collection.FindOneAndReplace(query, obj, new FindOneAndReplaceOptions<T, T> { IsUpsert = true });
                }
            }
            else
            {
                if (keyVal == String.Empty)
                {
                    collection.InsertOne(obj);
                }
                else
                {
                    var query = Builders<T>.Filter.Eq("_id", keyVal);
                    oldobj = collection.FindOneAndReplace(query, obj, new FindOneAndReplaceOptions<T, T> { IsUpsert = true });
                }
            }

            // INsert Log


            // call extended postsave
            if (extpostSave != null)
            {
                extpostSave.PostSave(db, oldobj);

                if (Startup.StaticConfig != null)
                {
                    var cfg = Startup.StaticConfig["AuditLog:Activated"];
                    if (!String.IsNullOrEmpty(cfg) && cfg.Equals("1"))
                    {
                        WriteLog<T>(obj, db, "Insert-Update");
                    }
                }

                //var del = obj;
                //Type t = del.GetType();
                //try
                //{
                //    var yy = t.GetProperty("UpdateBy").GetValue(del);
                //    var xx = new Audit<T>(del, "Insert-Update", yy.ToString(), db);
                //    var colx = db.GetCollection<BsonDocument>(del.GetType().Name + "_Log");
                //    colx.InsertOne(xx.ToBsonDocument());
                //}
                //catch (Exception ex) { }
            }
        }
    }

    public interface IMongoPreSave<T>
    {
        // PreSave will be triggered when document saved using extension method Save<T>(this IMongoDatabase db)
        // PreSave is expected to contains only algorithm to update value (eg: sum, average, dsb)
        // Do not put any external action and long time action such as email sent here.
        void PreSave(IMongoDatabase db);
    }

    public interface IMongoExtendedPreSave<T>
    {
        // PreSave will be triggered when document saved using extension method Save<T>(this IMongoDatabase db, T obj)
        // PreSave is expected to contains only algorithm to update value (eg: sum, average, dsb)
        // Do not put any external action and long time action such as email sent here.
        void PreSave(IMongoDatabase db, T originalObject);
    }

    public interface IMongoExtendedPostSave<T>
    {
        // PostSave will be triggered when document saved using extension method Save<T>(this IMongoDatabase db, T obj)
        // PostSave is expected to contains only algorithm to update value on other collection (eg: sum, average, dsb)
        // Do not put any external action such as sending email here, because document may not completely saved yet and can
        // be rolled back by transaction
        void PostSave(IMongoDatabase db, T originalObject);
    }

    public interface IMongoExtendedPostDelete<T>
    {
        void PostDelete(IMongoDatabase db, T originalObject);
    }

    public interface IMongoCollectionExtensions<T>
    {
        void PostFind(IMongoDatabase db, T originalObject);
    }


}
