using Microsoft.AspNetCore.Http;
using PayderPay.Application.Common.Pagination;

namespace PayderPay.Api.Extensions;

public static class PaginationResponseExtensions
{
    public static void AddPaginationHeaders<T>(this HttpResponse response, PagedResult<T> paged)
    {
        response.Headers["X-Page"] = paged.Page.ToString();
        response.Headers["X-Page-Size"] = paged.PageSize.ToString();
        response.Headers["X-Total-Count"] = paged.TotalCount.ToString();
        response.Headers["X-Total-Pages"] = paged.TotalPages.ToString();
    }
}
