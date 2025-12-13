using System;

namespace KollectorScum.Api.DTOs
{
    public class BootstrapRequest
    {
        public string IdToken { get; set; } = string.Empty;
    }

    public class BootstrapResponse
    {
        public Guid UserId { get; set; }
        public string GoogleSub { get; set; } = string.Empty;
    }
}
