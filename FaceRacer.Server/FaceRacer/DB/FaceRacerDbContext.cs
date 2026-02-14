using FaceRacer.DB.Entities;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FaceRacer.DB;

public class FaceRacerDbContext : DbContext
{
    public DbSet<RankingData> Rankings { get; set; }

    public DbSet<RecordChange> RecordChanges { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=ranking.db");

        // Only when running in Debug mode:
        /*if (System.Diagnostics.Debugger.IsAttached)
        {
            optionsBuilder
                .LogTo(Console.WriteLine)
                .EnableSensitiveDataLogging();
        }*/
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RankingData>()
            .HasKey(r => r.Id);
            
        modelBuilder.Entity<RankingData>()
            .HasIndex(r => new { r.TrackId, r.Period, r.PeriodDate });

        modelBuilder.Entity<RankingData>(entity =>
        {
            entity.OwnsMany(r => r.RankingDetails, d => { d.ToJson(); });
        });

        modelBuilder.Entity<RecordChange>()
            .HasKey(r => r.Id);

        modelBuilder.Entity<RecordChange>()
            .HasIndex(r => new { r.TrackId, r.Period, r.PeriodDate });

        modelBuilder.Entity<RecordChange>()
            .HasIndex(r => r.NotificationDate);

        modelBuilder.Entity<RecordChange>(entity =>
        {
            entity.OwnsMany(r => r.RankingChanges, d => { d.ToJson(); });
        });

        modelBuilder.Entity<RankingDataQueryResult>(entity =>
        {
            entity.HasNoKey();
            entity.ToView(null);
        });
        
        modelBuilder.Entity<UserInfo>(entity =>
        {
            entity.HasNoKey();
            entity.ToView(null);
        });
    }

    public IQueryable<RankingDataQueryResult> QueryRankingsForUser(int userId)
    {
        var sql =
        """
            SELECT
                r.Id AS RankingId,
                r.Period,
                r.PeriodDate,
                json_extract(j.value, '$.user_uuid')    AS user_uuid,
                json_extract(j.value, '$.date')    AS date,
                json_extract(j.value, '$.user_id')      AS user_id,
                json_extract(j.value, '$.pos')          AS pos,
                json_extract(j.value, '$.full_name')    AS full_name,
                json_extract(j.value, '$.age')   		AS age,
            	json_extract(j.value, '$.profile_image_normal') AS profile_image_normal,
            	json_extract(j.value, '$.profile_url') AS profile_url,
            	json_extract(j.value, '$.best_time') AS best_time,
            	json_extract(j.value, '$.best_time_ms') AS best_time_ms,
            	json_extract(j.value, '$.username') AS username,
            	json_extract(j.value, '$.session_uuid') AS session_uuid
            FROM Rankings AS r,
                 json_each(r.RankingDetails, '$') AS j
            WHERE json_extract(j.value, '$.user_id') = @userId
        """;

        var parameter = new SqliteParameter("@userId", userId);

        return Set<RankingDataQueryResult>().FromSqlRaw(sql, parameter).AsNoTracking();
    }

    public IQueryable<UserInfo> SearchUsers(string fullNameSearch)
    {
        var sql =
            """
            SELECT 
                json_extract(j.value, '$.user_id')      AS user_id,
                MIN(json_extract(j.value, '$.user_uuid'))    AS user_uuid,
                MIN(json_extract(j.value, '$.full_name'))    AS full_name,
                MIN(json_extract(j.value, '$.age'))   		AS age,
                MIN(json_extract(j.value, '$.profile_image_normal')) AS profile_image_normal,
                MIN(json_extract(j.value, '$.profile_url')) AS profile_url,
                MIN(json_extract(j.value, '$.username')) AS username
            FROM Rankings AS r, json_each(r.RankingDetails, '$') AS j
            WHERE json_extract(j.value, '$.full_name') like @fullNameSearch
            GROUP BY json_extract(j.value, '$.user_id')
            """;

        var parameter = new SqliteParameter("@fullNameSearch", $"{fullNameSearch}");

        return Set<UserInfo>().FromSqlRaw(sql, parameter).AsNoTracking();
    }
}