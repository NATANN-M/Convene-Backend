using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Convene.Application.DTOs.PagenationDtos
{
    public class PagedAndSortedRequest
    {
        public int PageNumber { get; set; } = 1;        
        public int PageSize { get; set; } = 10;         

        public string? SortBy { get; set; }             
        public string? SortDirection { get; set; } = "asc"; 
    }
}
