namespace TeamsMediaBot;

using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

public record JoinCallRequest
(
    [Required]
    [property: JsonProperty("joinUrl")]
    string JoinUrl
);
