namespace KollectorScum.Api.Models.Enums
{
    /// <summary>
    /// Goldmine Grading Guide constants for vinyl record condition.
    /// Grades are stored as short codes (e.g. "VG+") in the database.
    /// </summary>
    public static class GoldmineGrade
    {
        /// <summary>Mint – perfect in every way, often unplayed.</summary>
        public const string Mint = "M";

        /// <summary>Near Mint – nearly perfect, shows minimal signs of handling.</summary>
        public const string NearMint = "NM";

        /// <summary>Very Good Plus – shows some signs of play but plays near perfectly.</summary>
        public const string VeryGoodPlus = "VG+";

        /// <summary>Very Good – obvious signs of handling, plays through without skips.</summary>
        public const string VeryGood = "VG";

        /// <summary>Good Plus – heavily played but plays through without skipping.</summary>
        public const string GoodPlus = "G+";

        /// <summary>Good – heavily worn, plays through but with significant noise.</summary>
        public const string Good = "G";

        /// <summary>Fair – dirty, cracked, or warped, plays with difficulty.</summary>
        public const string Fair = "F";

        /// <summary>Poor – barely playable.</summary>
        public const string Poor = "P";

        /// <summary>
        /// All valid Goldmine grade codes.
        /// </summary>
        public static readonly IReadOnlySet<string> ValidGrades = new HashSet<string>
        {
            Mint, NearMint, VeryGoodPlus, VeryGood, GoodPlus, Good, Fair, Poor
        };

        /// <summary>
        /// Parses a Discogs full-text condition string (e.g. "Very Good Plus (VG+)") into a
        /// Goldmine short code (e.g. "VG+"). Returns <c>null</c> if the value cannot be
        /// recognised or is empty.
        /// </summary>
        /// <param name="discogsCondition">The raw condition string from the Discogs API.</param>
        /// <returns>A Goldmine short code, or <c>null</c>.</returns>
        public static string? ParseDiscogsCondition(string? discogsCondition)
        {
            if (string.IsNullOrWhiteSpace(discogsCondition))
                return null;

            var trimmed = discogsCondition.Trim();

            // Direct match first (handles cases where Discogs already returns the code)
            if (ValidGrades.Contains(trimmed))
                return trimmed;

            // Discogs typically wraps the code in parentheses: "Very Good Plus (VG+)"
            var parenStart = trimmed.LastIndexOf('(');
            var parenEnd = trimmed.LastIndexOf(')');
            if (parenStart >= 0 && parenEnd > parenStart)
            {
                var code = trimmed.Substring(parenStart + 1, parenEnd - parenStart - 1).Trim();
                if (ValidGrades.Contains(code))
                    return code;
            }

            // Fallback: case-insensitive text lookup
            return trimmed.ToUpperInvariant() switch
            {
                "MINT" => Mint,
                "NEAR MINT" or "NEARMINT" => NearMint,
                "VERY GOOD PLUS" or "VERYGOODPLUS" => VeryGoodPlus,
                "VERY GOOD" or "VERYGOOD" => VeryGood,
                "GOOD PLUS" or "GOODPLUS" => GoodPlus,
                "GOOD" => Good,
                "FAIR" => Fair,
                "POOR" => Poor,
                _ => null
            };
        }
    }
}
