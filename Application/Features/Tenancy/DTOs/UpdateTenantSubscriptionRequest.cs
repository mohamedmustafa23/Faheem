using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Tenancy.DTOs
{
    public class UpdateTenantSubscriptionRequest
    {
        public string TenantId { get; set; } = string.Empty;
        public DateTime ValidUpTo { get; set; }
    }
}
