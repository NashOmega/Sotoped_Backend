using Core.Data;
using Microsoft.EntityFrameworkCore;

namespace Sotoped.Configuration
{

    public static class DatabaseConfig
    {
        public static void AddDatabaseConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            var connectionString = Environment.GetEnvironmentVariable("DefaultConnection") ?? configuration.GetConnectionString("DefaultConnection");
            services.AddDbContext<SotopedContext>(options =>
            {
                options.UseSqlServer(connectionString);
            });
        }

        public static void InitializeDbTestDataAsync(this IApplicationBuilder app)
        {
            using (var serviceScope = app?.ApplicationServices?.GetService<IServiceScopeFactory>()?.CreateScope())
            {
                if (serviceScope != null)
                {
                    var context = serviceScope.ServiceProvider.GetRequiredService<SotopedContext>();
                    context.Database.EnsureCreated();
                    if (context.Spectators.Any())
                    {
                        SetCodeAsync(context).Wait();
                    }
                }
            }
        }

        static async Task SetCodeAsync(SotopedContext context)
        {
            var spectatorsList = await context.Spectators.Where(s => string.IsNullOrEmpty(s.Code)).ToListAsync();
            var spectatorsWithCodeList = await context.Spectators.Where(s => !string.IsNullOrEmpty(s.Code)).ToListAsync();

            var oldCodes = spectatorsWithCodeList?.Select(s => s.Code).ToList();
            if (spectatorsList.Any())
            {
                var spectatorsCode = GenerateUniqueCodes(spectatorsList.Count(), oldCodes);
                for (var i = 0; i < spectatorsList.Count(); i++)
                {
                    spectatorsList[i].Code = spectatorsCode[i].Trim();
                }
                await context.SaveChangesAsync();
            }
        }

        static List<string> GenerateUniqueCodes(int userCount, List<string?>? oldCodes)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            Random random = new Random();
            HashSet<string> uniqueCodes = new HashSet<string>();

            while (uniqueCodes.Count < userCount)
            {
                string newCode = new string(Enumerable.Range(0, 6)
                    .Select(_ => chars[random.Next(chars.Length)])
                    .ToArray());

                if (oldCodes != null && !oldCodes.Contains(newCode)) uniqueCodes.Add(newCode);
            }

            return uniqueCodes.ToList();
        }
    }
}
