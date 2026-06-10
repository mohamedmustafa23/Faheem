using Infrastructure.Tenancy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.OpenApi
{
    public class TenantHeaderAttribute()
      : SwaggerHeaderAttribute(
          TenancyConstants.TenantIdName,
          "Enter your tenant name to access this API",
          null,
          true);
}
