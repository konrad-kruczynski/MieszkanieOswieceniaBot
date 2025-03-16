using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Flurl.Http;
using MieszkanieOswieceniaBot.Base;

namespace MieszkanieOswieceniaBot.Heating;

public class Shelly : HttpBasedSomething, IValve
{
    public Shelly(decimal warmTemperature, decimal coldTemperature, string hostname, TimeSpan timeout = default) : base(hostname, timeout)
    {
        WarmTemperature = warmTemperature;
        ColdTemperature = coldTemperature;
    }

    public Task<bool> Activate()
    {
        return SetTemperature(WarmTemperature);
    }

    public Task<bool> Deactivate()
    {
        return SetTemperature(ColdTemperature);
    }

    public decimal WarmTemperature { get; }
    public decimal ColdTemperature { get; }
    
    public async Task<bool> Boost(bool enable)
    {
        var result = await Query<BasicSubquery, object>
            (enable ? Method.SetBoost : Method.ClearBoost, new BasicSubquery());
        return result is { Item1: true, Item2: null };
    }

    public async Task<(bool, decimal)> GetCurrentTemperature()
    {
        var status = await Query<GetStatusResponse>(Method.GetStatus);
        return (status.Item1, status.Item2.Status.CurrentTemperature);
    }

    public async Task<bool> SetTemperature(decimal temperature)
    {
        var result = await Query<SetTemperatureSubquery, object>
            (Method.SetTarget, new SetTemperatureSubquery { TargetTemperature = temperature });
        return result is { Item1: true, Item2: null };
    }

    private Task<(bool, TResult)> Query<TResult>(Method method) where TResult : new()
    {
        return Query<BasicSubquery, TResult>(method, new BasicSubquery());
    }

    private async Task<(bool, TResult)> Query<TSubquery, TResult>(Method method, TSubquery subqueryParams) where TResult : new()
    {
        try
        {
            var request = FlurlClient.Request("rpc", "BluTrv.Call");
            request.SetQueryParam("id", 200);
            var prefix = method == Method.GetStatus ? "Shelly" : "Trv";
            request.SetQueryParam("method", $"{prefix}.{method}");

            var subQueryParamsSerialized = JsonSerializer.Serialize(subqueryParams);
            request.SetQueryParam("params", subQueryParamsSerialized);
            return (true, await request.GetJsonAsync<TResult>());
        }
        catch (Exception exception) when (exception is FlurlHttpException || exception is TaskCanceledException)
        {
            CircularLogger.Instance.Log($"Exception on {FlurlClient}: {exception.Message}");
            return (false, new TResult());
        }
    }

    private enum Method
    {
        AddScheduleRule,
        RemoveScheduleRule,
        ListScheduleRules,
        GetStatus,
        SetTarget,
        SetBoost,
        ClearBoost
    }

    private record GetStatusResponse
    {
        [JsonPropertyName("trv:0")]
        public TrvStatus Status { get; set; }
    }

    private record TrvStatus
    {
        [JsonPropertyName("current_C")]
        public decimal CurrentTemperature { get; set; }
    }

    private record BasicSubquery
    {
        [JsonPropertyName("id")]
        public int Id { get; init; }
    }

    private record SetTemperatureSubquery : BasicSubquery
    {
        [JsonPropertyName("target_C")]
        public decimal TargetTemperature { get; init; }
    }
    
    private record ScheduleResponse
    {
        [JsonPropertyName("schedule_rev")]
        public int Revision { get; init; }
        
        public ScheduleRule[] Rules { get; init; }
    }

    private record ScheduleRule
    {
        [JsonPropertyName("rule_id")]
        public int Id { get; init; }
        
        public bool Enable { get; init; }
        
        [JsonPropertyName("target_C")]
        public decimal TargetTemperature { get; init; }
        
        public string Timespec { get; init; }
    }
}