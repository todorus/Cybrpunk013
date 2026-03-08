namespace SurveillanceStategodot.scripts.domain.communication;

public sealed class Intercept
{
    public string Id { get; }
    public Communication Communication { get; }
    public Interceptor Interceptor { get; }
    public InterceptQuality Quality { get; set; }
    public double Time { get; }

    public Intercept(string id, Communication communication, Interceptor interceptor, double time)
    {
        Id = id;
        Communication = communication;
        Interceptor = interceptor;
        Time = time;
    }
}