using System.Text.Json.Serialization;

namespace KollectorScum.Api.Models.ValueObjects
{
    /// <summary>
    /// Value object for purchase information as stored in JSON
    /// </summary>
    public class PurchaseInfo
    {
        /// <summary>
        /// Gets or sets the purchase date
        /// </summary>
        [JsonPropertyName("Date")]
        public DateTime? Date { get; set; }

        /// <summary>
        /// Gets or sets the purchase price
        /// </summary>
        [JsonPropertyName("Price")]
        public decimal? Price { get; set; }

        /// <summary>
        /// Gets or sets the store ID
        /// </summary>
        [JsonPropertyName("StoreID")]
        public int? StoreID { get; set; }

        /// <summary>
        /// Gets or sets any purchase notes
        /// </summary>
        [JsonPropertyName("Notes")]
        public string? Notes { get; set; }
    }
}
