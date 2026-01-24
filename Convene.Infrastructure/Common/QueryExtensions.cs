using Microsoft.EntityFrameworkCore;
using Convene.Application.DTOs.PagenationDtos;
using Convene.Application.DTOs.Requests;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace Convene.Infrastructure.Common
{
    public static class QueryableExtensions
    {
        public static async Task<PaginatedResult<T>> ApplyPaginationAndSortingAsync<T>(
            this IQueryable<T> query,
            PagedAndSortedRequest request)
        {
            // Apply sorting if specified
            if (!string.IsNullOrEmpty(request.SortBy))
            {
                query = ApplySorting(query, request.SortBy, request.SortDirection);
            }

            // Count total items
            var totalCount = await query.CountAsync();

            // Apply paging
            var items = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            return new PaginatedResult<T>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize)
            };
        }

        private static IQueryable<T> ApplySorting<T>(IQueryable<T> query, string sortBy, string sortDirection)
        {
            // Get property by name
            var property = typeof(T).GetProperty(sortBy, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (property == null)
                return query; // property not found, skip sorting

            // Build expression: x => x.Property
            var parameter = Expression.Parameter(typeof(T), "x");
            var propertyAccess = Expression.MakeMemberAccess(parameter, property);
            var orderByExp = Expression.Lambda(propertyAccess, parameter);

            string methodName = sortDirection.ToLower() == "desc" ? "OrderByDescending" : "OrderBy";

            var resultExp = Expression.Call(
                typeof(Queryable),
                methodName,
                new Type[] { typeof(T), property.PropertyType },
                query.Expression,
                Expression.Quote(orderByExp));

            return query.Provider.CreateQuery<T>(resultExp);
        }
    }
}
