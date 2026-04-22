using System.Text.Json;
using KansasChildSupport.Web.Models;

namespace KansasChildSupport.Web.Services;

public interface ISessionService
{
    WorksheetSession GetSession(ISession session);
    void SaveSession(ISession session, WorksheetSession data);
    void ClearSession(ISession session);
}

public class SessionService : ISessionService
{
    private const string SessionKey = "worksheet_session";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    public WorksheetSession GetSession(ISession session)
    {
        var json = session.GetString(SessionKey);
        if (string.IsNullOrEmpty(json))
            return new WorksheetSession();
        try
        {
            return JsonSerializer.Deserialize<WorksheetSession>(json, JsonOptions) ?? new WorksheetSession();
        }
        catch
        {
            return new WorksheetSession();
        }
    }

    public void SaveSession(ISession session, WorksheetSession data)
    {
        var json = JsonSerializer.Serialize(data, JsonOptions);
        session.SetString(SessionKey, json);
    }

    public void ClearSession(ISession session)
    {
        session.Remove(SessionKey);
    }
}
