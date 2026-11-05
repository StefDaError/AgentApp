using System;
using System.Text.Json.Serialization;

namespace AgentApp.Serialization;

public sealed record MessageEnvelope(
    [property: JsonPropertyName("v")] int Version,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("msg_id")] string MessageId,
    [property: JsonPropertyName("ts")] double Timestamp,
    [property: JsonPropertyName("agent_id")] string AgentId,
    [property: JsonPropertyName("payload")] object Payload)
{
    public static MessageEnvelope CreateHardware(string agentId, object payload) =>
        new(
            Version: 1,
            Type: "hardware",
            MessageId: Guid.NewGuid().ToString("N"),
            Timestamp: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0,
            AgentId: agentId,
            Payload: payload);
}