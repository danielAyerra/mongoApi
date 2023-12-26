namespace DbCom;

using DbCom.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;


public class MongoDbApi
{
    private readonly MongoDbApiLogger logger = new MongoDbApiLogger();

    private MongoClient _mongoClient;
    private IMongoDatabase _mongoDatabase;

    public bool RegisterClassMaps(){
        bool registrationOk;
        try{
            logger.Debug("It is mandatory before building to add here any Model available in your project, as set at the example");
            if(!BsonClassMap.IsClassMapRegistered(typeof(AbstractModel)))
                BsonClassMap.RegisterClassMap<AbstractModel>(cm =>
                {
                    cm.AutoMap();
                    cm.SetDiscriminatorIsRequired(true);
                });
            logger.Info("Registered abstract class, root of all the models to use");
            byte counter=0;
            //Add here any Models available at the project
            if(!BsonClassMap.IsClassMapRegistered(typeof(Example))){
                BsonClassMap.RegisterClassMap<Example>();
                counter++;    
            }
            logger.Info($"Added {counter} models. Enjoy");
            registrationOk = true;
        } catch(Exception e){
            logger.Err($"Error while registering model maps: {e.Message}");
            registrationOk=false;
        }
        return registrationOk;
    }

    public bool Connect(string connection, string database){
        bool connOk;
        try{
            _mongoClient=new MongoClient(connection);
            _mongoDatabase=_mongoClient.GetDatabase(database);
            logger.Info("Connection succesful");
            connOk=true;
        }catch(Exception e){
            logger.Err($"Issue connecting to MongoDb: {e.Message}");
            connOk=false;
        }
        return connOk;    
    }

    public bool Insert(string jsonString, Type type){   
        bool insertOk;
        try{          
            AbstractModel retrievedData = GetDataObjectFromJsonString(jsonString, type);
            var collection = _mongoDatabase.GetCollection<AbstractModel>(type.Name);
            collection.InsertOne(retrievedData);
            logger.Info($"Insertion successful");
            insertOk=true;
        }
        catch(Exception e){
            logger.Err($"Insertion error: {e.Message}");
            insertOk=false; 
        }
        return insertOk;
    }

    private AbstractModel GetDataObjectFromJsonString(string jsonString, Type type)
    {
        logger.Debug("Parsing json to object");
        logger.Warn("Make sure type inherits from AbstractModel");
        JToken jObj=JObject.Parse(jsonString);
        string typeName = jObj["ChildType"].ToString();
        if (type.Name!=typeName)
            throw new Exception("Type mismatch. Review json and check the type actually desired");
        AbstractModel retrievedObject =(AbstractModel)jObj.ToObject(type);
        return retrievedObject;
    }

    public bool InsertMany (string jsonString, Type type){
        bool insertOk;
        logger.Warn("Check that type passed is registered and maped");
        try{
            List<AbstractModel> data = GetDataListFromJsonString(jsonString, type);
            var collection = _mongoDatabase.GetCollection<AbstractModel>(type.Name);
            collection.InsertMany(data);
            logger.Info("Insertion succesful");
            insertOk=true;
        }
        catch(Exception e){
            logger.Err($"Insertion error: {e.Message}");
            insertOk=false;
        }
        return insertOk;
    }

    private List<AbstractModel> GetDataListFromJsonString(string jsonString, Type type)
    {
        logger.Debug("Parsing json to object");
        logger.Warn("Make sure type inherits from AbstractModel");
        JArray jArr=JArray.Parse(jsonString);
        List<AbstractModel> elements = new List<AbstractModel>();
        foreach(JObject jObj in jArr){
            string tipo = jObj["ChildType"].ToString();
            if (type.Name!=tipo)
                throw new Exception("Type mismatch. Review json and check the type actually desired");
            AbstractModel retrievedObject=(AbstractModel)jObj.ToObject(type);
            elements.Add(retrievedObject);
        }
        return elements;
    }

    public bool Select(out List<AbstractModel> data, string? jsonParams, Type type){
        bool selectOk;
        data = new List<AbstractModel>();
        try{
            var collection=_mongoDatabase.GetCollection<BsonDocument>(type.Name);
            List<BsonDocument> findObject;
            logger.Warn($"make sure that jsonParams are attributes which actually exist at <{type.Name}>");
            BsonDocument filterDocument=composeFilter(jsonParams);
            findObject = collection.Find<BsonDocument>(filterDocument).ToList<BsonDocument>();
            foreach(BsonDocument bsonElement in findObject){
                data.Add(BsonSerializer.Deserialize<AbstractModel>(bsonElement));
            }
            logger.Info("No issues on searching for data");
            selectOk=true;
        }catch(Exception e){
            logger.Err($"Issue while searching: {e.Message}");
            selectOk=false;
            data=new List<AbstractModel>();
        }
        return selectOk; 
    }
    
    public bool Update(object data, Type type, Dictionary<string, string> newVals){
        bool updateOk;
        try{
            var collection=_mongoDatabase.GetCollection<BsonDocument>(type.Name);
            AbstractModel oldObject = (AbstractModel)data;
            if(oldObject.ObjectId!=null){
                ObjectId id = new ObjectId(oldObject.ObjectId);
                var filter = Builders<BsonDocument>.Filter.Eq("_id", id);
                List<UpdateDefinition<BsonDocument>> updateParams = new List<UpdateDefinition<BsonDocument>>();
                foreach(string key in newVals.Keys){
                    updateParams.Add(Builders<BsonDocument>.Update.Set(key, newVals[key]));
                }
                var updater = Builders<BsonDocument>.Update.Combine(updateParams);
                collection.UpdateOne(filter, updater);
                logger.Info("Update successful");
            }else{
                updateOk= false;
                throw new Exception("No ObjectId was provided. Check parameter or use UpdateMany");
            }
            updateOk=true;
        }catch(Exception e){
            logger.Err($"Not posible to update: {e.Message}");
            updateOk=false;
        }
        return updateOk;
    }
    
    public bool UpdateMany(Type type, string jsonParams, Dictionary<string,string> newVals){
        bool updateManyOk;
        try{
            var collection=_mongoDatabase.GetCollection<BsonDocument>(type.Name);
            BsonDocument filter = composeFilter(jsonParams);
            List<UpdateDefinition<BsonDocument>> updateParams = new List<UpdateDefinition<BsonDocument>>();
            foreach(string key in newVals.Keys){
                updateParams.Add(Builders<BsonDocument>.Update.Set(key, newVals[key]));
            }
            var updater = Builders<BsonDocument>.Update.Combine(updateParams);
            collection.UpdateMany(filter, updater);
            logger.Info("Update successful");
            updateManyOk= true;
        }catch(Exception e){
            logger.Err($"Failure while updating: {e.Message}");
            updateManyOk=false;
        }
        return updateManyOk;
    }
    
    public bool Delete(object data, string collectionName){
        bool deleteOk;
        try{
            logger.Warn("Using Delete method. Make sure you know what you are doing");
            var collection=_mongoDatabase.GetCollection<BsonDocument>(collectionName);
            AbstractModel objectToDelete = (AbstractModel) data;
            if(objectToDelete.ObjectId!=null){
                ObjectId id = new ObjectId(objectToDelete.ObjectId);
                var filter = Builders<BsonDocument>.Filter.Eq("_id", id);
                collection.DeleteOne(filter);
                logger.Info("Deletion successful");
            }else{
                throw new Exception("No Object Id was provided for deleting");
            }
            deleteOk= true;
        }catch(Exception e){
            logger.Err($"Problem while trying to delete: {e.Message}");
            deleteOk=false;
        }
        return deleteOk;
    }
    
    public bool DeleteMany(Type type, string jsonParams){
        bool deleteManyOk;
        try{
            logger.Warn("Using Delete method. Make sure you know what you are doing");
            var collection=_mongoDatabase.GetCollection<BsonDocument>(type.Name);
            BsonDocument filter = composeFilter(jsonParams);
            collection.DeleteMany(filter);
            deleteManyOk=true;
            logger.Info("Deletion successful");
        }
        catch(Exception e){
            logger.Err($"Problem while trying to delete: {e.Message}");
            deleteManyOk=false;
        }
        return deleteManyOk;
    }
    
    public static BsonDocument composeFilter(string jsonParams){
        JObject jsonObject = JObject.Parse(jsonParams);
        BsonDocument filter = BsonDocument.Parse(jsonObject.ToString());
        return filter;
    }
    
}
