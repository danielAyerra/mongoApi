using Serilog;

namespace DbCom;

public class MongoDbApiLogger{
    public MongoDbApiLogger(){
       Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File("logs/MongoDbApi.log", rollingInterval: RollingInterval.Day)
            .CreateLogger();
        Log.Information("App start");
    }

    public void Debug(string message){
        Log.Debug(message);
    }
    public void Info(string message){
        Log.Information(message);
    }
    public void Warn(string message){
        Log.Warning(message);
    }
    public void Err(string message){
        Log.Error(message);
    }
}