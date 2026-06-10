using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Tenancy
{
    public interface ITenantDbSeeder
    {
        Task InitializeDatabaseAsync(CancellationToken ct);
    }
}
