using System.Linq.Expressions;
using KollectorScum.Api.DTOs;
using KollectorScum.Api.Models;

namespace KollectorScum.Api.Services
{
    /// <summary>
    /// Builds EF-compatible filter expressions for music release queries.
    /// Pure expression construction — no database or service dependencies.
    /// Each sub-method owns a single filter concern and has CCN ≤ 5.
    /// </summary>
    internal static class MusicReleaseFilterBuilder
    {
        private static readonly System.Reflection.MethodInfo StringContainsMethod =
            typeof(string).GetMethod("Contains", new[] { typeof(string) })!;

        private static readonly System.Reflection.MethodInfo StringToLowerMethod =
            typeof(string).GetMethod("ToLower", Array.Empty<Type>())!;

        private static readonly System.Reflection.MethodInfo ListIntContainsMethod =
            typeof(List<int>).GetMethod("Contains", new[] { typeof(int) })!;

        /// <summary>
        /// Builds the composite filter expression from query parameters, the resolved user ID,
        /// and any pre-resolved kollection genre IDs.
        /// </summary>
        /// <param name="parameters">Query parameters from the API request.</param>
        /// <param name="userId">The acting user's ID — always applied as the first clause.</param>
        /// <param name="kollectionGenreIds">
        /// Genre IDs belonging to the requested kollection, or <c>null</c> if no kollection filter.
        /// An empty list means the kollection was found but has no genres (no genre clause is added).
        /// </param>
        /// <returns>A combined filter expression, or <c>null</c> if no clauses could be built.</returns>
        internal static Expression<Func<MusicRelease, bool>>? Build(
            MusicReleaseQueryParameters parameters,
            Guid userId,
            IReadOnlyList<int>? kollectionGenreIds)
        {
            var param = Expression.Parameter(typeof(MusicRelease), "mr");
            var clauses = new List<Expression>();

            clauses.Add(BuildUserClause(param, userId));

            var idsClause = BuildIdsClause(param, parameters.Ids);
            if (idsClause != null)
                clauses.Add(idsClause);

            if (!string.IsNullOrEmpty(parameters.Search))
                clauses.Add(BuildSearchClause(param, parameters.Search));

            if (parameters.ArtistId.HasValue)
                clauses.Add(BuildJsonContainsClause(param, nameof(MusicRelease.Artists), parameters.ArtistId.Value));

            if (parameters.GenreId.HasValue)
                clauses.Add(BuildJsonContainsClause(param, nameof(MusicRelease.Genres), parameters.GenreId.Value));

            if (kollectionGenreIds != null && kollectionGenreIds.Count > 0)
            {
                var kollectionClause = BuildKollectionGenreClause(param, kollectionGenreIds);
                if (kollectionClause != null)
                    clauses.Add(kollectionClause);
            }

            AppendNullableIntEquals(clauses, param, nameof(MusicRelease.LabelId), parameters.LabelId);
            AppendNullableIntEquals(clauses, param, nameof(MusicRelease.CountryId), parameters.CountryId);
            AppendNullableIntEquals(clauses, param, nameof(MusicRelease.FormatId), parameters.FormatId);

            if (parameters.Live.HasValue)
                clauses.Add(BuildLiveClause(param, parameters.Live.Value));

            if (parameters.YearFrom.HasValue)
                clauses.Add(BuildYearFromClause(param, parameters.YearFrom.Value));

            if (parameters.YearTo.HasValue)
                clauses.Add(BuildYearToClause(param, parameters.YearTo.Value));

            if (clauses.Count == 0)
                return null;

            Expression combined = clauses[0];
            for (var i = 1; i < clauses.Count; i++)
                combined = Expression.AndAlso(combined, clauses[i]);

            return Expression.Lambda<Func<MusicRelease, bool>>(combined, param);
        }

        /// <summary>Always-on clause that restricts results to the acting user's releases.</summary>
        private static Expression BuildUserClause(ParameterExpression param, Guid userId)
        {
            var prop = Expression.Property(param, nameof(MusicRelease.UserId));
            return Expression.Equal(prop, Expression.Constant(userId));
        }

        /// <summary>Optional CSV-of-IDs clause; returns <c>null</c> when Ids is empty or unparseable.</summary>
        private static Expression? BuildIdsClause(ParameterExpression param, string? ids)
        {
            if (string.IsNullOrEmpty(ids))
                return null;

            var idList = ids
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => int.TryParse(s, out var id) ? (int?)id : null)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .ToList();

            if (idList.Count == 0)
                return null;

            var idProp = Expression.Property(param, nameof(MusicRelease.Id));
            return Expression.Call(Expression.Constant(idList), ListIntContainsMethod, idProp);
        }

        /// <summary>Case-insensitive substring match on the Title property.</summary>
        private static Expression BuildSearchClause(ParameterExpression param, string search)
        {
            var titleProp = Expression.Property(param, nameof(MusicRelease.Title));
            var titleLower = Expression.Call(titleProp, StringToLowerMethod);
            return Expression.Call(titleLower, StringContainsMethod, Expression.Constant(search.ToLower()));
        }

        /// <summary>
        /// JSON-array contains check for integer ID stored as a JSON array string.
        /// Matches all four boundary patterns: [id], [id,, ,id], ,id,
        /// </summary>
        internal static Expression BuildJsonContainsClause(ParameterExpression param, string propName, int id)
        {
            var prop = Expression.Property(param, propName);
            var notNull = Expression.NotEqual(prop, Expression.Constant(null, typeof(string)));

            var c1 = Expression.Call(prop, StringContainsMethod, Expression.Constant("[" + id + "]"));
            var c2 = Expression.Call(prop, StringContainsMethod, Expression.Constant("[" + id + ","));
            var c3 = Expression.Call(prop, StringContainsMethod, Expression.Constant("," + id + "]"));
            var c4 = Expression.Call(prop, StringContainsMethod, Expression.Constant("," + id + ","));

            var anyMatch = Expression.OrElse(Expression.OrElse(c1, c2), Expression.OrElse(c3, c4));
            return Expression.AndAlso(notNull, anyMatch);
        }

        /// <summary>ORs genre contains clauses for all genres in a kollection.</summary>
        private static Expression? BuildKollectionGenreClause(ParameterExpression param, IReadOnlyList<int> genreIds)
        {
            Expression? result = null;
            foreach (var gid in genreIds)
            {
                var expr = BuildJsonContainsClause(param, nameof(MusicRelease.Genres), gid);
                result = result == null ? expr : Expression.OrElse(result, expr);
            }
            return result;
        }

        /// <summary>Appends a nullable int equality clause if the value is non-null.</summary>
        private static void AppendNullableIntEquals(
            List<Expression> clauses,
            ParameterExpression param,
            string propName,
            int? value)
        {
            if (!value.HasValue)
                return;

            var prop = Expression.Property(param, propName);
            clauses.Add(Expression.Equal(prop, Expression.Constant(value.Value, typeof(int?))));
        }

        /// <summary>Boolean equality clause for the Live flag.</summary>
        private static Expression BuildLiveClause(ParameterExpression param, bool live)
        {
            var prop = Expression.Property(param, nameof(MusicRelease.Live));
            return Expression.Equal(prop, Expression.Constant(live));
        }

        /// <summary>Lower-bound year filter (inclusive); excludes releases with null ReleaseYear.</summary>
        private static Expression BuildYearFromClause(ParameterExpression param, int year)
        {
            var prop = Expression.Property(param, nameof(MusicRelease.ReleaseYear));
            var hasValue = Expression.Property(prop, "HasValue");
            var value = Expression.Property(prop, "Value");
            var compare = Expression.GreaterThanOrEqual(
                value,
                Expression.Constant(new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
            return Expression.AndAlso(hasValue, compare);
        }

        /// <summary>Upper-bound year filter (inclusive); excludes releases with null ReleaseYear.</summary>
        private static Expression BuildYearToClause(ParameterExpression param, int year)
        {
            var prop = Expression.Property(param, nameof(MusicRelease.ReleaseYear));
            var hasValue = Expression.Property(prop, "HasValue");
            var value = Expression.Property(prop, "Value");
            var compare = Expression.LessThanOrEqual(
                value,
                Expression.Constant(new DateTime(year, 12, 31, 23, 59, 59, 999, DateTimeKind.Utc)));
            return Expression.AndAlso(hasValue, compare);
        }
    }
}
