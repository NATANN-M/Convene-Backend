using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Convene.Application.DTOs.Payment
{
    public class ChapaInitResponse
    {
        public string Status { get; set; } = null!;
        public string Message { get; set; } = null!;
        public ChapaInitData Data { get; set; } = null!;
    }

    public class ChapaInitData
    {
        public string TxRef { get; set; } = null!;
        [JsonPropertyName("checkout_url")]
        public string CheckoutUrl { get; set; } = null!;
    }

    public class ChapaVerifyResponse
    {
        public string Status { get; set; } = null!;
        public string Message { get; set; } = null!;
        public ChapaVerifyData Data { get; set; } = null!;
    }

    public class ChapaVerifyData
    {
        public string Status { get; set; } = null!;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = null!;
        public string TxRef { get; set; } = null!;
    }

    public class ChapaCallbackDto
    {
        [JsonPropertyName("tx_ref")]
        public string TxRef { get; set; } = null!;

       
        [JsonPropertyName("status")]
        public string? Status { get; set; }
    }

}
